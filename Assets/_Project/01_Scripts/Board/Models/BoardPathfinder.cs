using System.Collections.Generic;
using OzGameLab01.Map;

namespace OzGameLab01.Board.Models
{
    /// <summary>
    /// 노드 연결과 이동 한도를 기준으로 최단 경로를 계산합니다.
    /// </summary>
    public static class BoardPathfinder
    {
        public static bool IsObstacle(NodeType type)
        {
            return type == NodeType.Tree || type == NodeType.Rock || type == NodeType.WaterPuddle || type == NodeType.WaterStart || type == NodeType.WaterBody || type == NodeType.WaterEnd;
        }

        public static List<MapNode> FindPath(MapNode start, MapNode target, int maxDistance)
        {
            if (start == null || target == null || start == target || maxDistance <= 0 || IsObstacle(target.Type))
            {
                return null;
            }
            Queue<MapNode> queue = new Queue<MapNode>();
            Dictionary<MapNode, MapNode> previous = new Dictionary<MapNode, MapNode>();
            Dictionary<MapNode, int> distance = new Dictionary<MapNode, int>();
            queue.Enqueue(start);
            previous[start] = null;
            distance[start] = 0;
            while (queue.Count > 0)
            {
                MapNode current = queue.Dequeue();
                if (current == target)
                {
                    break;
                }
                foreach (MapNode next in current.ConnectedNodes)
                {
                    if (next == null || IsObstacle(next.Type) || previous.ContainsKey(next))
                    {
                        continue;
                    }
                    int cost = distance[current] + 1;
                    if (cost > maxDistance)
                    {
                        continue;
                    }
                    previous[next] = current;
                    distance[next] = cost;
                    queue.Enqueue(next);
                }
            }
            if (!previous.ContainsKey(target))
            {
                return null;
            }
            List<MapNode> path = new List<MapNode>();
            for (MapNode node = target; node != start; node = previous[node])
            {
                path.Add(node);
            }
            path.Reverse();
            return path;
        }
    }
}
