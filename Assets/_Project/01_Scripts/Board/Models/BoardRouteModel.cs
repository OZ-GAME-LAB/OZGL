using System.Collections.Generic;
using OzGameLab01.Map;
using UnityEngine;

namespace OzGameLab01.Board.Models
{
    /// <summary>
    /// 맵과 진행 상태의 스냅샷을 받아 목표 후보와 거리 점수를 계산합니다.
    /// </summary>
    public sealed class BoardRouteModel
    {
        private readonly IReadOnlyDictionary<Vector2Int, MapNode> _nodes;
        private readonly HashSet<Vector2Int> _consumed;
        private readonly int _defeatedCount;
        private readonly BoardRouteSettings _settings;

        public BoardRouteModel(IReadOnlyDictionary<Vector2Int, MapNode> nodes, IEnumerable<Vector2Int> consumed, int defeatedCount, BoardRouteSettings settings)
        {
            _nodes = nodes;
            _consumed = new HashSet<Vector2Int>(consumed);
            _defeatedCount = defeatedCount;
            _settings = settings;
        }

        // 진행 상태 기준 목표 종류 및 후보 결정
        public BoardRouteDecision SelectNext(bool hasPlayerPosition, Vector2Int playerPosition)
        {
            MapNode start = FindStartNode();
            if (start == null) { return new BoardRouteDecision(BoardRouteStatus.MissingStart); }
            var distances = BuildDistanceMap(start);
            MapNode boss = SelectFinalBossNode(start, distances);
            if (boss == null) { return new BoardRouteDecision(BoardRouteStatus.MissingBoss); }
            Vector2Int position = hasPlayerPosition ? playerPosition : start.Position;
            if (!_nodes.TryGetValue(position, out MapNode current) || !IsWalkable(current)) { current = start; }
            NodeType type = _defeatedCount >= _settings.requiredEliteCount ? NodeType.Boss : NodeType.Elite;
            MapNode target = type == NodeType.Boss ? SelectBossForCurrentPosition(current, boss) : SelectEliteNode(current, start, boss, distances);
            return new BoardRouteDecision(target != null ? BoardRouteStatus.Ready : BoardRouteStatus.MissingTarget, boss, target, type);
        }

        public MapNode FindStartNode()
        {
            foreach (MapNode node in _nodes.Values)
            {
                if (node.Type == NodeType.Start)
                {
                    return node;
                }
            }

            _nodes.TryGetValue(Vector2Int.zero, out MapNode fallback);
            return fallback;
        }

        public MapNode SelectFinalBossNode(
            MapNode startNode,
            Dictionary<MapNode, int> distanceFromStart)
        {
            MapNode best = null;
            int bestDistance = -1;

            foreach (KeyValuePair<MapNode, int> pair in distanceFromStart)
            {
                MapNode candidate = pair.Key;
                if (!IsAvailableObjectiveNode(candidate))
                {
                    continue;
                }

                if (pair.Value > bestDistance ||
                    (pair.Value == bestDistance && IsLowerPosition(candidate, best)))
                {
                    best = candidate;
                    bestDistance = pair.Value;
                }
            }

            // 특수 타일만 남은 작은 맵도 작동하도록 마지막 안전장치를 둡니다.
            if (best != null)
            {
                return best;
            }

            foreach (KeyValuePair<MapNode, int> pair in distanceFromStart)
            {
                MapNode candidate = pair.Key;
                if (candidate == startNode ||
                    !IsWalkable(candidate) ||
                    _consumed.Contains(candidate.Position))
                {
                    continue;
                }

                if (pair.Value > bestDistance ||
                    (pair.Value == bestDistance && IsLowerPosition(candidate, best)))
                {
                    best = candidate;
                    bestDistance = pair.Value;
                }
            }

            return best;
        }

        public MapNode SelectBossForCurrentPosition(MapNode currentNode, MapNode plannedBossNode)
        {
            if (IsAvailableObjectiveNode(plannedBossNode))
            {
                Dictionary<MapNode, int> distances = BuildDistanceMap(currentNode);
                if (distances.TryGetValue(plannedBossNode, out int bossDistance) &&
                    bossDistance >= _settings.minimumBossLegDistance)
                {
                    return plannedBossNode;
                }
            }

            // 최종 보스 예약 타일이 너무 가까워졌거나 다른 타입으로 사용된 경우,
            // 현재 위치에서 충분히 떨어진 가장 먼 Normal 타일을 대체 목표로 사용합니다.
            Dictionary<MapNode, int> fromCurrent = BuildDistanceMap(currentNode);
            MapNode fallback = null;
            int furthestDistance = -1;

            foreach (KeyValuePair<MapNode, int> pair in fromCurrent)
            {
                if (!IsAvailableObjectiveNode(pair.Key) || pair.Value < _settings.minimumBossLegDistance)
                {
                    continue;
                }

                if (pair.Value > furthestDistance ||
                    (pair.Value == furthestDistance && IsLowerPosition(pair.Key, fallback)))
                {
                    fallback = pair.Key;
                    furthestDistance = pair.Value;
                }
            }

            return fallback ?? (IsAvailableObjectiveNode(plannedBossNode) ? plannedBossNode : null);
        }

