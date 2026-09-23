using System;
using System.Collections.Generic;
using System.Linq;
using OzGameLab01.Map;
using UnityEngine;
using Random = UnityEngine.Random;

namespace OzGameLab01.Board.Models
{
    /// <summary>
    /// 화면과 런 저장소를 참조하지 않고 지형 및 타일 배치 규칙을 처리합니다.
    /// </summary>
    public sealed class BoardMapModel
    {
        private const int ObstacleClusterPlacementAttempts = 100;
        private readonly Dictionary<Vector2Int, MapNode> _nodeDict;
        private readonly List<MapNode> _allNodes;
        private readonly BoardMapSettings _settings;
        private readonly Action<string> _warning;
        private readonly Action<string> _error;

        public BoardMapModel(Dictionary<Vector2Int, MapNode> nodes, List<MapNode> allNodes, BoardMapSettings settings, Action<string> warning, Action<string> error)
        {
            _nodeDict = nodes;
            _allNodes = allNodes;
            _settings = settings;
            _warning = warning;
            _error = error;
        }

        public void GenerateLogicalShape()
        {
            Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

            float offsetX = Random.Range(-10000f, 10000f);
            float offsetY = Random.Range(-10000f, 10000f);

            Vector2Int startPos = Vector2Int.zero;
            MapNode startNode = new MapNode { Position = startPos, Type = NodeType.Start };
            _nodeDict.Add(startPos, startNode);
            _allNodes.Add(startNode);

            List<Vector2Int> candidates = new List<Vector2Int>();
            foreach (Vector2Int dir in directions)
            {
                candidates.Add(startPos + dir);
            }

            while (_allNodes.Count < _settings.totalNodeCount && candidates.Count > 0)
            {
                int bestIndex = -1;
                float bestScore = float.MinValue;

                for (int i = 0; i < candidates.Count; i++)
                {
                    Vector2Int pos = candidates[i];

                    float distFromCenter = Vector2.Distance(Vector2.zero, pos);

                    if (distFromCenter > _settings.maxRadius)
                        continue;

                    float pX = pos.x * _settings.noiseScale + offsetX;
                    float pY = pos.y * _settings.noiseScale + offsetY;
                    float noiseVal = Mathf.PerlinNoise(pX, pY);
                    float falloff = Mathf.Clamp01(distFromCenter / _settings.maxRadius);

                    // [핵심 로직] 지정한 코어 반경(_settings.coreRadius) 안쪽은 노이즈 점수를 무시하고 엄청난 가산점(+10점)을 부여하여 무조건 꽉 채웁니다!
                    float coreBonus = (distFromCenter <= _settings.coreRadius) ? 10f : 0f;
                    float score = noiseVal - (falloff * _settings.edgeFalloffStrength) + coreBonus;

                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestIndex = i;
                    }
                }

                if (bestIndex == -1) break;

                Vector2Int bestPos = candidates[bestIndex];
                candidates.RemoveAt(bestIndex);

                if (!_nodeDict.ContainsKey(bestPos))
                {
                    MapNode newNode = new MapNode { Position = bestPos, Type = NodeType.Normal };
                    _nodeDict.Add(bestPos, newNode);
                    _allNodes.Add(newNode);

                    foreach (Vector2Int dir in directions)
                    {
                        Vector2Int neighborPos = bestPos + dir;

                        if (_nodeDict.TryGetValue(neighborPos, out MapNode neighbor))
                        {
                            if (!newNode.ConnectedNodes.Contains(neighbor))
                                newNode.ConnectedNodes.Add(neighbor);
                            if (!neighbor.ConnectedNodes.Contains(newNode))
                                neighbor.ConnectedNodes.Add(newNode);
                        }
                        else
                        {
                            if (!candidates.Contains(neighborPos))
                                candidates.Add(neighborPos);
                        }
                    }
                }
            }
        }

