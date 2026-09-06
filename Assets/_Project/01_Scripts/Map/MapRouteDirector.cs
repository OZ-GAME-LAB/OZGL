using System.Collections;
using System.Collections.Generic;
using OzGameLab01.Data;
using UnityEngine;

namespace OZGL.Map
{
    /// <summary>
    /// 절차적으로 생성된 보드 위에 '중간 보스 -> 최종 보스' 진행 경로를 후처리로 배치합니다.
    ///
    /// 기존 MapGenerator를 수정하지 않고 NodeDict와 ReplaceTileVisual만 사용합니다.
    /// 같은 씬에서 MapObjectiveManager와 함께 활성화하면 목표가 중복 생성되므로,
    /// 이 컴포넌트를 사용할 때는 기존 MapObjectiveManager 컴포넌트를 비활성화해야 합니다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MapRouteDirector : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private MapGenerator mapGenerator;
        [SerializeField] private GameObject highlightPrefab;

        [Header("Progress")]
        [Tooltip("최종 보스 전에 처치해야 하는 중간 보스 수입니다.")]
        [Min(0)] [SerializeField] private int eliteCount = 3;

        [Tooltip("맵 생성 애니메이션이 끝난 뒤 목표를 배치하기까지의 추가 대기 시간입니다.")]
        [Min(0f)] [SerializeField] private float objectiveSpawnDelay = 0.5f;

        [Header("Leg Distance (tile hops)")]
        [Tooltip("현재 목표/플레이어 위치에서 다음 중간 보스까지의 최소 그래프 거리입니다.")]
        [Min(1)] [SerializeField] private int minimumEliteLegDistance = 10;

        [Tooltip("현재 목표/플레이어 위치에서 다음 중간 보스까지의 최대 그래프 거리입니다.")]
        [Min(1)] [SerializeField] private int maximumEliteLegDistance = 18;

        [Tooltip("최종 보스가 직전 중간 보스에 너무 붙지 않도록 보장하는 최소 거리입니다.")]
        [Min(1)] [SerializeField] private int minimumBossLegDistance = 10;

        [Tooltip("남은 중간 보스와 최종 보스를 배치할 공간이 부족한 후보를 피하기 위한 여유 거리입니다.")]
        [Range(0f, 1f)] [SerializeField] private float futureRouteReserveRatio = 0.6f;

        [Header("Route Scoring")]
        [Tooltip("중간 보스 후보가 최종 보스를 향해 전진할수록 받는 가중치입니다.")]
        [Min(0f)] [SerializeField] private float forwardProgressWeight = 1.5f;

        [Tooltip("갈림길 및 주변 타일이 있는 후보를 선호하는 가중치입니다.")]
        [Min(0f)] [SerializeField] private float explorationOpportunityWeight = 1.2f;

        [Tooltip("중간 보스마다 좌/우 방향을 번갈아 선호하는 정도입니다. 강제가 아닌 가산점입니다.")]
        [Min(0f)] [SerializeField] private float sideAlternationWeight = 2f;

        [Tooltip("최종 보스로 가는 최단 경로에서 과도하게 벗어나는 후보의 감점입니다.")]
        [Min(1f)] [SerializeField] private float maximumDetourRatio = 1.6f;

        [Header("Highlight")]
        [SerializeField] private float highlightHeight = 2f;

        private GameObject currentHighlight;
        private MapNode finalBossNode;
        private MapNode currentObjective;

        /// <summary>현재 하이라이트된 목표입니다. 디버그 UI 등에서 읽기 전용으로 사용할 수 있습니다.</summary>
        public MapNode CurrentObjective => currentObjective;

        /// <summary>맵 분석으로 예약된 최종 보스 위치입니다.</summary>
        public MapNode FinalBossNode => finalBossNode;

        private void Awake()
        {
            ResolveActiveMapGenerator();
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
            while (mapGenerator == null || mapGenerator.NodeDict.Count == 0)
            {
                ResolveActiveMapGenerator();
                yield return null;
            }

            // 전투 복귀에서는 MapGenerator가 타일을 즉시 복원하므로 연출 시간만큼 기다리지 않습니다.
            if (mapGenerator.UsedInitialGenerationAnimation)
            {
                yield return new WaitForSeconds(mapGenerator.animationDuration + objectiveSpawnDelay);
            }

            RefreshNextObjective();
        }

        private void ResolveActiveMapGenerator()
        {
            if (mapGenerator != null && mapGenerator.isActiveAndEnabled)
            {
                return;
            }

            mapGenerator = FindFirstObjectByType<MapGenerator>();
        }

        private void HandleBattleCompleted()
        {
            // 전투 씬을 거쳐 보드 씬이 새로 로드되는 경우에는 Start()가 다시 처리합니다.
            // 보드가 유지되는 전환 방식도 지원하도록 한 프레임 뒤에 다시 계산합니다.
            StartCoroutine(RefreshAfterBattle());
        }

        private IEnumerator RefreshAfterBattle()
        {
            yield return null;
            RefreshNextObjective();
        }