        public MapNode SelectEliteNode(
            MapNode currentNode,
            MapNode startNode,
            MapNode bossNode,
            Dictionary<MapNode, int> distanceFromStart)
        {
            Dictionary<MapNode, int> distanceFromCurrent = BuildDistanceMap(currentNode);
            Dictionary<MapNode, int> distanceToBoss = BuildDistanceMap(bossNode);

            if (!distanceToBoss.TryGetValue(currentNode, out int currentToBossDistance))
            {
                return null;
            }

            int defeatedCount = _defeatedCount;
            int remainingEliteCount = Mathf.Max(0, _settings.requiredEliteCount - defeatedCount - 1);
            int futureMinimumDistance =
                remainingEliteCount * _settings.minimumEliteLegDistance + _settings.minimumBossLegDistance;
            int reserveDistance = Mathf.RoundToInt(futureMinimumDistance * _settings.futureRouteReserveRatio);

            List<CandidateScore> strictCandidates = new List<CandidateScore>();
            List<CandidateScore> relaxedCandidates = new List<CandidateScore>();

            foreach (KeyValuePair<MapNode, int> pair in distanceFromCurrent)
            {
                MapNode candidate = pair.Key;
                int distanceFromCurrentNode = pair.Value;

                if (!IsAvailableObjectiveNode(candidate) || candidate == bossNode ||
                    distanceFromCurrentNode < _settings.minimumEliteLegDistance ||
                    distanceFromCurrentNode > _settings.maximumEliteLegDistance ||
                    !distanceToBoss.TryGetValue(candidate, out int distanceFromCandidateToBoss))
                {
                    continue;
                }

                float detourRatio = (distanceFromCurrentNode + distanceFromCandidateToBoss) /
                    (float)Mathf.Max(1, currentToBossDistance);

                if (detourRatio > _settings.maximumDetourRatio)
                {
                    continue;
                }

                float score = ScoreEliteCandidate(
                    candidate,
                    currentNode,
                    startNode,
                    bossNode,
                    distanceFromCurrentNode,
                    distanceFromCandidateToBoss,
                    distanceFromStart,
                    defeatedCount);

                CandidateScore scoredCandidate = new CandidateScore(candidate, score);
                relaxedCandidates.Add(scoredCandidate);

                if (distanceFromCandidateToBoss >= reserveDistance)
                {
                    strictCandidates.Add(scoredCandidate);
                }
            }

            // 작은 맵에서는 이후 구간을 모두 확보하는 엄격한 조건이 비어 있을 수 있습니다.
            // 이때도 멈추지 않고 같은 품질 점수로 완화 후보를 선택합니다.
            List<CandidateScore> candidates = strictCandidates.Count > 0
                ? strictCandidates
                : relaxedCandidates;

            if (candidates.Count == 0)
            {
                return FindRelaxedEliteFallback(currentNode, bossNode, distanceFromCurrent, distanceToBoss);
            }

            candidates.Sort(CompareCandidates);
            return candidates[0].Node;
        }

        private float ScoreEliteCandidate(
            MapNode candidate,
            MapNode currentNode,
            MapNode startNode,
            MapNode bossNode,
            int distanceFromCurrent,
            int distanceToBoss,
            Dictionary<MapNode, int> distanceFromStart,
            int defeatedCount)
        {
            float desiredDistance = (_settings.minimumEliteLegDistance + _settings.maximumEliteLegDistance) * 0.5f;
            float distanceScore = -Mathf.Abs(distanceFromCurrent - desiredDistance);

            distanceFromStart.TryGetValue(currentNode, out int currentStartDistance);
            distanceFromStart.TryGetValue(candidate, out int candidateStartDistance);
            float forwardProgress = candidateStartDistance - currentStartDistance;

            int nearbyOpportunityCount = CountNearbyOpportunity(candidate, 3);
            int branchCount = Mathf.Max(0, CountWalkableNeighbors(candidate) - 1);

            Vector2 mainAxis = ((Vector2)bossNode.Position - startNode.Position).normalized;
            Vector2 sideAxis = new Vector2(-mainAxis.y, mainAxis.x);
            float candidateSide = Mathf.Sign(Vector2.Dot(candidate.Position - startNode.Position, sideAxis));
            float desiredSide = defeatedCount % 2 == 0 ? 1f : -1f;
            float sideScore = candidateSide == 0f ? 0f : candidateSide * desiredSide;

            // 현재 위치 -> 후보 -> 보스가 최단 경로와 많이 달라질수록 감점합니다.
            float directDistance = Vector2Int.Distance(currentNode.Position, bossNode.Position);
            float geometricDetour = Mathf.Max(0f, distanceFromCurrent + distanceToBoss - directDistance);

            return distanceScore +
                   forwardProgress * _settings.forwardProgressWeight +
                   (nearbyOpportunityCount + branchCount) * _settings.explorationOpportunityWeight +
                   sideScore * _settings.sideAlternationWeight -
                   geometricDetour * 0.25f;
        }

