using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using OzGameLab01.Map;
namespace OzGameLab01.Board.Models
{
    // 상호작용 공백 보정 규칙
    public sealed class BoardCoverageModel
    {
        private readonly int maximumStepsWithoutInteraction;
        private readonly int minimumBranchDepth;
        private readonly int maximumBranchPaths;
        private readonly float eventTileRatio;
        private readonly HashSet<Vector2Int> _consumed;
        public BoardCoverageModel(int maximumSteps, int branchDepth, int branchPaths, float eventRatio, IEnumerable<Vector2Int> consumed)
        {
            maximumStepsWithoutInteraction = maximumSteps;
            minimumBranchDepth = branchDepth;
            maximumBranchPaths = branchPaths;
            eventTileRatio = eventRatio;
            _consumed = new HashSet<Vector2Int>(consumed);
        }
        public void Repair(MapNode start, out int bands, out int branches)
        {
            BuildTraversalData(start, out var distances, out var parents);
            bands = RepairDistanceBands(distances);
            branches = RepairMajorBranches(distances, parents);
        }
        private void BuildTraversalData(
            MapNode startNode,
            out Dictionary<MapNode, int> distances,
            out Dictionary<MapNode, MapNode> parents)
        {
            distances = new Dictionary<MapNode, int> { [startNode] = 0 };
            parents = new Dictionary<MapNode, MapNode>();
            Queue<MapNode> queue = new Queue<MapNode>();
            queue.Enqueue(startNode);

            while (queue.Count > 0)
            {
                MapNode current = queue.Dequeue();
                int nextDistance = distances[current] + 1;

                foreach (MapNode neighbor in current.ConnectedNodes)
                {
                    if (BoardPathfinder.IsObstacle(neighbor.Type) || distances.ContainsKey(neighbor))
                    {
                        continue;
                    }

                    distances.Add(neighbor, nextDistance);
                    parents.Add(neighbor, current);
                    queue.Enqueue(neighbor);
                }
            }
        }

        private int RepairDistanceBands(Dictionary<MapNode, int> distances)
        {
            Dictionary<int, List<MapNode>> nodesByBand = new Dictionary<int, List<MapNode>>();

            foreach (KeyValuePair<MapNode, int> pair in distances)
            {
                if (pair.Value == 0)
                {
                    continue;
                }

                int band = (pair.Value - 1) / maximumStepsWithoutInteraction;
                if (!nodesByBand.TryGetValue(band, out List<MapNode> nodes))
                {
                    nodes = new List<MapNode>();
                    nodesByBand.Add(band, nodes);
                }

                nodes.Add(pair.Key);
            }

            int repairedCount = 0;
            foreach (List<MapNode> nodes in nodesByBand.Values)
            {
                if (nodes.Any(IsInteractionNode))
                {
                    continue;
                }

                MapNode candidate = SelectBestNormalNode(nodes);
                if (candidate == null)
                {
                    continue;
                }

                PromoteToInteraction(candidate);
                repairedCount++;
            }

            return repairedCount;
        }

        private int RepairMajorBranches(
            Dictionary<MapNode, int> distances,
            Dictionary<MapNode, MapNode> parents)
        {
            List<MapNode> branchEnds = distances
                .Where(pair => pair.Value >= minimumBranchDepth && GetWalkableNeighborCount(pair.Key) <= 2)
                .OrderByDescending(pair => pair.Value)
                .Take(maximumBranchPaths)
                .Select(pair => pair.Key)
                .ToList();

            // 외곽 노드가 모두 넓게 연결된 맵에서도 가장 먼 경로들을 검사합니다.
            if (branchEnds.Count == 0)
            {
                branchEnds = distances
                    .Where(pair => pair.Value >= minimumBranchDepth)
                    .OrderByDescending(pair => pair.Value)
                    .Take(maximumBranchPaths)
                    .Select(pair => pair.Key)
                    .ToList();
            }

            int repairedCount = 0;
            foreach (MapNode branchEnd in branchEnds)
            {
                List<MapNode> path = BuildPathToStart(branchEnd, parents);
                repairedCount += RepairInteractionGapsOnPath(path);
            }

            return repairedCount;
        }

        private List<MapNode> BuildPathToStart(MapNode endNode, Dictionary<MapNode, MapNode> parents)
        {
            List<MapNode> path = new List<MapNode> { endNode };
            MapNode current = endNode;

            while (parents.TryGetValue(current, out MapNode parent))
            {
                path.Add(parent);
                current = parent;
            }

            path.Reverse();
            return path;
        }

        private int RepairInteractionGapsOnPath(List<MapNode> path)
        {
            int repairedCount = 0;
            int lastInteractionIndex = 0;

            for (int index = 1; index < path.Count; index++)
            {
                if (IsInteractionNode(path[index]))
                {
                    lastInteractionIndex = index;
                    continue;
                }

                if (index - lastInteractionIndex < maximumStepsWithoutInteraction)
                {
                    continue;
                }

                MapNode candidate = FindNormalNodeNear(path, index);
                if (candidate == null)
                {
                    continue;
                }

                PromoteToInteraction(candidate);
                lastInteractionIndex = path.IndexOf(candidate);
                repairedCount++;
            }

            return repairedCount;
        }

        private MapNode FindNormalNodeNear(List<MapNode> path, int centerIndex)
        {
            for (int offset = 0; offset < maximumStepsWithoutInteraction; offset++)
            {
                int beforeIndex = centerIndex - offset;
                if (beforeIndex > 0 && IsAvailableNormalNode(path[beforeIndex]))
                {
                    return path[beforeIndex];
                }

                int afterIndex = centerIndex + offset;
                if (afterIndex < path.Count && IsAvailableNormalNode(path[afterIndex]))
                {
                    return path[afterIndex];
                }
            }

            return null;
        }

        private MapNode SelectBestNormalNode(IEnumerable<MapNode> nodes)
        {
            return nodes
                .Where(IsAvailableNormalNode)
                .OrderByDescending(GetWalkableNeighborCount)
                .FirstOrDefault();
        }

        private int GetWalkableNeighborCount(MapNode node)
        {
            return node.ConnectedNodes.Count(neighbor => !BoardPathfinder.IsObstacle(neighbor.Type));
        }

        private void PromoteToInteraction(MapNode node)
        {
            node.Type = Random.value < eventTileRatio ? NodeType.Event : NodeType.Battle;
        }

        private bool IsInteractionNode(MapNode node)
        {
            return !_consumed.Contains(node.Position) &&
                   BoardTileRules.IsSingleUse(node.Type);
        }

        private bool IsAvailableNormalNode(MapNode node)
        {
            return node != null &&
                   node.Type == NodeType.Normal &&
                   !_consumed.Contains(node.Position);
        }
    }
}