        // 맵의 모든 타일이 시작점으로부터 몇 걸음 떨어져 있는지(Depth) 저장할 캐시
        private Dictionary<MapNode, int> _nodeDepths = new Dictionary<MapNode, int>();

        public void AssignNodeTypes()
        {
            List<MapNode> availableNodes = new List<MapNode>(_allNodes);
            availableNodes.RemoveAll(n => n.Type == NodeType.Start);
            if (availableNodes.Count == 0) return;

            MapNode startNode = _allNodes.FirstOrDefault(node => node.Type == NodeType.Start);
            if (startNode == null)
            {
                _error("[MapGenerator3] Start 타일이 없어 노드 타입을 배치할 수 없습니다!");
                return;
            }

            // 1. 장애물 먼저 배치하되, Start 기준 이동 가능 영역이 분리되지 않는 후보만 확정합니다.
            PlaceObstacleClusters(availableNodes, startNode);

            // [추가됨] 강제 시작 동선 셋팅 (시작 타일 3면 차단, 1면 유닛 확정 획득)
            if (_settings.forceUnitAtStart)
            {
                ConfigureForcedStartPath(startNode, availableNodes);
            }

            // 2. Start 타일로부터 맵 전체의 걸음 수(Depth)를 한 번 계산하여 캐싱합니다.
            CalculateAllNodeDepths(startNode);
            ValidateWalkableConnectivity();

            // 3. 일반 상호작용 타일을 배치합니다.
            // Elite와 Boss 목표는 MapRouteDirector가 진행 상태에 맞춰 별도로 배치합니다.
            PlaceNodesOfType(NodeType.UnitAcquisition, _settings.unitAcquisitionCount, _settings.minUnitAcquisitionDistance, availableNodes, _settings.minUnitAcquisitionDistFromStart, _settings.maxUnitAcquisitionDistFromStart, false);

            PlaceNodesOfType(NodeType.Shop, _settings.shopCount, _settings.minShopDistance, availableNodes, _settings.minShopDistFromStart, _settings.maxShopDistFromStart, false);
            PlaceNodesOfType(NodeType.Event, _settings.eventCount, _settings.minEventDistance, availableNodes, _settings.minEventDistFromStart, _settings.maxEventDistFromStart, false);
            PlaceNodesOfType(NodeType.Battle, _settings.battleCount, _settings.minBattleDistance, availableNodes, _settings.minBattleDistFromStart, _settings.maxBattleDistFromStart, false);
        }

        // Start 타일로부터 맵 전체로 퍼져나가며 모든 타일의 '실제 도달 걸음 수'를 기록합니다.
        private void CalculateAllNodeDepths(MapNode startNode)
        {
            _nodeDepths.Clear();
            if (startNode == null) return;
            Queue<MapNode> queue = new Queue<MapNode>();
            queue.Enqueue(startNode);
            _nodeDepths[startNode] = 0;
            while (queue.Count > 0)
            {
                MapNode curr = queue.Dequeue();
                int currentDepth = _nodeDepths[curr];
                foreach (MapNode neighbor in curr.ConnectedNodes)
                {
                    if (IsObstacle(neighbor.Type)) continue;
                    if (!_nodeDepths.ContainsKey(neighbor))
                    {
                        _nodeDepths[neighbor] = currentDepth + 1;
                        queue.Enqueue(neighbor);
                    }
                }
            }

        }


        private void PlaceObstacleClusters(List<MapNode> availableNodes, MapNode startNode)
        {
            if (_settings.hasTreePrefabs)
                CreateCluster(availableNodes, startNode, _settings.treeClusterCount, _settings.minTreeClusterSize, _settings.maxTreeClusterSize, NodeType.Tree, false);

            if (_settings.hasRockPrefabs)
                CreateCluster(availableNodes, startNode, _settings.rockClusterCount, _settings.minRockClusterSize, _settings.maxRockClusterSize, NodeType.Rock, false);

            if (_settings.hasWaterPrefabs)
                CreateCluster(availableNodes, startNode, _settings.waterClusterCount, _settings.minWaterClusterSize, _settings.maxWaterClusterSize, NodeType.WaterPuddle, true);
        }