        /// <summary>
        /// 현재 플레이어 위치와 중간 보스 처치 수를 기준으로 다음 목표를 배치하고 하이라이트합니다.
        /// 인스펙터의 Context Menu로 현재 맵에서 반복 시험할 수 있습니다.
        /// </summary>
        [ContextMenu("Refresh Next Objective")]
        public void RefreshNextObjective()
        {
            if (mapGenerator == null || mapGenerator.NodeDict.Count == 0)
            {
                Debug.LogWarning("[MapRouteDirector] MapGenerator가 준비되지 않았습니다.", this);
                return;
            }

            if (BoardRunData.IsBossDefeated)
            {
                ClearHighlight();
                return;
            }

            MapNode startNode = FindStartNode();
            if (startNode == null)
            {
                Debug.LogError("[MapRouteDirector] Start 노드를 찾지 못했습니다.", this);
                return;
            }

            Dictionary<MapNode, int> distanceFromStart = BuildDistanceMap(startNode);
            finalBossNode = SelectFinalBossNode(startNode, distanceFromStart);

            if (finalBossNode == null)
            {
                Debug.LogError("[MapRouteDirector] 최종 보스 후보를 찾지 못했습니다.", this);
                return;
            }

            Vector2Int currentPosition = BoardRunData.HasPlayerPosition
                ? BoardRunData.PlayerPosition
                : startNode.Position;

            if (!mapGenerator.NodeDict.TryGetValue(currentPosition, out MapNode currentNode) ||
                !IsWalkable(currentNode))
            {
                currentNode = startNode;
            }

            NodeType targetType = BoardRunData.DefeatedElitesCount >= eliteCount
                ? NodeType.Boss
                : NodeType.Elite;

            MapNode targetNode = targetType == NodeType.Boss
                ? SelectBossForCurrentPosition(currentNode, finalBossNode)
                : SelectEliteNode(currentNode, startNode, finalBossNode, distanceFromStart);

            if (targetNode == null)
            {
                Debug.LogError("[MapRouteDirector] 다음 목표에 쓸 수 있는 Normal 타일을 찾지 못했습니다.", this);
                return;
            }

            SetObjective(targetNode, targetType);
        }

        private MapNode FindStartNode()
        {
            foreach (MapNode node in mapGenerator.NodeDict.Values)
            {
                if (node.Type == NodeType.Start)
                {
                    return node;
                }
            }

            mapGenerator.NodeDict.TryGetValue(Vector2Int.zero, out MapNode fallback);
            return fallback;
        }

        private MapNode SelectFinalBossNode(
            MapNode startNode,
            Dictionary<MapNode, int> distanceFromStart)
        {
            MapNode best = null;
            int bestDistance = -1;

            foreach (KeyValuePair<MapNode, int> pair in distanceFromStart)
            {
                MapNode candidate = pair.Key;
                if (candidate.Type != NodeType.Normal)
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
                if (candidate == startNode || !IsWalkable(candidate))
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

        private MapNode SelectBossForCurrentPosition(MapNode currentNode, MapNode plannedBossNode)
        {
            if (plannedBossNode.Type == NodeType.Normal)
            {
                Dictionary<MapNode, int> distances = BuildDistanceMap(currentNode);
                if (distances.TryGetValue(plannedBossNode, out int bossDistance) &&
                    bossDistance >= minimumBossLegDistance)
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
                if (pair.Key.Type != NodeType.Normal || pair.Value < minimumBossLegDistance)
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

            return fallback ?? (plannedBossNode.Type == NodeType.Normal ? plannedBossNode : null);
        }

        private MapNode SelectEliteNode(
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

            int defeatedCount = BoardRunData.DefeatedElitesCount;
            int remainingEliteCount = Mathf.Max(0, eliteCount - defeatedCount - 1);
            int futureMinimumDistance =
                remainingEliteCount * minimumEliteLegDistance + minimumBossLegDistance;
            int reserveDistance = Mathf.RoundToInt(futureMinimumDistance * futureRouteReserveRatio);

            List<CandidateScore> strictCandidates = new List<CandidateScore>();
            List<CandidateScore> relaxedCandidates = new List<CandidateScore>();

            foreach (KeyValuePair<MapNode, int> pair in distanceFromCurrent)
            {
                MapNode candidate = pair.Key;
                int distanceFromCurrentNode = pair.Value;

                if (candidate.Type != NodeType.Normal || candidate == bossNode ||
                    distanceFromCurrentNode < minimumEliteLegDistance ||
                    distanceFromCurrentNode > maximumEliteLegDistance ||
                    !distanceToBoss.TryGetValue(candidate, out int distanceFromCandidateToBoss))
                {
                    continue;
                }

                float detourRatio = (distanceFromCurrentNode + distanceFromCandidateToBoss) /
                    (float)Mathf.Max(1, currentToBossDistance);

                if (detourRatio > maximumDetourRatio)
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
            float desiredDistance = (minimumEliteLegDistance + maximumEliteLegDistance) * 0.5f;
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
                   forwardProgress * forwardProgressWeight +
                   (nearbyOpportunityCount + branchCount) * explorationOpportunityWeight +
                   sideScore * sideAlternationWeight -
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
                if (pair.Key.Type != NodeType.Normal || pair.Key == bossNode ||
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

        private Dictionary<MapNode, int> BuildDistanceMap(MapNode startNode, int maximumDistance = int.MaxValue)
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

        private void SetObjective(MapNode targetNode, NodeType targetType)
        {
            ClearHighlight();

            targetNode.Type = targetType;
            mapGenerator.ReplaceTileVisual(targetNode);
            currentObjective = targetNode;

            if (highlightPrefab != null && targetNode.NodeView != null)
            {
                currentHighlight = Instantiate(highlightPrefab, targetNode.NodeView.transform);
                currentHighlight.transform.localPosition = Vector3.up * highlightHeight;
            }

            Debug.Log(
                $"[MapRouteDirector] 다음 목표 배치 | Type: {targetType}, " +
                $"Position: {targetNode.Position}, " +
                $"Defeated Elites: {BoardRunData.DefeatedElitesCount}/{eliteCount}",
                this);
        }

        private void ClearHighlight()
        {
            if (currentHighlight != null)
            {
                Destroy(currentHighlight);
                currentHighlight = null;
            }

            currentObjective = null;
        }

        private static bool IsWalkable(MapNode node)
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