        private MapNode FindRelaxedEliteFallback(
            MapNode currentNode,
            MapNode bossNode,
            Dictionary<MapNode, int> distanceFromCurrent,
            Dictionary<MapNode, int> distanceToBoss)
        {
            MapNode fallback = null;
            int bestDistance = -1;

            foreach (KeyValuePair<MapNode, int> pair in distanceFromCurrent)
            {
                if (!IsAvailableObjectiveNode(pair.Key) || pair.Key == bossNode ||
                    pair.Value < 1 || !distanceToBoss.ContainsKey(pair.Key))
                {
                    continue;
                }

                if (pair.Value > bestDistance ||
                    (pair.Value == bestDistance && IsLowerPosition(pair.Key, fallback)))
                {
                    fallback = pair.Key;
                    bestDistance = pair.Value;
                }
            }

            return fallback;
        }

        private int CountNearbyOpportunity(MapNode origin, int maximumHops)
        {
            Dictionary<MapNode, int> distances = BuildDistanceMap(origin, maximumHops);
            int count = 0;

            foreach (KeyValuePair<MapNode, int> pair in distances)
            {
                if (pair.Key != origin && IsWalkable(pair.Key))
                {
                    count++;
                }
            }

            return count;
        }

        private int CountWalkableNeighbors(MapNode node)
        {
            int count = 0;
            foreach (MapNode neighbor in node.ConnectedNodes)
            {
                if (IsWalkable(neighbor))
                {
                    count++;
                }
            }

            return count;
        }

        public Dictionary<MapNode, int> BuildDistanceMap(MapNode startNode, int maximumDistance = int.MaxValue)
        {
            Dictionary<MapNode, int> distances = new Dictionary<MapNode, int>();
            if (startNode == null || !IsWalkable(startNode))
            {
                return distances;
            }

            Queue<MapNode> queue = new Queue<MapNode>();
            queue.Enqueue(startNode);
            distances[startNode] = 0;

            while (queue.Count > 0)
            {
                MapNode current = queue.Dequeue();
                int currentDistance = distances[current];

                if (currentDistance >= maximumDistance)
                {
                    continue;
                }

                foreach (MapNode neighbor in current.ConnectedNodes)
                {
                    if (!IsWalkable(neighbor) || distances.ContainsKey(neighbor))
                    {
                        continue;
                    }

                    distances[neighbor] = currentDistance + 1;
                    queue.Enqueue(neighbor);
                }
            }

            return distances;
        }

        public static bool IsWalkable(MapNode node)
        {
            if (node == null)
            {
                return false;
            }

            return node.Type != NodeType.Tree &&
                   node.Type != NodeType.Rock &&
                   node.Type != NodeType.WaterPuddle &&
                   node.Type != NodeType.WaterStart &&
                   node.Type != NodeType.WaterBody &&
                   node.Type != NodeType.WaterEnd;
        }

        private bool IsAvailableObjectiveNode(MapNode node)
        {
            return node != null &&
                   node.Type == NodeType.Normal &&
                   !_consumed.Contains(node.Position);
        }

        private static int CompareCandidates(CandidateScore left, CandidateScore right)
        {
            int scoreComparison = right.Score.CompareTo(left.Score);
            if (scoreComparison != 0)
            {
                return scoreComparison;
            }

            if (left.Node.Position.x != right.Node.Position.x)
            {
                return left.Node.Position.x.CompareTo(right.Node.Position.x);
            }

            return left.Node.Position.y.CompareTo(right.Node.Position.y);
        }

        private static bool IsLowerPosition(MapNode candidate, MapNode currentBest)
        {
            if (currentBest == null)
            {
                return true;
            }

            return candidate.Position.x < currentBest.Position.x ||
                   (candidate.Position.x == currentBest.Position.x &&
                    candidate.Position.y < currentBest.Position.y);
        }

        private readonly struct CandidateScore
        {
            public CandidateScore(MapNode node, float score)
            {
                Node = node;
                Score = score;
            }

            public MapNode Node { get; }
            public float Score { get; }
        }
    }
}
