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
    /// 기존 MapGenerator를 수정하지 않고 NodeDict와 ReplaceTileVisual만 사용합니다.
    /// 보드 목표 선정과 화면 갱신의 단일 진입점

    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MapRouteDirector : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private MapGenerator mapGenerator;
        [SerializeField] private GameObject highlightPrefab;

        [Header("Progress")]
        [Tooltip("최종 보스 전에 처치해야 하는 중간 보스 수입니다.")]
        [Min(0)] [SerializeField] private int eliteCount = 3;

        [Tooltip("맵 생성 애니메이션이 끝난 뒤 목표를 배치하기까지의 추가 대기 시간입니다.")]
        [Min(0f)] [SerializeField] private float objectiveSpawnDelay = 0.5f;

        [Header("Leg Distance (tile hops)")]
        [Tooltip("현재 목표/플레이어 위치에서 다음 중간 보스까지의 최소 그래프 거리입니다.")]
        [Min(1)] [SerializeField] private int minimumEliteLegDistance = 10;

        [Tooltip("현재 목표/플레이어 위치에서 다음 중간 보스까지의 최대 그래프 거리입니다.")]
        [Min(1)] [SerializeField] private int maximumEliteLegDistance = 18;

        [Tooltip("최종 보스가 직전 중간 보스에 너무 붙지 않도록 보장하는 최소 거리입니다.")]
        [Min(1)] [SerializeField] private int minimumBossLegDistance = 10;

        [Tooltip("남은 중간 보스와 최종 보스를 배치할 공간이 부족한 후보를 피하기 위한 여유 거리입니다.")]
        [Range(0f, 1f)] [SerializeField] private float futureRouteReserveRatio = 0.6f;

        [Header("Route Scoring")]
        [Tooltip("중간 보스 후보가 최종 보스를 향해 전진할수록 받는 가중치입니다.")]
        [Min(0f)] [SerializeField] private float forwardProgressWeight = 1.5f;

        [Tooltip("갈림길 및 주변 타일이 있는 후보를 선호하는 가중치입니다.")]
        [Min(0f)] [SerializeField] private float explorationOpportunityWeight = 1.2f;

        [Tooltip("중간 보스마다 좌/우 방향을 번갈아 선호하는 정도입니다. 강제가 아닌 가산점입니다.")]
        [Min(0f)] [SerializeField] private float sideAlternationWeight = 2f;

        [Tooltip("최종 보스로 가는 최단 경로에서 과도하게 벗어나는 후보의 감점입니다.")]
        [Min(1f)] [SerializeField] private float maximumDetourRatio = 1.6f;

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

            if (BoardRunData.IsBossDefeated)
            {
                ClearHighlight();
                return;
            }

            List<Vector2Int> consumed = new List<Vector2Int>();
            foreach (MapNode node in mapGenerator.NodeDict.Values)
            {
                if (BoardRunData.IsSpecialTileConsumed(node.Position))
                {
                    consumed.Add(node.Position);
                }
            }
            BoardRouteSettings settings = new BoardRouteSettings
            {
                eliteCount = eliteCount,
                minimumEliteLegDistance = minimumEliteLegDistance,
                maximumEliteLegDistance = maximumEliteLegDistance,
                minimumBossLegDistance = minimumBossLegDistance,
                futureRouteReserveRatio = futureRouteReserveRatio,
                forwardProgressWeight = forwardProgressWeight,
                explorationOpportunityWeight = explorationOpportunityWeight,
                sideAlternationWeight = sideAlternationWeight,
                maximumDetourRatio = maximumDetourRatio,
            };
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

            GameObject targetView = mapGenerator.GetNodeView(targetNode);
            if (highlightPrefab != null && targetView != null)
            {
                _highlightView.Show(highlightPrefab, targetView.transform, highlightHeight);
            }

            Debug.Log(
                $"[MapRouteDirector] 다음 목표 배치 | Type: {targetType}, " +
                $"Position: {targetNode.Position}, " +
                $"Defeated Elites: {BoardRunData.DefeatedElitesCount}/{eliteCount}",
                this);
        }

        private void ClearHighlight()
        {
            _highlightView.Clear();

            currentObjective = null;
        }

    }
}
