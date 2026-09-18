using System.Collections;
using OzGameLab01.Board.Models;
using OzGameLab01.Board.Views;
using System.Collections.Generic;
using OzGameLab01.Data;
using UnityEngine;

namespace OzGameLab01.Map
{
    /// <summary>
    /// 절차적으로 생성된 보드 위에 '중간 보스 -> 최종 보스' 진행 경로를 후처리로 배치합니다.
    ///
    /// MapGenerator의 정적 타일 생성과 분리해 목표 선정과 화면 갱신만 담당합니다.
    /// 보드 목표 디렉팅의 단일 진입점입니다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MapRouteDirector : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private MapGenerator mapGenerator;
        [SerializeField] private BoardRouteProfile routeProfile;
        [SerializeField] private GameObject highlightPrefab;

        [Tooltip("맵 생성 애니메이션이 끝난 뒤 목표를 배치하기까지의 추가 대기 시간입니다.")]
        [Min(0f)] [SerializeField] private float objectiveSpawnDelay = 0.5f;

        [Header("Highlight")]
        [SerializeField] private float highlightHeight = 2f;

        private readonly BoardObjectiveHighlightView _highlightView = new BoardObjectiveHighlightView();
        private MapNode finalBossNode;
        private MapNode currentObjective;

        /// <summary>현재 하이라이트된 목표입니다. 디버그 UI 등에서 읽기 전용으로 사용할 수 있습니다.</summary>
        public MapNode CurrentObjective => currentObjective;
        public GameObject CurrentObjectiveView => mapGenerator != null ? mapGenerator.GetNodeView(currentObjective) : null;

        /// <summary>맵 분석으로 예약된 최종 보스 위치입니다.</summary>
        public MapNode FinalBossNode => finalBossNode;

        private void Awake()
        {
            ResolveActiveMapGenerator();
        }

        private void OnEnable()
        {
            BoardRunData.OnBattleCompleted += HandleBattleCompleted;
        }

        private void OnDisable()
        {
            BoardRunData.OnBattleCompleted -= HandleBattleCompleted;
        }

        private IEnumerator Start()
        {
            while (mapGenerator == null || mapGenerator.NodeDict.Count == 0)
            {
                ResolveActiveMapGenerator();
                yield return null;
            }

            // 전투 복귀에서는 MapGenerator가 타일을 즉시 복원하므로 연출 시간만큼 기다리지 않습니다.
            if (mapGenerator.UsedInitialGenerationAnimation)
            {
                yield return new WaitForSeconds(mapGenerator.animationDuration + objectiveSpawnDelay);
            }

            RefreshNextObjective();
        }

        private void ResolveActiveMapGenerator()
        {
            if (mapGenerator != null && mapGenerator.isActiveAndEnabled)
            {
                return;
            }

            mapGenerator = FindFirstObjectByType<MapGenerator>();
        }

        private void HandleBattleCompleted()
        {
            if (mapGenerator != null &&
                BoardRunData.HasPlayerPosition &&
                mapGenerator.NodeDict.TryGetValue(BoardRunData.PlayerPosition, out MapNode completedNode))
            {
                mapGenerator.NormalizeConsumedSpecialTile(completedNode);
            }

            // 전투 씬을 거쳐 보드 씬이 새로 로드되는 경우에는 Start()가 다시 처리합니다.
            // 보드가 유지되는 전환 방식도 지원하도록 한 프레임 뒤에 다시 계산합니다.
            StartCoroutine(RefreshAfterBattle());
        }

        private IEnumerator RefreshAfterBattle()
        {
            yield return null;
            RefreshNextObjective();
        }

        /// <summary>
        /// 현재 플레이어 위치와 중간 보스 처치 수를 기준으로 다음 목표를 배치하고 하이라이트합니다.
        /// 보드 진입 및 전투 종료 후 목표 갱신
        /// </summary>
        public void RefreshNextObjective()
        {
            if (mapGenerator == null || mapGenerator.NodeDict.Count == 0)
            {
                Debug.LogWarning("[MapRouteDirector] MapGenerator가 준비되지 않았습니다.", this);
                return;
            }

            if (routeProfile == null)
            {
                Debug.LogError("[MapRouteDirector] BoardRouteProfile이 할당되지 않았습니다.", this);
                return;
            }

            if (BoardRunData.IsBossDefeated)
            {
                ClearHighlight();
                BoardRunData.ClearObjective();
                return;
            }

            NodeType targetType = BoardRunData.DefeatedElitesCount >= routeProfile.RequiredEliteCount
                ? NodeType.Boss
                : NodeType.Elite;

            // Continue 저장과는 분리된 실행 세션 상태입니다. 일반 전투에서 돌아왔다면
            // 아직 소비되지 않은 기존 목표를 같은 좌표에 먼저 복원합니다.
            if (BoardRunData.HasObjective)
            {
                if (mapGenerator.NodeDict.TryGetValue(
                        BoardRunData.ObjectivePosition,
                        out MapNode savedNode) &&
                    !BoardRunData.IsSpecialTileConsumed(savedNode.Position) &&
                    (savedNode.Type == NodeType.Normal || savedNode.Type == targetType))
                {
                    if (currentObjective != savedNode || savedNode.Type != targetType)
                    {
                        SetObjective(savedNode, targetType);
                    }

                    return;
                }

                BoardRunData.ClearObjective();
            }

            List<Vector2Int> consumed = new List<Vector2Int>();
            foreach (MapNode node in mapGenerator.NodeDict.Values)
            {
                if (BoardRunData.IsSpecialTileConsumed(node.Position))
                {
                    consumed.Add(node.Position);
                }
            }
            BoardRouteSettings settings = routeProfile.CreateSettings();
            BoardRouteModel model = new BoardRouteModel(mapGenerator.NodeDict, consumed, BoardRunData.DefeatedElitesCount, settings);
            BoardRouteDecision decision = model.SelectNext(BoardRunData.HasPlayerPosition, BoardRunData.PlayerPosition);
            if (decision.Status != BoardRouteStatus.MissingStart) { finalBossNode = decision.Boss; }
            if (decision.Status != BoardRouteStatus.Ready)
            {
                Debug.LogError("[MapRouteDirector] 목표 선정 실패: " + decision.Status, this);
                return;
            }
            SetObjective(decision.Target, decision.Type);
        }

        private void SetObjective(MapNode targetNode, NodeType targetType)
        {
            ClearHighlight();

            targetNode.Type = targetType;
            mapGenerator.ReplaceTileVisual(targetNode);
            currentObjective = targetNode;
            BoardRunData.SaveObjectivePosition(targetNode.Position);

            GameObject targetView = mapGenerator.GetNodeView(targetNode);
            if (highlightPrefab != null && targetView != null)
            {
                _highlightView.Show(highlightPrefab, targetView.transform, highlightHeight);
            }

            Debug.Log(
                $"[MapRouteDirector] 다음 목표 배치 | Type: {targetType}, " +
                $"Position: {targetNode.Position}, " +
                $"Defeated Elites: {BoardRunData.DefeatedElitesCount}/{routeProfile.RequiredEliteCount}",
                this);
        }

        private void ClearHighlight()
        {
            _highlightView.Clear();

            currentObjective = null;
        }

    }
}
