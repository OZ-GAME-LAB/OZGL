using System.Collections.Generic;
using UnityEngine;
using OzGameLab01.Board.Models;

namespace OzGameLab01.Map
{
    /// <summary>
    /// 기존 맵 조회 API를 유지하고 경로 계산을 독립 모델에 위임합니다.
    /// </summary>
    public class MapManager : MonoBehaviour
    {
        public static MapManager Instance { get; private set; }
        private Dictionary<Vector2Int, MapNode> _nodeDict = new Dictionary<Vector2Int, MapNode>();

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void InitializeMapData(Dictionary<Vector2Int, MapNode> generatedNodes)
        {
            _nodeDict = generatedNodes ?? new Dictionary<Vector2Int, MapNode>();
        }

        public MapNode GetNodeAt(Vector2Int position)
        {
            _nodeDict.TryGetValue(position, out MapNode node);
            return node;
        }

        public bool IsObstacle(NodeType type)
        {
            return BoardPathfinder.IsObstacle(type);
        }

        public List<MapNode> FindPath(MapNode startNode, MapNode targetNode, int maxDistance)
        {
            return BoardPathfinder.FindPath(startNode, targetNode, maxDistance);
        }

        /// <summary>
        /// 시작 노드로부터 특정 거리(행동력) 내에 도달할 수 있는 모든 노드를 찾아 반환합니다.
        /// </summary>
        public HashSet<MapNode> GetReachableNodes(MapNode startNode, int maxDistance)
        {
            var reachable = new HashSet<MapNode>();
            if (startNode == null) return reachable;
            var queue = new Queue<MapNode>();
            var costSoFar = new Dictionary<MapNode, int>();
            queue.Enqueue(startNode);
            costSoFar[startNode] = 0;
            reachable.Add(startNode);
            while (queue.Count > 0)
            {
                MapNode current = queue.Dequeue();
                int currentCost = costSoFar[current];
                // 최대 행동력에 도달한 노드라면, 더 이상 뻗어나가지 않음
                if (currentCost >= maxDistance) continue;
                // 상하좌우 4방향 탐색
                Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
                foreach (Vector2Int dir in directions)
                {
                    Vector2Int nextPos = current.Position + dir;

                    // MapManager가 가지고 있는 _nodeDict를 통해 이웃 타일 확인
                    if (_nodeDict.TryGetValue(nextPos, out MapNode nextNode))
                    {
                        // 장애물이 아닌 경우에만 이동 가능
                        if (!IsObstacle(nextNode.Type))
                        {
                            int newCost = currentCost + 1;
                            if (!costSoFar.ContainsKey(nextNode) || newCost < costSoFar[nextNode])
                            {
                                costSoFar[nextNode] = newCost;
                                queue.Enqueue(nextNode);
                                reachable.Add(nextNode);
                            }
                        }
                    }
                }
            }
            return reachable;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}