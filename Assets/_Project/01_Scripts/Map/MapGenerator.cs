/*
 * [핵심 알고리즘 요약: 펄린 노이즈 기반 우선순위 BFS (Perlin-guided Priority BFS)]
 * - 펄린 노이즈 지형 점수와 외곽 감쇠(Radial Falloff), 중앙 코어(Core) 가산점을 결합합니다.
 * - 점수가 높은 곳부터 채워나가며, 안정적인 중앙 대륙과 유기적이고 둥근 해안선을 가진 섬 형태의 맵을 보장합니다.
 */
using OzGameLab01.Data;
using OzGameLab01.Map;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace OzGameLab01.Map
{
    public class MapGenerator : MonoBehaviour
    {
        private const float NodeScaleAnimationDuration = 0.5f;
        private const int ObstacleClusterPlacementAttempts = 100;

        [Header("Theme Data")]
        [Tooltip("현재 스테이지에 맞는 테마 데이터(SO)를 연결해주세요.")]
        [SerializeField] private OzGameLab01.Data.MapThemeData _currentTheme;

        [Header("Map Size Settings")]
        [Tooltip("최종적으로 남길 타일(노드)의 목표 개수")]
        public int totalNodeCount = 150;
        [Tooltip("대륙이 뻗어나갈 수 있는 최대 반경 (둥근 형태 유도 및 보이지 않는 벽)")]
        public float maxRadius = 25f;

        [Header("Rendering Settings")]
        [Tooltip("타일 간의 간격")]
        public float tileSpacing = 2.0f;
        [Tooltip("타일 프리팹의 X, Z 스케일에만 적용할 수평 배율입니다. Y 배율은 1로 유지됩니다.")]
        [Min(0.01f)]
        [SerializeField] private float tileScaleMultiplier = 1f;
        [Tooltip("맵 전체 생성 애니메이션 재생 시간")]
        public float animationDuration = 5.0f;

        [Header("Continent Shape Settings")]
        [Tooltip("지형의 구불구불한 정도 (작을수록 큼지막한 덩어리 대륙이 됨)")]
        public float noiseScale = 0.15f;
        [Tooltip("가장자리로 갈수록 깎아내는 강도 (섬 모양 유도)")]
        [Range(0f, 1.5f)] public float edgeFalloffStrength = 0.8f;
        [Tooltip("맵 중앙이 휑하게 비는 것을 막기 위해 강제로 채워넣을 뼈대(코어) 반경")]
        public float coreRadius = 4f;

        [Header("Tree Settings")]
        public int treeClusterCount = 2;
        public int minTreeClusterSize = 1;
        public int maxTreeClusterSize = 3;

        [Header("Rock Settings")]
        public int rockClusterCount = 2;
        public int minRockClusterSize = 1;
        public int maxRockClusterSize = 4;

        [Header("Water Settings")]
        public int waterClusterCount = 1;
        public int minWaterClusterSize = 1;
        public int maxWaterClusterSize = 5;

        // --- [수정됨] 시작 동선 설계 및 최소/최대 거리 변수 세팅 ---
        [Header("Start Sequence Settings")]
        [Tooltip("시작 타일의 3면을 바위로 막고, 1면에 유닛 획득 타일을 확정 배치하여 초반 획득을 강제합니다.")]
        public bool forceUnitAtStart = true;

        [Header("Tile Counts & Distances")]
        public int bossCount = 1;
        public int minBossDistance = 5;
        public int minBossDistanceFromStart = 4;
        public int maxBossDistanceFromStart = 999; // 최대 거리 추가됨

        public int shopCount = 3;
        public int minShopDistance = 3;
        public int minShopDistFromStart = 0;
        public int maxShopDistFromStart = 999; // 최대 거리 추가됨

        public int eliteCount = 3;
        public int minEliteDistance = 3;
        public int minEliteDistFromStart = 0;
        public int maxEliteDistFromStart = 999; // 최대 거리 추가됨

        public int eventCount = 8;
        public int minEventDistance = 2;
        public int minEventDistFromStart = 0;
        public int maxEventDistFromStart = 999; // 최대 거리 추가됨

        public int battleCount = 15;
        public int minBattleDistance = 1;
        public int minBattleDistFromStart = 0;
        public int maxBattleDistFromStart = 999; // 최대 거리 추가됨

        public int unitAcquisitionCount = 2;
        public int minUnitAcquisitionDistance = 4;
        public int minUnitAcquisitionDistFromStart = 2;
        public int maxUnitAcquisitionDistFromStart = 999; // 유닛 획득 타일 설정 추가됨
        // --------------------------------------------------------

        private Dictionary<Vector2Int, MapNode> _nodeDict = new Dictionary<Vector2Int, MapNode>();
        private List<MapNode> _allNodes = new List<MapNode>();
        public IReadOnlyDictionary<Vector2Int, MapNode> NodeDict => _nodeDict;

        /// <summary>
        /// 논리 좌표 기준으로 설정된 맵의 최대 생성 반경입니다.
        /// </summary>
        public float MaxRadius => maxRadius;

        /// <summary>
        /// 타일 간격을 반영한 월드 좌표 기준 최대 생성 반경입니다.
        /// </summary>
        public float MaxWorldRadius => maxRadius * tileSpacing;

        /// <summary>
        /// 타일 프리팹의 X, Z 스케일에만 적용되는 수평 배율입니다.
        /// </summary>
        public float TileScaleMultiplier => tileScaleMultiplier;

        /// <summary>
        /// 실제로 생성된 모든 노드를 포함하는 월드 좌표 Bounds입니다.
        /// 절차적으로 생성된 맵을 덮는 효과에는 MaxRadius보다 이 값을 우선 사용하는 것이 정확합니다.
        /// </summary>
        public Bounds GeneratedWorldBounds { get; private set; }

        /// <summary>
        /// 이번 보드 씬 진입에서 맵 생성 연출을 재생하는지 여부입니다.
        /// MapRouteDirector가 최초 목표 배치 대기 여부를 판단할 때 사용합니다.
        /// </summary>
        public bool UsedInitialGenerationAnimation { get; private set; }

        /// <summary>
        /// 즉시 생성 또는 최초 생성 연출이 모두 끝나 플레이 가능한 상태인지 나타냅니다.
        /// </summary>
        public bool IsPresentationComplete { get; private set; }

        public event System.Action PresentationCompleted;

        protected virtual void Start()
        {
            // 새 게임 시작 시에는 이미 활성 런이 만들어져 있으므로 저장된 플레이어 위치로 최초 진입을 구분합니다.
            UsedInitialGenerationAnimation = !BoardRunData.HasPlayerPosition;
            IsPresentationComplete = false;

            if (_currentTheme == null)
            {
                Debug.LogError("[MapGenerator3] MapThemeData가 할당되지 않아 맵을 생성할 수 없습니다!");
                return;
            }

            ValidatePrefabs();
            GenerateMapData();

            if (UsedInitialGenerationAnimation)
            {
                PlayMapAnimation();
            }
            else
            {
                CreateMapImmediately();
            }
        }

        public void GenerateMapData()
        {
            BoardRunData.EnsureActiveRun();
            Random.State previousRandomState = Random.state;
            Random.InitState(BoardRunData.MapSeed);

            try
            {
                _nodeDict.Clear();
                _allNodes.Clear();

                GenerateLogicalShape();
                AssignNodeTypes();
                ApplyPostGenerationRules();
                ApplyConsumedSpecialTiles();
                UpdateGeneratedWorldBounds();
            }
            finally
            {
                Random.state = previousRandomState;
            }

            Debug.Log($"[MapGenerator3] 대륙 맵 생성 완료 | Seed: {BoardRunData.MapSeed} | 최종 노드 수: {_allNodes.Count}");

            if (OzGameLab01.Map.MapManager.Instance != null)
            {
                OzGameLab01.Map.MapManager.Instance.InitializeMapData(_nodeDict);
            }

            if (OzGameLab01.Controllers.BoardPlayerController.Instance == null) return;

            Vector2Int targetPosition = BoardRunData.HasPlayerPosition ? BoardRunData.PlayerPosition : Vector2Int.zero;

            if (!_nodeDict.TryGetValue(targetPosition, out MapNode targetNode))
            {
                targetPosition = GetStartNodePosition();
                if (!_nodeDict.TryGetValue(targetPosition, out targetNode)) return;
            }

            if (!BoardRunData.HasPlayerPosition || BoardRunData.PlayerPosition != targetPosition)
            {
                BoardRunData.SavePlayerPosition(targetPosition);
            }

            OzGameLab01.Controllers.BoardPlayerController.Instance.SetupPlayer(targetNode);
        }

        private Vector2Int GetStartNodePosition()
        {
            foreach (var node in _allNodes)
                if (node.Type == NodeType.Start) return node.Position;
            return Vector2Int.zero;
        }

        public void PlayMapAnimation()
        {
            StartCoroutine(AnimateMapGeneration());
        }

        /// <summary>
        /// 기본 생성이 끝난 뒤 파생 생성기가 추가 규칙을 적용할 수 있는 확장 지점입니다.
        /// 기본 생성기는 별도 보정을 수행하지 않습니다.
        /// </summary>
        protected virtual void ApplyPostGenerationRules()
        {
        }

        /// <summary>
        /// 같은 Seed로 맵을 다시 생성해도 이미 완료한 일회성 특수 타일은 Normal로 복원합니다.
        /// </summary>
        private void ApplyConsumedSpecialTiles()
        {
            foreach (MapNode node in _allNodes.Where(node =>
                         IsSingleUseSpecialTile(node.Type) &&
                         BoardRunData.IsSpecialTileConsumed(node.Position)))
            {
                node.Type = NodeType.Normal;
            }
        }

        /// <summary>
        /// 파생 생성기가 생성된 논리 노드를 읽고 타입을 보정할 때 사용합니다.
        /// </summary>
        protected IReadOnlyList<MapNode> AllNodes => _allNodes;

        /// <summary>
        /// 전투 씬에서 보드로 복귀할 때, 동일한 Seed로 복원한 맵의 타일을
        /// 생성 연출 없이 즉시 표시합니다.
        /// </summary>
        private void CreateMapImmediately()
        {
            foreach (MapNode node in _allNodes)
            {
                CreateNodeView(node, false);
            }

            CompletePresentation();
        }

        private void UpdateGeneratedWorldBounds()
        {
            if (_allNodes.Count == 0)
            {
                GeneratedWorldBounds = new Bounds(Vector3.zero, Vector3.zero);
                return;
            }

            Vector2Int firstPosition = _allNodes[0].Position;
            Vector3 firstWorldPosition = new Vector3(
                firstPosition.x * tileSpacing,
                0f,
                firstPosition.y * tileSpacing);
            Bounds bounds = new Bounds(firstWorldPosition, Vector3.zero);

            for (int i = 1; i < _allNodes.Count; i++)
            {
                Vector2Int position = _allNodes[i].Position;
                bounds.Encapsulate(new Vector3(
                    position.x * tileSpacing,
                    0f,
                    position.y * tileSpacing));
            }

            bounds.Expand(new Vector3(tileSpacing, 0f, tileSpacing));
            GeneratedWorldBounds = bounds;
        }

        private void CompletePresentation()
        {
            if (IsPresentationComplete)
            {
                return;
            }

            IsPresentationComplete = true;
            PresentationCompleted?.Invoke();
        }

        private void ValidatePrefabs()
        {
            if (_currentTheme.NormalPrefab == null) Debug.LogWarning("[MapGenerator3] 필수 프리팹 누락: Normal");
            if (_currentTheme.BossPrefab == null) Debug.LogWarning("[MapGenerator3] 필수 프리팹 누락: Boss");
            if (_currentTheme.BattlePrefab == null) Debug.LogWarning("[MapGenerator3] 필수 프리팹 누락: Battle");
        }

        private void GenerateLogicalShape()
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

            while (_allNodes.Count < totalNodeCount && candidates.Count > 0)
            {
                int bestIndex = -1;
                float bestScore = float.MinValue;

                for (int i = 0; i < candidates.Count; i++)
                {
                    Vector2Int pos = candidates[i];

                    float distFromCenter = Vector2.Distance(Vector2.zero, pos);

                    if (distFromCenter > maxRadius)
                        continue;

                    float pX = pos.x * noiseScale + offsetX;
                    float pY = pos.y * noiseScale + offsetY;
                    float noiseVal = Mathf.PerlinNoise(pX, pY);
                    float falloff = Mathf.Clamp01(distFromCenter / maxRadius);

                    // [핵심 로직] 지정한 코어 반경(coreRadius) 안쪽은 노이즈 점수를 무시하고 엄청난 가산점(+10점)을 부여하여 무조건 꽉 채웁니다!
                    float coreBonus = (distFromCenter <= coreRadius) ? 10f : 0f;
                    float score = noiseVal - (falloff * edgeFalloffStrength) + coreBonus;

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

        private void AssignNodeTypes()
        {
            List<MapNode> availableNodes = new List<MapNode>(_allNodes);
            availableNodes.RemoveAll(n => n.Type == NodeType.Start);
            if (availableNodes.Count == 0) return;

            MapNode startNode = _allNodes.FirstOrDefault(node => node.Type == NodeType.Start);
            if (startNode == null)
            {
                Debug.LogError("[MapGenerator3] Start 타일이 없어 노드 타입을 배치할 수 없습니다!", this);
                return;
            }

            // 1. 장애물 먼저 배치하되, Start 기준 이동 가능 영역이 분리되지 않는 후보만 확정합니다.
            PlaceObstacleClusters(availableNodes, startNode);

            // [추가됨] 강제 시작 동선 셋팅 (시작 타일 3면 차단, 1면 유닛 확정 획득)
            if (forceUnitAtStart)
            {
                ConfigureForcedStartPath(startNode, availableNodes);
            }

            // 2. Start 타일로부터 맵 전체의 걸음 수(Depth)를 한 번 계산하여 캐싱합니다.
            CalculateAllNodeDepths(startNode);
            ValidateWalkableConnectivity();

            // 3. 타일 배치 (isSequential 옵션을 true로 주면 순차적으로 더 깊은 곳에 스폰됨)
            // 최종 보스: 순차 배치 켬 (점점 깊은 곳)
            //PlaceNodesOfType(NodeType.Boss, bossCount, minBossDistance, availableNodes, minBossDistanceFromStart, maxBossDistanceFromStart, true);

            // [추가됨] 랜덤 유닛 획득 타일 배치
            PlaceNodesOfType(NodeType.UnitAcquisition, unitAcquisitionCount, minUnitAcquisitionDistance, availableNodes, minUnitAcquisitionDistFromStart, maxUnitAcquisitionDistFromStart, false);

            // 삭제 예정이라 하셨지만 일단 둡니다.
            PlaceNodesOfType(NodeType.Shop, shopCount, minShopDistance, availableNodes, minShopDistFromStart, maxShopDistFromStart, false);

            // 엘리트: 순차 배치 켬! (엘리트1 -> 2 -> 3 순으로 맵의 더 깊은 곳으로 강제 전진)
            //PlaceNodesOfType(NodeType.Elite, eliteCount, minEliteDistance, availableNodes, minEliteDistFromStart, maxEliteDistFromStart, true);

            PlaceNodesOfType(NodeType.Event, eventCount, minEventDistance, availableNodes, minEventDistFromStart, maxEventDistFromStart, false);
            PlaceNodesOfType(NodeType.Battle, battleCount, minBattleDistance, availableNodes, minBattleDistFromStart, maxBattleDistFromStart, false);
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
            if (_currentTheme.TreePrefabs != null && _currentTheme.TreePrefabs.Count > 0)
                CreateCluster(availableNodes, startNode, treeClusterCount, minTreeClusterSize, maxTreeClusterSize, NodeType.Tree, false);

            if (_currentTheme.RockPrefabs != null && _currentTheme.RockPrefabs.Count > 0)
                CreateCluster(availableNodes, startNode, rockClusterCount, minRockClusterSize, maxRockClusterSize, NodeType.Rock, false);

            if (_currentTheme.WaterPuddlePrefab != null && _currentTheme.WaterStartPrefab != null &&
                _currentTheme.WaterEndPrefab != null && _currentTheme.WaterBodyPrefabs != null &&
                _currentTheme.WaterBodyPrefabs.Count > 0)
                CreateCluster(availableNodes, startNode, waterClusterCount, minWaterClusterSize, maxWaterClusterSize, NodeType.WaterPuddle, true);
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
                    Debug.LogWarning(
                        $"[MapGenerator3] 연결성을 유지할 수 없어 {baseType} 장애물 클러스터 " +
                        $"{clusterIndex + 1}/{count} 배치를 생략했습니다.",
                        this);
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
                Debug.LogWarning(
                    "[MapGenerator3] 시작 타일 주변에 유닛 획득 타일로 사용할 수 있는 노드가 없습니다.",
                    this);
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
            availableNodes.Remove(fallbackUnitNode);

            Debug.LogWarning(
                "[MapGenerator3] 시작 지점의 3면 차단이 맵 연결성을 해쳐 바위 배치를 생략했습니다.",
                this);
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

            Debug.LogError(
                $"[MapGenerator3] 장애물 배치 후 이동 가능 영역이 분리되었습니다. " +
                $"도달 가능: {_nodeDepths.Count}, 전체 이동 가능: {walkableNodeCount}",
                this);
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

        protected static bool IsObstacle(NodeType type)
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
                Debug.LogWarning($"[MapGenerator3] {type} 타일을 목표치({count}개)만큼 배치하지 못했습니다. (배치됨: {currentCount}개)");
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


        private IEnumerator AnimateMapGeneration()
        {
            float delayPerNode = animationDuration / Mathf.Max(1, _allNodes.Count);
            WaitForSeconds wait = new WaitForSeconds(delayPerNode);

            Queue<MapNode> queue = new Queue<MapNode>();
            HashSet<MapNode> visited = new HashSet<MapNode>();

            MapNode startNode = null;
            foreach (var node in _allNodes)
            {
                if (node.Type == NodeType.Start)
                {
                    startNode = node;
                    break;
                }
            }

            if (startNode == null)
            {
                Debug.LogError("[MapGenerator3] Start 타일 누락!");
                yield break;
            }

            queue.Enqueue(startNode);
            visited.Add(startNode);

            while (queue.Count > 0)
            {
                MapNode currentNode = queue.Dequeue();
                CreateNodeView(currentNode, true);

                yield return wait;

                foreach (MapNode neighbor in currentNode.ConnectedNodes)
                {
                    if (!visited.Contains(neighbor))
                    {
                        visited.Add(neighbor);
                        queue.Enqueue(neighbor);
                    }
                }
            }

            // 마지막 타일의 확대 연출까지 끝난 뒤 플레이 가능 상태를 알립니다.
            yield return new WaitForSeconds(NodeScaleAnimationDuration);
            CompletePresentation();
        }

        private void CreateNodeView(MapNode node, bool animateScale)
        {
            GameObject targetPrefab = GetPrefabForType(node.Type) ?? _currentTheme.NormalPrefab;

            if (targetPrefab == null)
            {
                return;
            }

            Vector3 worldPos = new Vector3(
                node.Position.x * tileSpacing,
                0f,
                node.Position.y * tileSpacing);

            node.NodeView = Instantiate(targetPrefab, worldPos, Quaternion.identity, transform);
            Transform nodeTransform = node.NodeView.transform;
            nodeTransform.localScale = Vector3.Scale(
                nodeTransform.localScale,
                new Vector3(tileScaleMultiplier, 1f, tileScaleMultiplier));

            TileView tileView = node.NodeView.GetComponent<TileView>();
            if (tileView != null)
            {
                tileView.Init(node);
            }

            if (animateScale)
            {
                StartCoroutine(ScaleUpNode(nodeTransform, NodeScaleAnimationDuration));
            }
        }

        private GameObject GetPrefabForType(NodeType type)
        {
            switch (type)
            {
                case NodeType.Start:
                case NodeType.Normal: return _currentTheme.NormalPrefab;
                case NodeType.Boss: return _currentTheme.BossPrefab;
                case NodeType.Shop: return _currentTheme.ShopPrefab;
                case NodeType.Event: return _currentTheme.EventPrefab;
                case NodeType.Elite: return _currentTheme.ElitePrefab;
                case NodeType.Battle: return _currentTheme.BattlePrefab;
                case NodeType.UnitAcquisition: return _currentTheme.UnitAcquisitionPrefab != null ? _currentTheme.UnitAcquisitionPrefab : _currentTheme.NormalPrefab; // [추가됨] 유닛 획득 타일

                case NodeType.Tree: return GetRandomPrefab(_currentTheme.TreePrefabs, _currentTheme.NormalPrefab);
                case NodeType.Rock: return GetRandomPrefab(_currentTheme.RockPrefabs, _currentTheme.NormalPrefab);

                case NodeType.WaterPuddle: return _currentTheme.WaterPuddlePrefab != null ? _currentTheme.WaterPuddlePrefab : _currentTheme.NormalPrefab;
                case NodeType.WaterStart: return _currentTheme.WaterStartPrefab != null ? _currentTheme.WaterStartPrefab : _currentTheme.NormalPrefab;
                case NodeType.WaterEnd: return _currentTheme.WaterEndPrefab != null ? _currentTheme.WaterEndPrefab : _currentTheme.NormalPrefab;
                case NodeType.WaterBody: return GetRandomPrefab(_currentTheme.WaterBodyPrefabs, _currentTheme.NormalPrefab);

                default: return _currentTheme.NormalPrefab;
            }
        }

        private GameObject GetRandomPrefab(List<GameObject> prefabs, GameObject fallback)
        {
            if (prefabs == null || prefabs.Count == 0) return fallback;
            return prefabs[Random.Range(0, prefabs.Count)];
        }

        private IEnumerator ScaleUpNode(Transform nodeTransform, float duration)
        {
            float time = 0f;
            Vector3 targetScale = nodeTransform.localScale;
            nodeTransform.localScale = Vector3.zero;

            while (time < duration)
            {
                if (nodeTransform == null) yield break;
                time += Time.deltaTime;
                float t = time / duration;
                float easeOutT = t * (2f - t);
                nodeTransform.localScale = Vector3.Lerp(Vector3.zero, targetScale, easeOutT);
                yield return null;
            }

            if (nodeTransform != null) nodeTransform.localScale = targetScale;
        }

        public void ReplaceTileVisual(MapNode node)
        {
            if (node == null)
            {
                return;
            }

            if (node.NodeView != null) Destroy(node.NodeView); // 기존 평범한 타일 모델 삭제
            CreateNodeView(node, false);
        }

        /// <summary>
        /// 효과 처리가 끝난 특수 타일을 현재 런에 기록하고 Normal 타일로 교체합니다.
        /// 특수 타일이 아닌 경우에는 아무 작업도 하지 않습니다.
        /// </summary>
        public bool ConsumeSpecialTile(MapNode node)
        {
            if (node == null || !IsSingleUseSpecialTile(node.Type))
            {
                return false;
            }

            BoardRunData.ConsumeSpecialTile(node.Position);
            return NormalizeConsumedSpecialTile(node);
        }

        /// <summary>
        /// 완료 기록이 있는 특수 타일의 논리 타입과 외형을 Normal로 동기화합니다.
        /// </summary>
        public bool NormalizeConsumedSpecialTile(MapNode node)
        {
            if (node == null ||
                !BoardRunData.IsSpecialTileConsumed(node.Position) ||
                !IsSingleUseSpecialTile(node.Type))
            {
                return false;
            }

            node.Type = NodeType.Normal;
            ReplaceTileVisual(node);
            return true;
        }

        /// <summary>
        /// 발동 후 Normal로 바뀌는 일회성 특수 타일인지 확인합니다.
        /// </summary>
        public static bool IsSingleUseSpecialTile(NodeType type)
        {
            return type == NodeType.Battle ||
                   type == NodeType.Event ||
                   type == NodeType.Shop ||
                   type == NodeType.Elite ||
                   type == NodeType.Boss ||
                   type == NodeType.UnitAcquisition;
        }
    }
}