        private void CreateCluster(
            List<MapNode> availableNodes,
            MapNode startNode,
            int count,
            int minSize,
            int maxSize,
            NodeType baseType,
            bool isWater)
        {
            int safeMinSize = Mathf.Max(1, minSize);
            int safeMaxSize = Mathf.Max(safeMinSize, maxSize);

            for (int clusterIndex = 0; clusterIndex < count; clusterIndex++)
            {
                if (availableNodes.Count < safeMinSize) break;

                int requestedSize = Random.Range(safeMinSize, safeMaxSize + 1);
                int largestPossibleSize = Mathf.Min(requestedSize, availableNodes.Count);
                bool wasPlaced = false;

                // 요청 크기의 안전한 위치가 없으면 최소 크기까지 단계적으로 축소합니다.
                for (int targetSize = largestPossibleSize;
                     targetSize >= safeMinSize && !wasPlaced;
                     targetSize--)
                {
                    for (int attempt = 0;
                         attempt < ObstacleClusterPlacementAttempts && !wasPlaced;
                         attempt++)
                    {
                        List<MapNode> cluster = BuildClusterCandidate(availableNodes, targetSize);
                        if (cluster.Count != targetSize)
                        {
                            continue;
                        }

                        HashSet<MapNode> proposedObstacles = cluster.ToHashSet();
                        if (!KeepsWalkableMapConnected(startNode, proposedObstacles))
                        {
                            continue;
                        }

                        CommitObstacleCluster(availableNodes, cluster, baseType, isWater);
                        wasPlaced = true;
                    }
                }

                if (!wasPlaced)
                {
                    _warning(
                        $"[MapGenerator3] 연결성을 유지할 수 없어 {baseType} 장애물 클러스터 " +
                        $"{clusterIndex + 1}/{count} 배치를 생략했습니다.");
                }
            }
        }

        private List<MapNode> BuildClusterCandidate(List<MapNode> availableNodes, int targetSize)
        {
            MapNode seed = availableNodes[Random.Range(0, availableNodes.Count)];
            HashSet<MapNode> availableSet = availableNodes.ToHashSet();
            HashSet<MapNode> visited = new HashSet<MapNode> { seed };
            Queue<MapNode> queue = new Queue<MapNode>();
            List<MapNode> cluster = new List<MapNode>(targetSize);

            queue.Enqueue(seed);

            while (queue.Count > 0 && cluster.Count < targetSize)
            {
                MapNode current = queue.Dequeue();
                if (!availableSet.Contains(current))
                {
                    continue;
                }

                cluster.Add(current);

                List<MapNode> neighbors = current.ConnectedNodes
                    .Where(availableSet.Contains)
                    .ToList();
                ShuffleList(neighbors);

                foreach (MapNode neighbor in neighbors)
                {
                    if (visited.Add(neighbor))
                    {
                        queue.Enqueue(neighbor);
                    }
                }
            }

            return cluster;
        }

        private void CommitObstacleCluster(
            List<MapNode> availableNodes,
            List<MapNode> cluster,
            NodeType baseType,
            bool isWater)
        {
            foreach (MapNode node in cluster)
            {
                availableNodes.Remove(node);
            }

            if (!isWater)
            {
                foreach (MapNode node in cluster)
                {
                    node.Type = baseType;
                }

                return;
            }

            if (cluster.Count == 1)
            {
                cluster[0].Type = NodeType.WaterPuddle;
                return;
            }

            cluster[0].Type = NodeType.WaterStart;
            cluster[cluster.Count - 1].Type = NodeType.WaterEnd;

            for (int index = 1; index < cluster.Count - 1; index++)
            {
                cluster[index].Type = NodeType.WaterBody;
            }
        }

