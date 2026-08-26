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
        Boss // 추가됨: 보스 타일
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

        [Header("Tile Counts")]
        public int bossCount = 1; // 추가됨
        public int shopCount = 3;
        public int eliteCount = 3;
        public int eventCount = 8;
        public int battleCount = 15;

        [Header("Minimum Distances (Manhattan)")]
        public int minBossDistance = 5; // 추가됨
        public int minShopDistance = 3;
        public int minEliteDistance = 3; // 추가됨
        public int minEventDistance = 2; // 추가됨
        public int minBattleDistance = 1; // 추가됨 (1이면 인접 가능, 2 이상이면 떨어짐)

        [Header("Prefabs (Type-specific)")]
        [SerializeField] private GameObject _normalPrefab;
        [SerializeField] private GameObject _bossPrefab; // 추가됨
        [SerializeField] private GameObject _battlePrefab;
        [SerializeField] private GameObject _eventPrefab;
        [SerializeField] private GameObject _shopPrefab;
        [SerializeField] private GameObject _elitePrefab;

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

            // 중요도(희귀도)가 높은 타일 순서대로 거리 제약을 두며 배치합니다. (보스 -> 상점 -> 엘리트 -> 이벤트 -> 전투)
            PlaceNodesOfType(NodeType.Boss, bossCount, minBossDistance, availableNodes);
            PlaceNodesOfType(NodeType.Shop, shopCount, minShopDistance, availableNodes);
            PlaceNodesOfType(NodeType.Elite, eliteCount, minEliteDistance, availableNodes);
            PlaceNodesOfType(NodeType.Event, eventCount, minEventDistance, availableNodes);
            PlaceNodesOfType(NodeType.Battle, battleCount, minBattleDistance, availableNodes);

            // 할당량이 끝난 나머지 빈칸들은 LogicalShape에서 부여한 기본값(Normal)을 그대로 유지합니다.
        }

        // 셔플백 대신 거리 제약 할당 알고리즘을 모든 타일이 사용할 수 있도록 함수로 분리했습니다.
        private void PlaceNodesOfType(NodeType type, int count, int minDistance, List<MapNode> availableNodes)
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
                foreach (MapNode placed in placedNodes)
                {
                    int distance = Mathf.Abs(candidate.Position.x - placed.Position.x) + Mathf.Abs(candidate.Position.y - placed.Position.y);
                    if (distance < minDistance)
                    {
                        isValidDistance = false;
                        break;
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
                case NodeType.Boss: return _bossPrefab; // 추가됨
                case NodeType.Shop: return _shopPrefab;
                case NodeType.Event: return _eventPrefab;
                case NodeType.Elite: return _elitePrefab;
                case NodeType.Battle: return _battlePrefab;
                default: return _normalPrefab;
            }
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