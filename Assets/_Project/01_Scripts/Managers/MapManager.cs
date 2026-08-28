using OZGL.Map;
using System.Collections.Generic;
using UnityEngine;

namespace OzGameLab01.Map
{
    public class MapManager : MonoBehaviour
    {
        public static MapManager Instance { get; private set; }

        // MapGenerator가 생성한 데이터를 넘겨받아 보관합니다.
        private Dictionary<Vector2Int, MapNode> _nodeDict = new Dictionary<Vector2Int, MapNode>();

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        // MapGenerator에서 맵 생성이 완료되면 이 함수를 호출하여 데이터를 주입합니다.
        public void InitializeMapData(Dictionary<Vector2Int, MapNode> generatedNodes)
        {
            _nodeDict = generatedNodes;
        }

        public MapNode GetNodeAt(Vector2Int position)
        {
            if (_nodeDict.TryGetValue(position, out MapNode node))
                return node;
            return null;
        }

        public bool IsObstacle(NodeType type)
        {
            return type == NodeType.Tree || type == NodeType.Rock ||
                   type == NodeType.WaterPuddle || type == NodeType.WaterStart ||
                   type == NodeType.WaterBody || type == NodeType.WaterEnd;
        }

        // 시작점에서 목표점까지 주사위 값 내에 갈 수 있는 최단 경로를 반환합니다.
        public List<MapNode> FindPath(MapNode startNode, MapNode targetNode, int maxDistance)
        {
            if (startNode == targetNode || IsObstacle(targetNode.Type)) return null;

            Queue<MapNode> queue = new Queue<MapNode>();
            Dictionary<MapNode, MapNode> cameFrom = new Dictionary<MapNode, MapNode>();
            Dictionary<MapNode, int> costSoFar = new Dictionary<MapNode, int>();

            queue.Enqueue(startNode);
            cameFrom[startNode] = null;
            costSoFar[startNode] = 0;

            while (queue.Count > 0)
            {
                MapNode current = queue.Dequeue();

                if (current == targetNode) break; // 목표 도달

                foreach (MapNode next in current.ConnectedNodes)
                {
                    if (IsObstacle(next.Type)) continue; // 장애물 통과 불가

                    int newCost = costSoFar[current] + 1;

                    // 주사위 한계 거리를 초과하면 탐색 안 함
                    if (newCost > maxDistance) continue;

                    if (!costSoFar.ContainsKey(next) || newCost < costSoFar[next])
                    {
                        costSoFar[next] = newCost;
                        cameFrom[next] = current;
                        queue.Enqueue(next);
                    }
                }
            }

            // 목표점까지 도달하지 못했다면 null 반환
            if (!cameFrom.ContainsKey(targetNode)) return null;

            // 역추적하여 경로 리스트 생성
            List<MapNode> path = new List<MapNode>();
            MapNode curr = targetNode;
            while (curr != startNode)
            {
                path.Add(curr);
                curr = cameFrom[curr];
            }
            path.Reverse(); // start -> target 순서로 정렬

            return path;
        }
    }
}