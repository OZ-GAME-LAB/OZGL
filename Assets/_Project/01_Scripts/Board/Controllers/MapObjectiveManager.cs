using UnityEngine;
using OzGameLab01.Board.Models;
using OzGameLab01.Board.Views;
using System.Collections;
using OzGameLab01.Data;
using OzGameLab01.Map;

public class MapObjectiveManager : MonoBehaviour
{
    [Header("연결")]
    public MapGenerator mapGenerator;
    public GameObject highlightPrefab;

    [Header("스폰 거리 설정")]
    public int minSpawnDistance = 10;
    // [추가됨] 너무 멀리 스폰되지 않도록 제한하는 최대 거리
    public int maxSpawnDistance = 999;

    [Header("진행도 설정")]
    public int maxElites = 3;

    private readonly BoardObjectiveHighlightView _highlightView = new BoardObjectiveHighlightView();

    private void Awake()
    {
        // 1. 실수로 인스펙터에 연결을 안 해두었더라도 자동으로 찾아오도록 안전장치 추가
        if (mapGenerator == null)
            mapGenerator = FindFirstObjectByType<MapGenerator>();
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
        // 맵 생성기가 준비될 때까지 대기
        while (mapGenerator == null || mapGenerator.NodeDict.Count == 0)
        {
            yield return null;
        }

        // 2. 타이밍 충돌 방지: 맵 생성 애니메이션(5초)이 완전히 끝날 때까지 기다린 후 첫 스폰!
        yield return new WaitForSeconds(mapGenerator.animationDuration + 0.5f);

        SpawnNextObjective();
    }

    private void HandleBattleCompleted()
    {
        SpawnNextObjective();
    }

    public void SpawnNextObjective()
    {
        if (BoardRunData.IsBossDefeated) return; // [추가] 보스를 이미 처치했다면 스폰 중지
        if (BoardRunData.DefeatedElitesCount > maxElites) return;

        NodeType targetType = (BoardRunData.DefeatedElitesCount == maxElites) ? NodeType.Boss : NodeType.Elite;

        Vector2Int currentPos = BoardRunData.HasPlayerPosition ? BoardRunData.PlayerPosition : Vector2Int.zero;
        if (!mapGenerator.NodeDict.TryGetValue(currentPos, out MapNode startNode)) return;

        MapNode targetNode = null;

        // 3. 거리 조건 완화: 최소 거리 조건에 맞는 타일이 없으면 9칸, 8칸... 계속 줄여나가며 찾아냄!
        // (단, maxSpawnDistance 이내여야 함)
        for (int dist = minSpawnDistance; dist >= 1; dist--)
        {
            targetNode = BoardObjectiveSelection.FindValidSpawnNode(startNode, dist, maxSpawnDistance, count => Random.Range(0, count));
            if (targetNode != null) break;
        }

        if (targetNode == null)
        {
            Debug.LogError("[MapObjectiveManager] 맵에 조건에 맞는 빈 타일(Normal)이 하나도 없어 스폰에 실패했습니다!");
            return;
        }

        // 논리적 타입 변경
        targetNode.Type = targetType;

        // 시각적 모델 교체 (MapThemeData에 설정된 프리팹을 자동으로 가져옴)
        mapGenerator.ReplaceTileVisual(targetNode);

        // 하이라이트 생성
        GameObject targetView = mapGenerator.GetNodeView(targetNode);
        if (highlightPrefab != null && targetView != null)
        {
            _highlightView.Show(highlightPrefab, targetView.transform, 2f);
        }

        Debug.Log($"[MapObjectiveManager] 퀘스트 목표({targetType}) 등장 성공! 위치: {targetNode.Position}");
    }

}
