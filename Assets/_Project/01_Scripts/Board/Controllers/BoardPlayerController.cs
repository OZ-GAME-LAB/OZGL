using System;
using System.Collections;
using System.Collections.Generic;
using OzGameLab01.Board.Controllers;
using OzGameLab01.Board.Models;
using OzGameLab01.Board.Views;
using OzGameLab01.Data;
using OzGameLab01.Map;
using OzGameLab01.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using OzGameLab01.Common;

namespace OzGameLab01.Controllers
{
    /// <summary>
    /// 보드 입력을 모델에 전달하고 이동 연출과 기존 외부 API를 조율합니다.
    /// </summary>
    public class BoardPlayerController : MonoBehaviour, IBoardTileInput
    {
        public static BoardPlayerController Instance { get; private set; }

        [Header("Player Visuals")]
        [SerializeField] private GameObject _playerTokenPrefab;
        [Header("New Game Spawn Presentation")]
        [Tooltip("새 게임 최초 진입 시 플레이어 토큰이 떨어지기 시작하는 높이입니다.")]
        [Min(0f)] [SerializeField] private float _spawnDropHeight = 8f;
        [Tooltip("공중에서 스타트 노드까지 착지하는 데 걸리는 시간입니다.")]
        [Min(0f)] [SerializeField] private float _spawnAnimationDuration = 2f;
        [Tooltip("낙하 중 토큰이 초당 회전하는 각도입니다.")]
        [Min(0f)] [SerializeField] private float _spawnRotationSpeed = 720f;
        [Tooltip("토큰의 로컬 좌표계를 기준으로 한 회전축입니다.")]
        [SerializeField] private Vector3 _spawnRotationAxis = Vector3.right;
        [Header("Player State")]
        // 기존 Inspector 직렬화 필드 호환용 표시 값
        [SerializeField] private int _currentDiceValue = 0;
        [SerializeField] private ActionPowerHUDView _actionPowerHud;
        [Header("Movement Settings")]
        [SerializeField] private float _moveSpeed = 5f;
        [SerializeField] private float _shakeIntensity = 0.3f;
        [SerializeField] private float _shakeDuration = 0.4f;

        private BoardMovementModel _model;
        private BoardPlayerView _view;
        private MapGenerator _mapGenerator;
        private TileView _hoveredTile;
        private bool _isFeedbackPlaying;
        private BoardDiceRequestAdapter _diceRequests;
        private Coroutine _spawnAnimationRoutine;

        private BoardMovementModel Model
        {
            get
            {
                if (_model == null)
                {
                    _model = new BoardMovementModel(new BoardRunMovementState());
                }
                return _model;
            }
        }

        public bool IsMoving => Model.IsMoving || _isFeedbackPlaying;
        public MapNode CurrentNode => Model.CurrentNode;

        /// <summary>범위 표시는 현재 이동 모델의 판정 결과만 사용합니다.</summary>
        public IReadOnlyList<MapNode> GetReachableNodes()
        {
            return IsMoving ? Array.Empty<MapNode>() : Model.GetReachableNodes();
        }

        public IReadOnlyDictionary<MapNode, int> GetReachableNodeDistances()
        {
            return IsMoving
                ? new Dictionary<MapNode, int>()
                : Model.GetReachableNodeDistances();
        }

        public int CurrentDiceValue
        {
            get => Model.RemainingDiceValue;
            set => SetCurrentDiceValue(value);
        }

        public void SetCurrentDiceValue(int value, bool refreshHud = true)
        {
            Model.SetRemainingDiceValue(value);

            if (refreshHud)
            {
                RefreshActionPowerHud();
            }
        }

        public event Action<MapNode> PlayerArrived;
        public static event Action OnPlayerStartedMoving;
        public static event Action OnPlayerFinishedMoving;
        public static event Action OnPlayerStepCompleted;
        public static event Action OnPlayerSetupCompleted;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            _view = new BoardPlayerView(transform);
            _diceRequests = new BoardDiceRequestAdapter(SystemBus.Messages);
        }

        public void SetupPlayer(MapNode startNode, bool playSpawnAnimation = false)
        {
            if (startNode == null)
            {
                return;
            }
            Model.Setup(startNode);
            RefreshActionPowerHud();
            _mapGenerator = FindFirstObjectByType<MapGenerator>();
            float spacing = _mapGenerator != null ? _mapGenerator.tileSpacing : 2f;
            _view.Setup(new Vector3(startNode.Position.x * spacing, 0.5f, startNode.Position.y * spacing), _playerTokenPrefab);

            StopSpawnAnimation();
            if (playSpawnAnimation && _spawnAnimationDuration > 0f && _spawnDropHeight > 0f)
            {
                _spawnAnimationRoutine = StartCoroutine(PlaySpawnAnimationRoutine());
            }
            else
            {
                _view.CompleteSpawnAnimation();
            }
            OnPlayerSetupCompleted?.Invoke();
        }

