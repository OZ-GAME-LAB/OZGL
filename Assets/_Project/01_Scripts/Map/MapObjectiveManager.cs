using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using OzGameLab01.Data;
using OZGL.Map;

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

    private GameObject _currentHighlight;

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
        if (BoardRunData.DefeatedElitesCount > maxElites) return;

        NodeType targetType = (BoardRunData.DefeatedElitesCount == maxElites) ? NodeType.Boss : NodeType.Elite;

        Vector2Int currentPos = BoardRunData.HasPlayerPosition ? BoardRunData.PlayerPosition : Vector2Int.zero;
        if (!mapGenerator.NodeDict.TryGetValue(currentPos, out MapNode startNode)) return;

        MapNode targetNode = null;

        // 3. 거리 조건 완화: 최소 거리 조건에 맞는 타일이 없으면 9칸, 8칸... 계속 줄여나가며 찾아냄!
        // (단, maxSpawnDistance 이내여야 함)
        for (int dist = minSpawnDistance; dist >= 1; dist--)
        {
            targetNode = FindValidSpawnNode(startNode, dist, maxSpawnDistance);
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
        if (highlightPrefab != null)
        {
            if (_currentHighlight != null) Destroy(_currentHighlight);
            _currentHighlight = Instantiate(highlightPrefab, targetNode.NodeView.transform);
            _currentHighlight.transform.localPosition = Vector3.up * 2f;
        }

        Debug.Log($"[MapObjectiveManager] 퀘스트 목표({targetType}) 등장 성공! 위치: {targetNode.Position}");
    }

    // [수정됨] maxDistance 매개변수가 추가되었습니다.
    private MapNode FindValidSpawnNode(MapNode startNode, int minDistance, int maxDistance)
    {
        Queue<MapNode> queue = new Queue<MapNode>();
        Dictionary<MapNode, int> distances = new Dictionary<MapNode, int>();
        List<MapNode> validCandidates = new List<MapNode>();

        queue.Enqueue(startNode);
        distances[startNode] = 0;

        while (queue.Count > 0)
        {
            MapNode current = queue.Dequeue();
            int currentDist = distances[current];

            // 최대 거리를 초과하면 더 이상 깊게 탐색할 필요가 없으므로 가지치기(Cut-off) 합니다.
            if (currentDist > maxDistance) continue;

            if (currentDist >= minDistance && currentDist <= maxDistance && current.Type == NodeType.Normal)
            {
                validCandidates.Add(current);
            }

            foreach (MapNode neighbor in current.ConnectedNodes)
            {
                bool isObstacle = neighbor.Type == NodeType.Tree || neighbor.Type == NodeType.Rock ||
                                  neighbor.Type == NodeType.WaterPuddle || neighbor.Type == NodeType.WaterStart ||
                                  neighbor.Type == NodeType.WaterBody || neighbor.Type == NodeType.WaterEnd;

                if (isObstacle) continue;

                if (!distances.ContainsKey(neighbor))
                {
                    distances[neighbor] = currentDist + 1;
                    queue.Enqueue(neighbor);
                }
            }
        }

        if (validCandidates.Count > 0)
            return validCandidates[Random.Range(0, validCandidates.Count)];

        return null;
    }
}