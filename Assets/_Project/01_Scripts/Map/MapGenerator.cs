/*
 * [핵심 알고리즘 요약: 펄린 노이즈 기반 우선순위 BFS (Perlin-guided Priority BFS)]
 * - 펄린 노이즈 지형 점수와 외곽 감쇠(Radial Falloff), 중앙 코어(Core) 가산점을 결합합니다.
 * - 점수가 높은 곳부터 채워나가며, 안정적인 중앙 대륙과 유기적이고 둥근 해안선을 가진 섬 형태의 맵을 보장합니다.
 */
using OzGameLab01.Data;
using OzGameLab01.Map;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace OZGL.Map
{
    public class MapGenerator : MonoBehaviour
    {
        [Header("Theme Data")]
        [Tooltip("현재 스테이지에 맞는 테마 데이터(SO)를 연결해주세요.")]
        [SerializeField] private OZGL.Data.MapThemeData _currentTheme;

        [Header("Map Size Settings")]
        [Tooltip("최종적으로 남길 타일(노드)의 목표 개수")]
        public int totalNodeCount = 150;
        [Tooltip("대륙이 뻗어나갈 수 있는 최대 반경 (둥근 형태 유도 및 보이지 않는 벽)")]
        public float maxRadius = 25f;

        [Header("Rendering Settings")]
        [Tooltip("타일 간의 간격")]
        public float tileSpacing = 2.0f;
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
        public IReadOnlyDictionary<Vector2Int, MapNode> NodeDict => _nodeDict;

        private void Start()
        {
            if (_currentTheme == null)
            {
                Debug.LogError("[MapGenerator3] MapThemeData가 할당되지 않아 맵을 생성할 수 없습니다!");
                return;
            }

            ValidatePrefabs();
            GenerateMapData();
            PlayMapAnimation();
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
            // 1. 장애물 먼저 배치 (이후 거리를 잴 때 길을 막기 위함)
            PlaceObstacleClusters(availableNodes);
            // 2. Start 타일을 찾고, 맵 전체의 걸음 수(Depth)를 단 한 번 계산하여 캐싱
            MapNode startNode = null;
            foreach (var node in _allNodes)
                if (node.Type == NodeType.Start) { startNode = node; break; }
            CalculateAllNodeDepths(startNode);
            // 3. 타일 배치 (isSequential 옵션을 true로 주면 순차적으로 더 깊은 곳에 스폰됨)
            // 최종 보스: 순차 배치 켬 (점점 깊은 곳)
            //PlaceNodesOfType(NodeType.Boss, bossCount, minBossDistance, availableNodes, minBossDistanceFromStart, true);

            // 삭제 예정이라 하셨지만 일단 둡니다.
            PlaceNodesOfType(NodeType.Shop, shopCount, minShopDistance, availableNodes, 0, false);

            // 엘리트: 순차 배치 켬! (엘리트1 -> 2 -> 3 순으로 맵의 더 깊은 곳으로 강제 전진)
            //PlaceNodesOfType(NodeType.Elite, eliteCount, minEliteDistance, availableNodes, minEliteDistance, true);

            PlaceNodesOfType(NodeType.Event, eventCount, minEventDistance, availableNodes, 0, false);
            PlaceNodesOfType(NodeType.Battle, battleCount, minBattleDistance, availableNodes, 0, false);
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


        private void PlaceObstacleClusters(List<MapNode> availableNodes)
        {
            if (_currentTheme.TreePrefabs != null && _currentTheme.TreePrefabs.Count > 0)
                CreateCluster(availableNodes, treeClusterCount, minTreeClusterSize, maxTreeClusterSize, NodeType.Tree, false);

            if (_currentTheme.RockPrefabs != null && _currentTheme.RockPrefabs.Count > 0)
                CreateCluster(availableNodes, rockClusterCount, minRockClusterSize, maxRockClusterSize, NodeType.Rock, false);

            if (_currentTheme.WaterPuddlePrefab != null && _currentTheme.WaterStartPrefab != null &&
                _currentTheme.WaterEndPrefab != null && _currentTheme.WaterBodyPrefabs != null &&
                _currentTheme.WaterBodyPrefabs.Count > 0)
                CreateCluster(availableNodes, waterClusterCount, minWaterClusterSize, maxWaterClusterSize, NodeType.WaterPuddle, true);
        }

        private void CreateCluster(List<MapNode> availableNodes, int count, int minSize, int maxSize, NodeType baseType, bool isWater)
        {
            for (int i = 0; i < count; i++)
            {
                if (availableNodes.Count == 0) break;

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
                        if (cluster.Count == 1)
                        {
                            cluster[0].Type = NodeType.WaterPuddle;
                        }
                        else
                        {
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

        private void PlaceNodesOfType(NodeType type, int count, int minDistance, List<MapNode> availableNodes, int minDistanceFromStart = 0, bool isSequential = false)
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
                        if (candidateDepth < requiredDepth) isValid = false;
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
                GameObject targetPrefab = GetPrefabForType(currentNode.Type);

                if (targetPrefab == null)
                {
                    targetPrefab = _currentTheme.NormalPrefab;
                    if (targetPrefab == null) continue;
                }

                Vector3 worldPos = new Vector3(currentNode.Position.x * tileSpacing, 0, currentNode.Position.y * tileSpacing);
                currentNode.NodeView = Instantiate(targetPrefab, worldPos, Quaternion.identity, this.transform);

                TileView tileView = currentNode.NodeView.GetComponent<TileView>();
                if (tileView != null) tileView.Init(currentNode);

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

        public void ReplaceTileVisual(MapNode node)
        {
            if (node.NodeView != null) Destroy(node.NodeView); // 기존 평범한 타일 모델 삭제
            GameObject targetPrefab = GetPrefabForType(node.Type);
            if (targetPrefab == null) targetPrefab = _currentTheme.NormalPrefab;
            Vector3 worldPos = new Vector3(node.Position.x * tileSpacing, 0, node.Position.y * tileSpacing);
            node.NodeView = Instantiate(targetPrefab, worldPos, Quaternion.identity, this.transform);

            TileView tileView = node.NodeView.GetComponent<TileView>();
            if (tileView != null) tileView.Init(node);
        }
    }
}