        private void ConfigureForcedStartPath(MapNode startNode, List<MapNode> availableNodes)
        {
            List<MapNode> unitCandidates = startNode.ConnectedNodes
                .Where(node => node.Type == NodeType.Normal)
                .ToList();

            if (unitCandidates.Count == 0)
            {
                _warning(
                    "[MapGenerator3] 시작 타일 주변에 유닛 획득 타일로 사용할 수 있는 노드가 없습니다.");
                return;
            }

            ShuffleList(unitCandidates);

            foreach (MapNode unitNode in unitCandidates)
            {
                HashSet<MapNode> proposedRocks = unitCandidates
                    .Where(node => node != unitNode)
                    .ToHashSet();

                if (!KeepsWalkableMapConnected(startNode, proposedRocks))
                {
                    continue;
                }

                unitNode.Type = NodeType.UnitAcquisition;
                unitNode.IsMandatoryStop = true; // [추가됨] 강제 시작 타일은 무조건 멈춤 처리
                availableNodes.Remove(unitNode);

                foreach (MapNode rockNode in proposedRocks)
                {
                    rockNode.Type = NodeType.Rock;
                    availableNodes.Remove(rockNode);
                }

                return;
            }

            // 모든 방향에서 3면 차단이 맵을 분리한다면 유닛 획득 타일만 보장합니다.
            MapNode fallbackUnitNode = unitCandidates[0];
            fallbackUnitNode.Type = NodeType.UnitAcquisition;
            fallbackUnitNode.IsMandatoryStop = true; // [추가됨] 강제 시작 타일은 무조건 멈춤 처리
            availableNodes.Remove(fallbackUnitNode);

            _warning(
                "[MapGenerator3] 시작 지점의 3면 차단이 맵 연결성을 해쳐 바위 배치를 생략했습니다.");
        }

        private bool KeepsWalkableMapConnected(
            MapNode startNode,
            HashSet<MapNode> proposedObstacles)
        {
            if (startNode == null || proposedObstacles.Contains(startNode))
            {
                return false;
            }

            HashSet<MapNode> visited = new HashSet<MapNode> { startNode };
            Queue<MapNode> queue = new Queue<MapNode>();
            queue.Enqueue(startNode);

            while (queue.Count > 0)
            {
                MapNode current = queue.Dequeue();

                foreach (MapNode neighbor in current.ConnectedNodes)
                {
                    if (visited.Contains(neighbor) ||
                        IsObstacle(neighbor.Type) ||
                        proposedObstacles.Contains(neighbor))
                    {
                        continue;
                    }

                    visited.Add(neighbor);
                    queue.Enqueue(neighbor);
                }
            }

            int expectedWalkableCount = _allNodes.Count(node =>
                !IsObstacle(node.Type) &&
                !proposedObstacles.Contains(node));

            return visited.Count == expectedWalkableCount;
        }

        private void ValidateWalkableConnectivity()
        {
            int walkableNodeCount = _allNodes.Count(node => !IsObstacle(node.Type));
            if (_nodeDepths.Count == walkableNodeCount)
            {
                return;
            }

            _error(
                $"[MapGenerator3] 장애물 배치 후 이동 가능 영역이 분리되었습니다. " +
                $"도달 가능: {_nodeDepths.Count}, 전체 이동 가능: {walkableNodeCount}");
        }

