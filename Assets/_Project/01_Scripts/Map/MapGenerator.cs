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
        Tree,          // 추가됨: 다중 프리팹 지원 나무
        Rock,          // 추가됨: 다중 프리팹 지원 바위
        WaterPuddle,   // 추가됨: 1칸짜리 물 웅덩이
        WaterStart,    // 추가됨: 물 (시작점)
        WaterBody,     // 추가됨: 다중 프리팹 지원 물 (몸통)
        WaterEnd       // 추가됨: 물 (끝점)
    }

    public class MapNode
    {
        public Vector2Int Position;
        public NodeType Type;
        public List<MapNode> ConnectedNodes = new List<MapNode>();
        public GameObject NodeView; // 시각적 타일 오브젝트
    }

    public class MapGenerator : MonoBehaviour
    {
        [Header("Map Settings")]
        public int totalNodeCount = 70;

        [Tooltip("타일 간의 물리적 배치 간격 (기본값: 2)")]
        public float tileSpacing = 2.0f;

        [Tooltip("맵 전체가 생성되는 데 걸리는 총 연출 시간 (초 단위)")]
        public float animationDuration = 5.0f;

        [Header("Obstacle Settings")]
        [Tooltip("생성할 장애물 군집(덩어리)의 총 개수")]
        public int obstacleClusterCount = 3;
        [Tooltip("하나의 장애물 군집이 차지할 최소 칸 수")]
        public int minObstacleClusterSize = 1;
        [Tooltip("하나의 장애물 군집이 차지할 최대 칸 수")]
        public int maxObstacleClusterSize = 4;

        [Header("Tile Counts")]
        public int bossCount = 1;
        public int minBossDistance = 5; // 같은 보스 타일끼리의 최소 거리
        public int minBossDistanceFromStart = 4; // 추가됨: 시작점으로부터 보스 타일의 최소 거리

        public int shopCount = 3;
        public int eliteCount = 3;
        public int eventCount = 8;
        public int battleCount = 15;

        public int minShopDistance = 3;
        public int minEliteDistance = 3;
        public int minEventDistance = 2;
        public int minBattleDistance = 1; // 1이면 인접 가능, 2 이상이면 떨어짐

        [Header("Prefabs (Type-specific)")]
        [SerializeField] private GameObject _normalPrefab;
        [SerializeField] private GameObject _bossPrefab;
        [SerializeField] private GameObject _battlePrefab;
        [SerializeField] private GameObject _eventPrefab;
        [SerializeField] private GameObject _shopPrefab;
        [SerializeField] private GameObject _elitePrefab;

        [Header("Obstacle Prefabs (Randomized)")]
        [Tooltip("등록된 나무 프리팹 중 하나를 무작위로 선택하여 생성합니다.")]
        [SerializeField] private List<GameObject> _treePrefabs;
        [Tooltip("등록된 바위 프리팹 중 하나를 무작위로 선택하여 생성합니다.")]
        [SerializeField] private List<GameObject> _rockPrefabs;

        [Header("Water Obstacle Prefabs")]
        [Tooltip("1칸짜리 독립된 물 타일 (웅덩이)")]
        [SerializeField] private GameObject _waterPuddlePrefab;
        [Tooltip("연속된 물 타일의 시작점")]
        [SerializeField] private GameObject _waterStartPrefab;
        [Tooltip("연속된 물 타일의 중간 지점들 (무작위 선택)")]
        [SerializeField] private List<GameObject> _waterBodyPrefabs;
        [Tooltip("연속된 물 타일의 종료 지점")]
        [SerializeField] private GameObject _waterEndPrefab;

        private Dictionary<Vector2Int, MapNode> _nodeDict = new Dictionary<Vector2Int, MapNode>();
        private List<MapNode> _allNodes = new List<MapNode>();

        private void Start()
        {
            StartCoroutine(GenerateAndAnimateMap());
        }

        private IEnumerator GenerateAndAnimateMap()
        {
            GenerateLogicalShape();
            AssignNodeTypes();
            // 파라미터를 제거하고 매니저의 interval 변수를 직접 참조하도록 변경
            yield return StartCoroutine(AnimateMapGeneration());
        }

        private void GenerateLogicalShape()
        {
            Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

            // 1. 시작 노드 생성
            MapNode startNode = new MapNode { Position = Vector2Int.zero, Type = NodeType.Start };
            _nodeDict.Add(startNode.Position, startNode);
            _allNodes.Add(startNode);

            // 2. 지정된 개수만큼 뻗어 나가며 노드 생성
            while (_allNodes.Count < totalNodeCount)
            {
                MapNode randomExistingNode = _allNodes[Random.Range(0, _allNodes.Count)];
                Vector2Int randomDir = directions[Random.Range(0, directions.Length)];
                Vector2Int newPos = randomExistingNode.Position + randomDir;

                if (!_nodeDict.ContainsKey(newPos))
                {
                    // 기본값을 Battle이 아닌 Normal(빈 타일)로 변경
                    MapNode newNode = new MapNode { Position = newPos, Type = NodeType.Normal };

                    randomExistingNode.ConnectedNodes.Add(newNode);
                    newNode.ConnectedNodes.Add(randomExistingNode);

                    _nodeDict.Add(newPos, newNode);
                    _allNodes.Add(newNode);
                }
            }
        }

        private void AssignNodeTypes()
        {
            List<MapNode> availableNodes = new List<MapNode>(_allNodes);

            // 시작 노드 제외
            availableNodes.RemoveAll(n => n.Type == NodeType.Start);

            if (availableNodes.Count == 0)
            {
                Debug.LogWarning("Total Node Count가 너무 작습니다.");
                return;
            }

            // 1. 길을 막는 장애물 군집(Cluster)을 가장 먼저 맵에 배치합니다.
            PlaceObstacleClusters(availableNodes);

            // 2. 중요도(희귀도)가 높은 타일 순서대로 거리 제약을 두며 배치합니다.
            PlaceNodesOfType(NodeType.Boss, bossCount, minBossDistance, availableNodes, minBossDistanceFromStart);
            PlaceNodesOfType(NodeType.Shop, shopCount, minShopDistance, availableNodes);
            PlaceNodesOfType(NodeType.Elite, eliteCount, minEliteDistance, availableNodes);
            PlaceNodesOfType(NodeType.Event, eventCount, minEventDistance, availableNodes);
            PlaceNodesOfType(NodeType.Battle, battleCount, minBattleDistance, availableNodes);

            // 3. 할당량이 끝난 나머지 빈칸들은 LogicalShape에서 부여한 기본값(Normal)을 그대로 유지합니다.
        }

        // 추가됨: 연속된 장애물 타일을 뭉쳐서 생성하는 군집화(Clustering) 알고리즘
        private void PlaceObstacleClusters(List<MapNode> availableNodes)
        {
            for (int i = 0; i < obstacleClusterCount; i++)
            {
                if (availableNodes.Count == 0) break;

                // 이번 군집이 가질 무작위 크기 결정
                int targetSize = Random.Range(minObstacleClusterSize, maxObstacleClusterSize + 1);

                // 군집의 중심(Seed)이 될 노드를 무작위로 선택
                MapNode seed = availableNodes[Random.Range(0, availableNodes.Count)];

                List<MapNode> cluster = new List<MapNode>();
                Queue<MapNode> queue = new Queue<MapNode>();
                HashSet<MapNode> visited = new HashSet<MapNode>();

                queue.Enqueue(seed);
                visited.Add(seed);

                // BFS(너비 우선 탐색)를 통해 인접한 노드들을 찾아 군집 크기만큼 확장
                while (queue.Count > 0 && cluster.Count < targetSize)
                {
                    MapNode current = queue.Dequeue();

                    if (availableNodes.Contains(current))
                    {
                        cluster.Add(current);
                        availableNodes.Remove(current); // 다른 타일이 배치되지 않도록 가용 목록에서 제거

                        // 자연스러운 형태(비정형)로 퍼져나가도록 인접 노드 목록을 섞음
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

                // --- 클러스터 수집 완료 후 타일 종류(나무, 바위, 물) 결정 및 적용 ---
                if (cluster.Count > 0)
                {
                    // 0: Tree, 1: Rock, 2: Water
                    int obstacleCategory = Random.Range(0, 3);

                    if (obstacleCategory == 0)
                    {
                        foreach (var node in cluster) node.Type = NodeType.Tree;
                    }
                    else if (obstacleCategory == 1)
                    {
                        foreach (var node in cluster) node.Type = NodeType.Rock;
                    }
                    else
                    {
                        // 강/호수/웅덩이 지능형 할당 알고리즘
                        if (cluster.Count == 1)
                        {
                            cluster[0].Type = NodeType.WaterPuddle;
                        }
                        else
                        {
                            // seed(최초 탐색 시작점)는 WaterStart
                            cluster[0].Type = NodeType.WaterStart;

                            // BFS로 가장 마지막에 도달한 노드는 WaterEnd
                            cluster[cluster.Count - 1].Type = NodeType.WaterEnd;

                            // 그 사이를 잇는 중간 노드들은 WaterBody
                            for (int j = 1; j < cluster.Count - 1; j++)
                            {
                                cluster[j].Type = NodeType.WaterBody;
                            }
                        }
                    }
                }
            }
        }

        // 리스트를 무작위로 섞는 유틸리티 함수 (Fisher-Yates Shuffle)
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

        // 셔플백 대신 거리 제약 할당 알고리즘을 모든 타일이 사용할 수 있도록 함수로 분리했습니다.
        // minDistanceFromStart 매개변수를 추가하여 시작점과의 거리 제약을 선택적으로 적용할 수 있게 했습니다.
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

                bool isValidDistance = true;

                if (minDistanceFromStart > 0)
                {
                    // 시작 노드의 좌표는 항상 (0,0)이므로 절대값의 합으로 맨해튼 거리를 구합니다.
                    int distFromStart = Mathf.Abs(candidate.Position.x) + Mathf.Abs(candidate.Position.y);
                    if (distFromStart < minDistanceFromStart)
                    {
                        isValidDistance = false;
                    }
                }

                if (isValidDistance)
                {
                    foreach (MapNode placed in placedNodes)
                    {
                        int distance = Mathf.Abs(candidate.Position.x - placed.Position.x) + Mathf.Abs(candidate.Position.y - placed.Position.y);
                        if (distance < minDistance)
                        {
                            isValidDistance = false;
                            break;
                        }
                    }
                }

                if (isValidDistance)
                {
                    candidate.Type = type;
                    placedNodes.Add(candidate);
                    availableNodes.Remove(candidate);
                    currentCount++;
                }
            }

            // 기획자가 설정한 수치(맵 크기는 작은데 전투 타일을 너무 많이 요구할 경우 등)의 오류를 찾아내기 위한 방어 코드
            if (currentCount < count)
            {
                Debug.LogWarning($"[MapGenerator] {type} 타일을 목표치({count}개)만큼 배치하지 못했습니다. (배치됨: {currentCount}개) - 맵의 총 노드 수를 늘리거나 최소 거리를 줄이세요.");
            }
        }

        private IEnumerator AnimateMapGeneration()
        {
            // 전체 애니메이션 시간을 노드 개수로 나누어 이전처럼 부드럽게 퍼져나가는 연출 복구
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

                // 1. 타입에 맞는 프리팹 가져오기
                GameObject targetPrefab = GetPrefabForType(currentNode.Type);

                // 안전 장치: 프리팹이 할당되지 않았을 경우 경고 출력
                if (targetPrefab == null)
                {
                    Debug.LogWarning($"[MapGenerator] {currentNode.Type} 타입의 프리팹이 인스펙터에 할당되지 않았습니다!");
                    continue;
                }

                // 기획자가 설정한 tileSpacing 변수를 곱하여 물리적인 월드 좌표 결정
                Vector3 worldPos = new Vector3(currentNode.Position.x * tileSpacing, 0, currentNode.Position.y * tileSpacing);

                // 2. 프리팹 인스턴스화
                currentNode.NodeView = Instantiate(targetPrefab, worldPos, Quaternion.identity, this.transform);

                // 3. 스케일 업 애니메이션 실행 (0.5초 동안)
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
                case NodeType.Normal:
                    return _normalPrefab;
                case NodeType.Boss: return _bossPrefab;
                case NodeType.Shop: return _shopPrefab;
                case NodeType.Event: return _eventPrefab;
                case NodeType.Elite: return _elitePrefab;
                case NodeType.Battle: return _battlePrefab;

                // 새로운 장애물 타입 반환 로직 적용
                case NodeType.Tree: return GetRandomPrefab(_treePrefabs, _normalPrefab);
                case NodeType.Rock: return GetRandomPrefab(_rockPrefabs, _normalPrefab);

                // 물 타입 할당 (할당되지 않았을 경우 에러 방지를 위해 기본 타일 반환)
                case NodeType.WaterPuddle: return _waterPuddlePrefab != null ? _waterPuddlePrefab : _normalPrefab;
                case NodeType.WaterStart: return _waterStartPrefab != null ? _waterStartPrefab : _normalPrefab;
                case NodeType.WaterEnd: return _waterEndPrefab != null ? _waterEndPrefab : _normalPrefab;
                case NodeType.WaterBody: return GetRandomPrefab(_waterBodyPrefabs, _normalPrefab);

                default: return _normalPrefab;
            }
        }

        // 리스트에서 무작위 프리팹을 안전하게 반환하는 헬퍼 함수
        private GameObject GetRandomPrefab(List<GameObject> prefabs, GameObject fallback)
        {
            if (prefabs == null || prefabs.Count == 0) return fallback;
            return prefabs[Random.Range(0, prefabs.Count)];
        }

        private IEnumerator ScaleUpNode(Transform nodeTransform, float duration)
        {
            float time = 0f;

            // 프리팹에 설정된 기본 스케일 값을 목표값으로 저장
            Vector3 targetScale = nodeTransform.localScale;

            // 시작 스케일을 0으로 초기화
            nodeTransform.localScale = Vector3.zero;

            while (time < duration)
            {
                // 애니메이션 도중 씬이 변경되거나 타일이 삭제될 경우를 대비한 방어 코드
                if (nodeTransform == null) yield break;

                time += Time.deltaTime;

                // 선형 진행률 (0.0 ~ 1.0)
                float t = time / duration;

                // 부드러운 확장을 위한 Ease-Out 계산식 적용 (처음엔 빠르고 끝에선 느리게)
                float easeOutT = t * (2f - t);

                nodeTransform.localScale = Vector3.Lerp(Vector3.zero, targetScale, easeOutT);
                yield return null;
            }

            // 루프 종료 후 목표 스케일로 정확히 맞춤
            if (nodeTransform != null)
            {
                nodeTransform.localScale = targetScale;
            }
        }
    }
}