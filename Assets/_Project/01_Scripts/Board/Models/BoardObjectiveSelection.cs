using System.Collections.Generic;
using OzGameLab01.Map;

namespace OzGameLab01.Board.Models
{
    /// <summary>
    /// 기존 목표 배치 방식의 거리 후보와 우선순위를 계산합니다.
    /// </summary>
    public static class BoardObjectiveSelection
    {
        // 기존 최대 거리 제한 및 후보 우선순위 유지
        public static MapNode FindValidSpawnNode(MapNode startNode, int minDistance, int maxDistance, System.Func<int, int> chooseIndex)
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
            {
                // 맵의 '깊은 곳'으로 향하도록 유도 (스타트 지점 0,0에서 가장 먼 노드를 우선순위로 정렬)
                validCandidates.Sort((a, b) =>
                {
                    int distA = a.Position.x + a.Position.y;
                    int distB = b.Position.x + b.Position.y;
                    return distB.CompareTo(distA); // 내림차순 (가장 먼 곳이 0번 인덱스)
                });

                // 가장 먼 곳 위주로 선택하되, 약간의 무작위성을 위해 상위 3개 중 하나를 고름
                int maxIndex = UnityEngine.Mathf.Min(3, validCandidates.Count);
                return validCandidates[chooseIndex(maxIndex)];
            }

            return null;
        }
    }
}
