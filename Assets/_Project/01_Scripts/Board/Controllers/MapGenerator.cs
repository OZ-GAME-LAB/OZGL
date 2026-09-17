/*
 * [핵심 알고리즘 요약: 펄린 노이즈 기반 우선순위 BFS (Perlin-guided Priority BFS)]
 * - 펄린 노이즈 지형 점수와 외곽 감쇠(Radial Falloff), 중앙 코어(Core) 가산점을 결합합니다.
 * - 점수가 높은 곳부터 채워나가며, 안정적인 중앙 대륙과 유기적이고 둥근 해안선을 가진 섬 형태의 맵을 보장합니다.
 */
using OzGameLab01.Data;
using OzGameLab01.Board.Models;
using OzGameLab01.Board.Views;
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
        private BoardMapView _mapView;
        private BoardMapView MapView => _mapView ?? (_mapView = new BoardMapView(transform));

        /// <summary>
        /// 논리 노드에 대응하는 현재 화면 오브젝트를 반환합니다.
        /// </summary>
        public GameObject GetNodeView(MapNode node)
        {
            return MapView.GetNodeView(node);
        }

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
                MapView.Clear();
                _nodeDict.Clear();
                _allNodes.Clear();

                BoardMapSettings settings = new BoardMapSettings
                {
                    totalNodeCount = totalNodeCount,
                    maxRadius = maxRadius,
                    noiseScale = noiseScale,
                    edgeFalloffStrength = edgeFalloffStrength,
                    coreRadius = coreRadius,
                    treeClusterCount = treeClusterCount,
                    minTreeClusterSize = minTreeClusterSize,
                    maxTreeClusterSize = maxTreeClusterSize,
                    rockClusterCount = rockClusterCount,
                    minRockClusterSize = minRockClusterSize,
                    maxRockClusterSize = maxRockClusterSize,
                    waterClusterCount = waterClusterCount,
                    minWaterClusterSize = minWaterClusterSize,
                    maxWaterClusterSize = maxWaterClusterSize,
                    forceUnitAtStart = forceUnitAtStart,
                    bossCount = bossCount,
                    minBossDistance = minBossDistance,
                    minBossDistanceFromStart = minBossDistanceFromStart,
                    maxBossDistanceFromStart = maxBossDistanceFromStart,
                    shopCount = shopCount,
                    minShopDistance = minShopDistance,
                    minShopDistFromStart = minShopDistFromStart,
                    maxShopDistFromStart = maxShopDistFromStart,
                    eliteCount = eliteCount,
                    minEliteDistance = minEliteDistance,
                    minEliteDistFromStart = minEliteDistFromStart,
                    maxEliteDistFromStart = maxEliteDistFromStart,
                    eventCount = eventCount,
                    minEventDistance = minEventDistance,
                    minEventDistFromStart = minEventDistFromStart,
                    maxEventDistFromStart = maxEventDistFromStart,
                    battleCount = battleCount,
                    minBattleDistance = minBattleDistance,
                    minBattleDistFromStart = minBattleDistFromStart,
                    maxBattleDistFromStart = maxBattleDistFromStart,
                    unitAcquisitionCount = unitAcquisitionCount,
                    minUnitAcquisitionDistance = minUnitAcquisitionDistance,
                    minUnitAcquisitionDistFromStart = minUnitAcquisitionDistFromStart,
                    maxUnitAcquisitionDistFromStart = maxUnitAcquisitionDistFromStart,
                    hasTreePrefabs = _currentTheme.TreePrefabs != null && _currentTheme.TreePrefabs.Count > 0,
                    hasRockPrefabs = _currentTheme.RockPrefabs != null && _currentTheme.RockPrefabs.Count > 0,
                    hasWaterPrefabs = _currentTheme.WaterPuddlePrefab != null && _currentTheme.WaterStartPrefab != null && _currentTheme.WaterEndPrefab != null && _currentTheme.WaterBodyPrefabs != null && _currentTheme.WaterBodyPrefabs.Count > 0
                };
                BoardMapModel model = new BoardMapModel(_nodeDict, _allNodes, settings, message => Debug.LogWarning(message, this), message => Debug.LogError(message, this));
                model.GenerateLogicalShape();
                model.AssignNodeTypes();
                ApplyPostGenerationRules();
                ApplyConsumedSpecialTiles();
                UpdateGeneratedWorldBounds();
            }
            finally
            {
                Random.state = previousRandomState;
            }

            Debug.Log($"[MapGenerator3] 대륙 맵 생성 완료 | Seed: {BoardRunData.MapSeed} | 최종 노드 수: {_allNodes.Count}");

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
            if (!BoardMapView.HasWeightedNormalPrefab(_currentTheme) && _currentTheme.NormalPrefab == null)
                Debug.LogWarning("[MapGenerator3] 필수 프리팹 누락: Normal");

            if (_currentTheme.BossBasePrefab == null &&
                _currentTheme.BossObjectPrefab == null &&
                _currentTheme.BossPrefab == null)
                Debug.LogWarning("[MapGenerator3] 필수 프리팹 누락: Boss");

            if (_currentTheme.BattleBasePrefab == null &&
                _currentTheme.BattleObjectPrefab == null &&
                _currentTheme.BattlePrefab == null)
                Debug.LogWarning("[MapGenerator3] 필수 프리팹 누락: Battle");
        }

        protected static bool IsObstacle(NodeType type)
        {
            return BoardMapModel.IsObstacle(type);
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
            Transform nodeTransform = MapView.CreateNode(node, _currentTheme, tileSpacing, tileScaleMultiplier, OzGameLab01.Controllers.BoardPlayerController.Instance);
            if (nodeTransform != null && animateScale)
            {
                StartCoroutine(MapView.ScaleUpNode(nodeTransform, NodeScaleAnimationDuration));
            }
        }

        public void ReplaceTileVisual(MapNode node)
        {
            if (node == null)
            {
                return;
            }

            MapView.Remove(node);
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
            return BoardTileRules.IsSingleUse(type);
        }
    }
}
