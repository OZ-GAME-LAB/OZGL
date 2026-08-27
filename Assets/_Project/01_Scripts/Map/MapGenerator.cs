using OzGameLab01.Map;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace OZGL.Map
{
    public enum NodeType
    {
        Normal,
        Start,
        Battle,
        Event,
        Shop,
        Elite,
        Boss,
        Tree,
        Rock,
        WaterPuddle,
        WaterStart,
        WaterBody,
        WaterEnd
    }

    public class MapNode
    {
        public Vector2Int Position;
        public NodeType Type;
        public List<MapNode> ConnectedNodes = new List<MapNode>();
        public GameObject NodeView;
    }

    public class MapGenerator : MonoBehaviour
    {
        [Header("Theme Data")]
        [Tooltip("현재 스테이지에 맞는 테마 데이터(SO)를 연결해주세요.")]
        [SerializeField] private OZGL.Data.MapThemeData _currentTheme;

        [Header("Map Settings")]
        public int totalNodeCount = 70;
        public float tileSpacing = 2.0f;
        public float animationDuration = 5.0f;

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

        [Header("Tile Counts")]
        public int bossCount = 1;
        public int minBossDistance = 5;
        public int minBossDistanceFromStart = 4;

        public int shopCount = 3;
        public int eliteCount = 3;
        public int eventCount = 8;
        public int battleCount = 15;

        public int minShopDistance = 3;
        public int minEliteDistance = 3;
        public int minEventDistance = 2;
        public int minBattleDistance = 1;

        private Dictionary<Vector2Int, MapNode> _nodeDict = new Dictionary<Vector2Int, MapNode>();
        private List<MapNode> _allNodes = new List<MapNode>();

        private void Start()
        {
            if (_currentTheme == null)
            {
                Debug.LogError("[MapGenerator] MapThemeData가 할당되지 않아 맵을 생성할 수 없습니다!");
                return;
            }

            ValidatePrefabs();

            // 테스트를 위해 Start에서 두 함수를 연달아 호출하지만,
            // 실제 게임에서는 로딩씬에서 GenerateMapData()를, 본게임 씬 진입 시 PlayMapAnimation()을 따로 호출하시면 됩니다.
            GenerateMapData();
            PlayMapAnimation();
        }

        // 외부(GameManager 등)에서 즉시 맵 데이터만 생성할 때 호출하는 public 함수
        public void GenerateMapData()
        {
            _nodeDict.Clear();
            _allNodes.Clear();

            GenerateLogicalShape();
            AssignNodeTypes();

            Debug.Log("[MapGenerator] 맵 데이터 생성이 완료되었습니다.");

            // ---------------- [연동 코드 추가 부분] ----------------
            // 1. MapManager에 생성된 노드 딕셔너리를 통째로 넘겨 길찾기 시스템을 활성화합니다.
            if (OzGameLab01.Map.MapManager.Instance != null)
            {
                OzGameLab01.Map.MapManager.Instance.InitializeMapData(_nodeDict);
            }

            // 2. 플레이어 컨트롤러에 시작점(Vector2Int.zero) 노드를 전달하여 플레이어 토큰을 세팅합니다.
            if (OzGameLab01.Controllers.BoardPlayerController.Instance != null && _nodeDict.ContainsKey(Vector2Int.zero))
            {
                OzGameLab01.Controllers.BoardPlayerController.Instance.SetupPlayer(_nodeDict[Vector2Int.zero]);
            }
            // -------------------------------------------------------
        }

        // 씬 전환이 완료된 후 타일 팝업 연출을 시작할 때 호출하는 public 함수
        public void PlayMapAnimation()
        {
            StartCoroutine(AnimateMapGeneration());
        }

        private void ValidatePrefabs()
        {
            if (_currentTheme.NormalPrefab == null) Debug.LogWarning("[MapGenerator] 필수: Normal 프리팹이 할당되지 않았습니다!");
            if (_currentTheme.BossPrefab == null) Debug.LogWarning("[MapGenerator] 필수: Boss 프리팹이 할당되지 않았습니다!");
            if (_currentTheme.BattlePrefab == null) Debug.LogWarning("[MapGenerator] 필수: Battle 프리팹이 할당되지 않았습니다!");
            if (_currentTheme.EventPrefab == null) Debug.LogWarning("[MapGenerator] 필수: Event 프리팹이 할당되지 않았습니다!");
            if (_currentTheme.ShopPrefab == null) Debug.LogWarning("[MapGenerator] 필수: Shop 프리팹이 할당되지 않았습니다!");
            if (_currentTheme.ElitePrefab == null) Debug.LogWarning("[MapGenerator] 필수: Elite 프리팹이 할당되지 않았습니다!");
        }

        private IEnumerator GenerateAndAnimateMap()
        {
            GenerateLogicalShape();
            AssignNodeTypes();
            yield return StartCoroutine(AnimateMapGeneration());
        }

        private void GenerateLogicalShape()
        {
            Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
            MapNode startNode = new MapNode { Position = Vector2Int.zero, Type = NodeType.Start };

            _nodeDict.Add(startNode.Position, startNode);
            _allNodes.Add(startNode);

            while (_allNodes.Count < totalNodeCount)
            {
                MapNode randomExistingNode = _allNodes[Random.Range(0, _allNodes.Count)];
                Vector2Int randomDir = directions[Random.Range(0, directions.Length)];
                Vector2Int newPos = randomExistingNode.Position + randomDir;

                if (!_nodeDict.ContainsKey(newPos))
                {
                    MapNode newNode = new MapNode { Position = newPos, Type = NodeType.Normal };

                    _nodeDict.Add(newPos, newNode);
                    _allNodes.Add(newNode);

                    // 핵심 수정: 방금 생성된 노드의 상하좌우를 모두 검사해서, 
                    // 인접한 위치에 이미 다른 노드가 있다면 서로 완벽하게 다리(Edge)를 연결해줍니다!
                    foreach (Vector2Int dir in directions)
                    {
                        Vector2Int neighborPos = newPos + dir;
                        if (_nodeDict.TryGetValue(neighborPos, out MapNode neighborNode))
                        {
                            // 서로 연결되어 있지 않다면 양방향으로 길을 뚫어줍니다.
                            if (!newNode.ConnectedNodes.Contains(neighborNode))
                            {
                                newNode.ConnectedNodes.Add(neighborNode);
                                neighborNode.ConnectedNodes.Add(newNode);
                            }
                        }
                    }
                }
            }
        }

        private void AssignNodeTypes()
        {
            List<MapNode> availableNodes = new List<MapNode>(_allNodes);
            availableNodes.RemoveAll(n => n.Type == NodeType.Start);

            if (availableNodes.Count == 0) return;

            PlaceObstacleClusters(availableNodes);

            PlaceNodesOfType(NodeType.Boss, bossCount, minBossDistance, availableNodes, minBossDistanceFromStart);
            PlaceNodesOfType(NodeType.Shop, shopCount, minShopDistance, availableNodes);
            PlaceNodesOfType(NodeType.Elite, eliteCount, minEliteDistance, availableNodes);
            PlaceNodesOfType(NodeType.Event, eventCount, minEventDistance, availableNodes);
            PlaceNodesOfType(NodeType.Battle, battleCount, minBattleDistance, availableNodes);
        }

        private void PlaceObstacleClusters(List<MapNode> availableNodes)
        {
            // 나무 군집 배치
            if (_currentTheme.TreePrefabs != null && _currentTheme.TreePrefabs.Count > 0)
            {
                CreateCluster(availableNodes, treeClusterCount, minTreeClusterSize, maxTreeClusterSize, NodeType.Tree, false);
            }

            // 바위 군집 배치
            if (_currentTheme.RockPrefabs != null && _currentTheme.RockPrefabs.Count > 0)
            {
                CreateCluster(availableNodes, rockClusterCount, minRockClusterSize, maxRockClusterSize, NodeType.Rock, false);
            }

            // 물 군집 배치
            if (_currentTheme.WaterPuddlePrefab != null && _currentTheme.WaterStartPrefab != null &&
                _currentTheme.WaterEndPrefab != null && _currentTheme.WaterBodyPrefabs != null &&
                _currentTheme.WaterBodyPrefabs.Count > 0)
            {
                CreateCluster(availableNodes, waterClusterCount, minWaterClusterSize, maxWaterClusterSize, NodeType.WaterPuddle, true);
            }
        }

        private void CreateCluster(List<MapNode> availableNodes, int count, int minSize, int maxSize, NodeType baseType, bool isWater)
        {
            for (int i = 0; i < count; i++)
            {
                if (availableNodes.Count == 0) break;

                // 인스펙터에서 min, max를 1로 주면 무조건 targetSize는 1이 됨
                int targetSize = Random.Range(minSize, maxSize + 1);
                MapNode seed = availableNodes[Random.Range(0, availableNodes.Count)];

                List<MapNode> cluster = new List<MapNode>();
                Queue<MapNode> queue = new Queue<MapNode>();
                HashSet<MapNode> visited = new HashSet<MapNode>();

                queue.Enqueue(seed);
                visited.Add(seed);

                while (queue.Count > 0 && cluster.Count < targetSize)
                {
                    MapNode current = queue.Dequeue();

                    if (availableNodes.Contains(current))
                    {
                        cluster.Add(current);
                        availableNodes.Remove(current);

                        List<MapNode> neighbors = new List<MapNode>(current.ConnectedNodes);
                        ShuffleList(neighbors);

                        foreach (MapNode neighbor in neighbors)
                        {
                            if (!visited.Contains(neighbor) && availableNodes.Contains(neighbor))
                            {
                                visited.Add(neighbor);
                                queue.Enqueue(neighbor);
                            }
                        }
                    }
                }

                if (cluster.Count > 0)
                {
                    if (!isWater)
                    {
                        foreach (var node in cluster) node.Type = baseType;
                    }
                    else
                    {
                        // 클러스터 크기가 1이면 무조건 '웅덩이(WaterPuddle)' 처리
                        if (cluster.Count == 1)
                        {
                            cluster[0].Type = NodeType.WaterPuddle;
                        }
                        else
                        {
                            // 2칸 이상이면 시작, 몸통, 끝점으로 구분하여 호수/강 형태 완성
                            cluster[0].Type = NodeType.WaterStart;
                            cluster[cluster.Count - 1].Type = NodeType.WaterEnd;
                            for (int j = 1; j < cluster.Count - 1; j++) cluster[j].Type = NodeType.WaterBody;
                        }
                    }
                }
            }
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

        private bool IsObstacle(NodeType type)
        {
            return type == NodeType.Tree || type == NodeType.Rock ||
                   type == NodeType.WaterPuddle || type == NodeType.WaterStart ||
                   type == NodeType.WaterBody || type == NodeType.WaterEnd;
        }

        private void PlaceNodesOfType(NodeType type, int count, int minDistance, List<MapNode> availableNodes, int minDistanceFromStart = 0)
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

                // 1. 고립 방지 검사: 주변에 이동 가능한 타일이 최소 1개는 있어야 함
                int walkableNeighbors = 0;
                foreach (MapNode neighbor in candidate.ConnectedNodes)
                {
                    if (!IsObstacle(neighbor.Type)) walkableNeighbors++;
                }

                if (walkableNeighbors == 0)
                {
                    isValid = false; // 4면이 모두 막혀있으면 탈락
                }

                // 2. 시작점으로부터의 최소 거리 검사
                if (isValid && minDistanceFromStart > 0)
                {
                    int distFromStart = Mathf.Abs(candidate.Position.x) + Mathf.Abs(candidate.Position.y);
                    if (distFromStart < minDistanceFromStart) isValid = false;
                }

                // 3. 동종 타일 간의 거리 검사
                if (isValid)
                {
                    foreach (MapNode placed in placedNodes)
                    {
                        int distance = Mathf.Abs(candidate.Position.x - placed.Position.x) + Mathf.Abs(candidate.Position.y - placed.Position.y);
                        if (distance < minDistance)
                        {
                            isValid = false;
                            break;
                        }
                    }
                }

                // 모든 검사를 통과했을 때만 배치
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
                Debug.LogWarning($"[MapGenerator] {type} 타일을 목표치({count}개)만큼 배치하지 못했습니다. (배치됨: {currentCount}개)");
            }
        }

        private IEnumerator AnimateMapGeneration()
        {
           float delayPerNode = animationDuration / totalNodeCount;
            WaitForSeconds wait = new WaitForSeconds(delayPerNode);

            Queue<MapNode> queue = new Queue<MapNode>();
            HashSet<MapNode> visited = new HashSet<MapNode>();

            MapNode startNode = _nodeDict[Vector2Int.zero];
            queue.Enqueue(startNode);
            visited.Add(startNode);

            while (queue.Count > 0)
            {
                MapNode currentNode = queue.Dequeue();
                GameObject targetPrefab = GetPrefabForType(currentNode.Type);
                
                if (targetPrefab == null)
                {
                    targetPrefab = _currentTheme.NormalPrefab;
                    if (targetPrefab == null) continue; 
                }

                Vector3 worldPos = new Vector3(currentNode.Position.x * tileSpacing, 0, currentNode.Position.y * tileSpacing);
                currentNode.NodeView = Instantiate(targetPrefab, worldPos, Quaternion.identity, this.transform);
                
                // 타일 초기화 (색상 및 노드 정보 저장)
                TileView tileView = currentNode.NodeView.GetComponent<TileView>();
                if (tileView != null)
                {
                    tileView.Init(currentNode);
                }
                else
                {
                    Debug.LogWarning($"[MapGenerator] {currentNode.Type} 타일 프리팹에 TileView 컴포넌트가 없습니다!");
                }

                StartCoroutine(ScaleUpNode(currentNode.NodeView.transform, 0.5f));

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
    }
}