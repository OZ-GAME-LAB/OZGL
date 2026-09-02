using System;
using OzGameLab01.Data;
using OzGameLab01.Map;
using OzGameLab01.UI;
using OZGL.Map;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace OzGameLab01.Controllers
{
    public class BoardPlayerController : MonoBehaviour
    {
        public static BoardPlayerController Instance { get; private set; }

        [Header("Player Visuals")]
        [Tooltip("보드 위에서 플레이어를 상징할 3D 토큰(말) 프리팹을 연결하세요.")]
        [SerializeField] private GameObject _playerTokenPrefab;
        private GameObject _tokenInstance;

        [Header("Player State")]
        [SerializeField] private int _currentDiceValue = 0;
        [Tooltip("남은 행동력을 상시 표시하는 HUD입니다. 비워두면 씬에서 자동으로 찾거나 새로 생성합니다.")]
        [SerializeField] private ActionPowerHUDView _actionPowerHud;
        private MapNode _currentNode;
        private bool _isMoving = false;

        // DiceManager 등 외부에서 상태를 확인하기 위한 프로퍼티 (읽기 전용)
        public bool IsMoving => _isMoving;
        public int CurrentDiceValue => _currentDiceValue;

        // ==================== 보드 도착 이벤트 추가 ====================

        /// <summary>
        /// 플레이어가 보드 이동을 모두 완료했을 때 호출되는 이벤트입니다.
        /// 도착한 MapNode를 전달하며, 씬 전환과 이벤트 타일 처리는 외부 컨트롤러가 담당합니다.
        /// </summary>
        public event Action<MapNode> PlayerArrived;

        [Header("Movement Settings")]
        [SerializeField] private float _moveSpeed = 5f;
        [SerializeField] private float _shakeIntensity = 0.3f;
        [SerializeField] private float _shakeDuration = 0.4f;

        // 캐싱 데이터
        private TileView _currentHoveredTile = null;
        private List<MapNode> _validPath = null;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        public void SetupPlayer(MapNode startNode)
        {
            _currentNode = startNode;

            // MapGenerator를 찾아 기획자가 설정한 타일 간격을 가져옵니다 (기본값 2f).
            float spacing = 2f;

            // 최신 유니티 권장 사항에 맞추어 FindFirstObjectByType으로 변경 (경고 해결)
            OZGL.Map.MapGenerator mapGen = FindFirstObjectByType<OZGL.Map.MapGenerator>();
            if (mapGen != null) spacing = mapGen.tileSpacing;

            // 컨트롤러의 논리적 위치를 시작 타일에 정확하게 맞춤
            transform.position = new Vector3(startNode.Position.x * spacing, 0.5f, startNode.Position.y * spacing);

            // 시각적 토큰 프리팹을 이 컨트롤러의 자식으로 생성
            if (_playerTokenPrefab != null && _tokenInstance == null)
            {
                // 위치와 회전을 초기화하여 컨트롤러(부모)의 위치를 완벽하게 따라가도록 설정
                _tokenInstance = Instantiate(_playerTokenPrefab, transform.position, Quaternion.identity, this.transform);

                // 만약 프리팹 자체에 어색한 오프셋이 있다면 아래 코드로 강제 중앙 정렬
                _tokenInstance.transform.localPosition = Vector3.zero;
            }
        }

        // UI 버튼 등을 통해 주사위를 굴렸을 때 호출됩니다.
        public void SetDiceValue(int value)
        {
            _currentDiceValue = value;
            Debug.Log($"주사위 눈금: {value}");

            RefreshActionPowerHud();
        }

        /// <summary>
        /// 남은 행동력 HUD 표시를 현재 값 기준으로 갱신합니다.
        /// </summary>
        private void RefreshActionPowerHud()
        {
            if (_actionPowerHud == null)
            {
                _actionPowerHud = FindFirstObjectByType<ActionPowerHUDView>();
            }

            if (_actionPowerHud == null)
            {
                _actionPowerHud = new GameObject("ActionPowerHUD").AddComponent<ActionPowerHUDView>();
            }

            _actionPowerHud.SetActionPower(_currentDiceValue);
        }

        /// <summary>
        /// 현재 턴을 종료하고 사용하지 않은 행동력을 저장합니다.
        /// </summary>
        public bool EndTurn()
        {
            if (_isMoving)
            {
                Debug.LogWarning(
                    "[BoardPlayerController] 이동 중에는 턴을 종료할 수 없습니다.");
                return false;
            }

            int unusedActionPoints = Mathf.Max(0, _currentDiceValue);
            BoardRunData.SaveUnusedActionPoints(unusedActionPoints);

            _currentDiceValue = 0;
            _currentHoveredTile?.ResetHighlight();
            _currentHoveredTile = null;
            _validPath = null;

            Debug.Log(
                $"[BoardPlayerController] 턴 종료 | " +
                $"남은 행동력: {unusedActionPoints}", this);

            return true;
        }

        public void OnTileHovered(TileView tile)
        {
            if (_isMoving || _currentDiceValue <= 0) return;

            // --- 안전망(Fail-safe) 추가 ---
            if (MapManager.Instance == null)
            {
                Debug.LogError("[BoardPlayerController] 맵 매니저를 찾을 수 없습니다! 씬에 MapManager 오브젝트가 있는지 확인하세요.");
                return;
            }
            if (_currentNode == null)
            {
                Debug.LogWarning("[BoardPlayerController] 플레이어의 현재 위치가 설정되지 않았습니다.");
                return;
            }
            // -----------------------------

            _currentHoveredTile = tile;
            MapNode targetNode = tile.MyNode;

            // MapManager를 통해 현재 위치에서 목표까지 경로가 있는지, 거리가 주사위 이내인지 검사
            _validPath = MapManager.Instance.FindPath(_currentNode, targetNode, _currentDiceValue);

            bool isReachable = (_validPath != null && _validPath.Count > 0);
            tile.SetHighlight(isReachable);
        }

        public void ClearHover()
        {
            // 방어막: 플레이어가 이동 중일 때는 마우스가 벗어나도 데이터를 날리지 않음!
            if (_isMoving) return;

            _currentHoveredTile = null;
            _validPath = null;
        }

        public void OnTileClicked(TileView tile)
        {
            if (_isMoving || _currentDiceValue <= 0) return;

            if (_validPath != null && _validPath.Count > 0)
            {
                // 이동 가능
                StartCoroutine(MoveAlongPathRoutine());
            }
            else
            {
                // 이동 불가능 (장애물이거나 너무 멂)
                StartCoroutine(ShakeRoutine());
                ShowCannotMoveUI();
            }
        }

        private IEnumerator MoveAlongPathRoutine()
        {
            _isMoving = true;
            _currentHoveredTile?.ResetHighlight(); // 이동 시작 시 하이라이트 해제

            // ★ 핵심 해결책: 마우스 이벤트와 꼬이지 않도록 경로를 복사해둡니다.
            List<MapNode> pathToMove = new List<MapNode>(_validPath);

            // 복사본을 만들었으니 원본 호버 데이터는 이제 안전하게 초기화합니다.
            _validPath = null;

            foreach (MapNode node in pathToMove)
            {
                // ★ 수학적 계산(* 2f)을 없애고, 실제 3D 타일 모델의 월드 위치를 가져옵니다.
                Vector3 targetPos = node.NodeView.transform.position;
                targetPos.y = 0.5f; // 타일 위로 띄우는 높이 고정

                while (Vector3.Distance(transform.position, targetPos) > 0.01f)
                {
                    transform.position = Vector3.MoveTowards(transform.position, targetPos, _moveSpeed * Time.deltaTime);
                    yield return null;
                }

                transform.position = targetPos;
                _currentNode = node;
                _currentDiceValue--; // 한 칸 갈 때마다 주사위 소모
                RefreshActionPowerHud();
            }

            _isMoving = false;

            // _validPath.Clear(); <--- 이 줄은 삭제되었습니다! (위에서 복사본을 썼기 때문)

            // 이동 완료 후 도착한 타일의 이벤트(전투, 상점 등) 실행
            //Debug.Log($"도착한 타일: {_currentNode.Type}");
            // 이동 완료 후 도착한 타일 정보 출력

            Debug.Log($"[BoardPlayerController] 도착한 타일 | Position: {_currentNode.Position}, " +
                $"Type: {_currentNode.Type}", this);

            // ==================== 보드 도착 이벤트 추가 ====================

            // 씬 전환과 타일 이벤트 처리를 담당하는 외부 컨트롤러에
            // 최종 도착 노드를 전달
            PlayerArrived?.Invoke(_currentNode);

            // GameManager.Instance.OnPlayerArrivedAt(_currentNode);
        }

        private IEnumerator ShakeRoutine()
        {
            _isMoving = true;
            float elapsed = 0f;
            Vector3 originalPos = transform.position;

            while (elapsed < _shakeDuration)
            {
                // X, Z 축으로만 진동 (사인파 활용)
                float xOffset = Mathf.Sin(Time.time * 50f) * _shakeIntensity;
                float zOffset = Mathf.Cos(Time.time * 60f) * _shakeIntensity;

                transform.position = originalPos + new Vector3(xOffset, 0, zOffset);

                elapsed += Time.deltaTime;
                yield return null;
            }

            transform.position = originalPos; // 원래 자리로 복구
            _isMoving = false;
        }

        private void ShowCannotMoveUI()
        {
            // 실제 구현에서는 UIManager를 호출하여 플로팅 텍스트를 띄웁니다.
            Debug.LogWarning("이동 불가!");
            // UIManager.Instance.ShowFloatingText(transform.position, "이동 불가!");
        }
    }
}