        private void ShuffleList<T>(List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int randomIndex = Random.Range(0, i + 1);
                T temp = list[i];
                list[i] = list[randomIndex];
                list[randomIndex] = temp;
            }
        }

        public static bool IsObstacle(NodeType type)
        {
            return type == NodeType.Tree || type == NodeType.Rock ||
                   type == NodeType.WaterPuddle || type == NodeType.WaterStart ||
                   type == NodeType.WaterBody || type == NodeType.WaterEnd;
        }

        // [수정됨] 파라미터에 maxDistanceFromStart 가 추가되었습니다.
        private void PlaceNodesOfType(NodeType type, int count, int minDistance, List<MapNode> availableNodes, int minDistanceFromStart = 0, int maxDistanceFromStart = 999, bool isSequential = false)
        {
            List<MapNode> placedNodes = new List<MapNode>();
            int currentCount = 0;
            int maxAttempts = 1000;
            int attempts = 0;
            while (currentCount < count && availableNodes.Count > 0 && attempts < maxAttempts)
            {
                attempts++;
                MapNode candidate = availableNodes[Random.Range(0, availableNodes.Count)];
                bool isValid = true;

                // 1. 고립 검사 (사방이 막혔는지)
                int walkableNeighbors = 0;
                foreach (MapNode neighbor in candidate.ConnectedNodes)
                {
                    if (!IsObstacle(neighbor.Type)) walkableNeighbors++;
                }
                if (walkableNeighbors == 0) isValid = false;

                // 2. 점진적 깊이(Depth) 검사 (미리 계산해둔 캐시 사용)
                if (isValid)
                {
                    if (_nodeDepths.TryGetValue(candidate, out int candidateDepth))
                    {
                        // 순차 배치(isSequential)가 켜져 있으면, 배치될 때마다 요구 거리가 증가합니다!
                        int requiredDepth = minDistanceFromStart;
                        if (isSequential)
                        {
                            requiredDepth += (currentCount * minDistance);
                        }

                        // [추가됨] 타일이 허용된 범위를 벗어나는지 (너무 가깝거나 너무 멀지 않은지) 검사합니다.
                        if (candidateDepth < requiredDepth || candidateDepth > maxDistanceFromStart) isValid = false;
                    }
                    else
                    {
                        isValid = false; // 아예 도달 불가능한 타일
                    }
                }

                // 3. 동종 타일 간의 최소 거리 확보 (서로 뭉치지 않게 BFS 탐색)
                if (isValid && placedNodes.Count > 0)
                {
                    isValid = CheckDistanceToPlacedNodes(candidate, placedNodes, minDistance);
                }

                if (isValid)
                {
                    candidate.Type = type;
                    placedNodes.Add(candidate);
                    availableNodes.Remove(candidate);
                    currentCount++;
                }
            }
            if (currentCount < count)
            {
                _warning($"[MapGenerator3] {type} 타일을 목표치({count}개)만큼 배치하지 못했습니다. (배치됨: {currentCount}개)");
            }
        }

        // 특정 노드(candidate)에서 이미 배치된 타일들(placedNodes)까지의 거리가 허용 반경 내에 있는지 BFS로 검사
        private bool CheckDistanceToPlacedNodes(MapNode candidate, List<MapNode> placedNodes, int minDistance)
        {
            if (minDistance <= 0) return true;
            Queue<MapNode> queue = new Queue<MapNode>();
            Dictionary<MapNode, int> distances = new Dictionary<MapNode, int>();
            queue.Enqueue(candidate);
            distances[candidate] = 0;
            while (queue.Count > 0)
            {
                MapNode current = queue.Dequeue();
                int currentDist = distances[current];
                // 이미 배치된 타일과 너무 가까우면 탈락
                if (placedNodes.Contains(current) && currentDist < minDistance)
                {
                    return false;
                }
                // 최소 거리만큼 벌어졌음이 확인되면 이 방향은 안전함 (탐색 중지)
                if (currentDist >= minDistance) continue;
                foreach (MapNode neighbor in current.ConnectedNodes)
                {
                    if (IsObstacle(neighbor.Type)) continue;
                    if (!distances.ContainsKey(neighbor))
                    {
                        distances[neighbor] = currentDist + 1;
                        queue.Enqueue(neighbor);
                    }
                }
            }
            return true;
        }


    }
}
