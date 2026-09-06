using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace OZGL.Map
{
    /// <summary>
    /// 기본 MapGenerator의 랜덤 타일 배치 뒤에 콘텐츠 공백을 보정하는 비교용 생성기입니다.
    /// 거리 구간과 시작점에서 멀리 뻗은 분기 경로에 전투 또는 이벤트를 추가 배치합니다.
    /// </summary>
    [AddComponentMenu("OZGL/Map/Map Coverage Generator")]
    public sealed class MapCoverageGenerator : MapGenerator
    {
        [Header("Interaction Coverage")]
        [Tooltip("상호작용 타일 없이 허용하는 최대 이동 칸 수입니다.")]
        [Min(1)] [SerializeField] private int maximumStepsWithoutInteraction = 8;

        [Tooltip("이 거리 이상 떨어진 주요 분기 경로만 보정 대상으로 사용합니다.")]
        [Min(1)] [SerializeField] private int minimumBranchDepth = 10;

        [Tooltip("한 맵에서 검사할 가장 먼 분기 경로의 최대 수입니다.")]
        [Min(1)] [SerializeField] private int maximumBranchPaths = 16;

        [Tooltip("보정으로 추가되는 상호작용 타일 중 Event가 될 비율입니다. 나머지는 Battle입니다.")]
        [Range(0f, 1f)] [SerializeField] private float eventTileRatio = 0.6f;

        protected override void ApplyPostGenerationRules()
        {
            MapNode startNode = AllNodes.FirstOrDefault(node => node.Type == NodeType.Start);
            if (startNode == null)
            {
                Debug.LogWarning("[MapCoverageGenerator] Start 노드가 없어 커버리지 보정을 건너뜁니다.", this);
                return;
            }

            BuildTraversalData(startNode, out Dictionary<MapNode, int> distances, out Dictionary<MapNode, MapNode> parents);

            int repairedBands = RepairDistanceBands(distances);
            int repairedBranches = RepairMajorBranches(distances, parents);

            Debug.Log(
                $"[MapCoverageGenerator] 콘텐츠 커버리지 보정 완료 | " +
                $"거리 구간: {repairedBands}, 분기 경로: {repairedBranches}",
                this);
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
                    if (IsObstacle(neighbor.Type) || distances.ContainsKey(neighbor))
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
                if (beforeIndex > 0 && path[beforeIndex].Type == NodeType.Normal)
                {
                    return path[beforeIndex];
                }

                int afterIndex = centerIndex + offset;
                if (afterIndex < path.Count && path[afterIndex].Type == NodeType.Normal)
                {
                    return path[afterIndex];
                }
            }

            return null;
        }

        private MapNode SelectBestNormalNode(IEnumerable<MapNode> nodes)
        {
            return nodes
                .Where(node => node.Type == NodeType.Normal)
                .OrderByDescending(GetWalkableNeighborCount)
                .FirstOrDefault();
        }

        private int GetWalkableNeighborCount(MapNode node)
        {
            return node.ConnectedNodes.Count(neighbor => !IsObstacle(neighbor.Type));
        }

        private void PromoteToInteraction(MapNode node)
        {
            node.Type = Random.value < eventTileRatio ? NodeType.Event : NodeType.Battle;
        }

        private static bool IsInteractionNode(MapNode node)
        {
            return node.Type == NodeType.Battle ||
                   node.Type == NodeType.Event ||
                   node.Type == NodeType.Shop ||
                   node.Type == NodeType.UnitAcquisition ||
                   node.Type == NodeType.Elite ||
                   node.Type == NodeType.Boss;
        }
    }
}