        private IEnumerator PlaySpawnAnimationRoutine()
        {
            _isFeedbackPlaying = true;
            bool canSkip = Mouse.current == null || !Mouse.current.leftButton.isPressed;

            bool SkipRequested()
            {
                Mouse mouse = Mouse.current;
                if (!canSkip)
                {
                    canSkip = mouse == null || !mouse.leftButton.isPressed;
                    return false;
                }

                return mouse != null && mouse.leftButton.wasPressedThisFrame;
            }

            try
            {
                yield return _view.PlaySpawnAnimation(
                    _spawnDropHeight,
                    _spawnAnimationDuration,
                    _spawnRotationSpeed,
                    _spawnRotationAxis,
                    SkipRequested);
            }
            finally
            {
                _view.CompleteSpawnAnimation();
                _isFeedbackPlaying = false;
                _spawnAnimationRoutine = null;
            }
        }

        private void StopSpawnAnimation()
        {
            if (_spawnAnimationRoutine != null)
            {
                StopCoroutine(_spawnAnimationRoutine);
                _spawnAnimationRoutine = null;
            }

            _view?.CompleteSpawnAnimation();
            _isFeedbackPlaying = false;
        }

        public void RefreshActionPowerHud()
        {
            _currentDiceValue = Model.RemainingDiceValue;
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

        public bool EndTurn()
        {
            if (IsMoving || !Model.TryEndTurn(out int unusedActionPoints))
            {
                return false;
            }
            BoardRunData.SaveUnusedActionPoints(unusedActionPoints);
            ClearHover();
            RefreshActionPowerHud();
            return true;
        }

        public void OnTileHovered(TileView tile)
        {
            if (IsMoving || CurrentDiceValue <= 0 || tile == null)
            {
                return;
            }
            ClearHover();
            _hoveredTile = tile;
            List<MapNode> path = Model.FindPath(tile.MyNode);
            tile.SetHighlight(path != null && path.Count > 0);
        }

        public void ClearHover()
        {
            if (IsMoving)
            {
                return;
            }
            _hoveredTile?.ResetHighlight();
            _hoveredTile = null;
        }

        public void OnTileClicked(TileView tile)
        {
            if (IsMoving || CurrentDiceValue <= 0 || tile == null)
            {
                return;
            }
            // 호버 캐시 대신 실제 클릭 대상 기준 경로 판정
            if (Model.TryBeginMovement(tile.MyNode, out IReadOnlyList<MapNode> path))
            {
                _hoveredTile?.ResetHighlight();
                _hoveredTile = null;
                OnPlayerStartedMoving?.Invoke();
                StartCoroutine(MoveAlongPathRoutine(path));
            }
            else
            {
                StartCoroutine(ShakeRoutine());
                Debug.LogWarning("이동 불가!");
            }
        }

        private IEnumerator MoveAlongPathRoutine(IReadOnlyList<MapNode> path)
        {
            bool completed = false;
            try
            {
                foreach (MapNode node in path)
                {
                    GameObject nodeView = _mapGenerator != null ? _mapGenerator.GetNodeView(node) : null;
                    if (nodeView == null)
                    {
                        yield break;
                    }
                    yield return _view.MoveTo(nodeView.transform.position, _moveSpeed);
                    Model.CompleteStep(node);
                    RefreshActionPowerHud();
                    OnPlayerStepCompleted?.Invoke();

                    if (node.IsMandatoryStop)
                    {
                        node.IsMandatoryStop = false;
                        break;
                    }
                }
                completed = true;
            }
            finally
            {
                Model.CancelMovement();
                OnPlayerFinishedMoving?.Invoke();
            }
            if (completed)
            {
                PlayerArrived?.Invoke(Model.CurrentNode);
            }
        }

        private IEnumerator ShakeRoutine()
        {
            _isFeedbackPlaying = true;
            try
            {
                yield return _view.Shake(_shakeDuration, _shakeIntensity);
            }
            finally
            {
                _isFeedbackPlaying = false;
            }
        }

        private void OnDisable()
        {
            StopSpawnAnimation();
            StopAllCoroutines();
            _view?.ResetPosition();
            _model?.CancelMovement();
            _isFeedbackPlaying = false;
            ClearHover();
        }

        private void OnDestroy()
        {
            _diceRequests?.Dispose();
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}
