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
        private TileView _hoveredTile;
        private bool _isFeedbackPlaying;

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
        public int CurrentDiceValue
        {
            get => Model.RemainingDiceValue;
            set
            {
                Model.SetRemainingDiceValue(value);
                RefreshActionPowerHud();
            }
        }

        public event Action<MapNode> PlayerArrived;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            _view = new BoardPlayerView(transform);
        }

        public void SetupPlayer(MapNode startNode)
        {
            if (startNode == null)
            {
                return;
            }
            Model.Setup(startNode);
            RefreshActionPowerHud();
            MapGenerator generator = FindFirstObjectByType<MapGenerator>();
            float spacing = generator != null ? generator.tileSpacing : 2f;
            _view.Setup(new Vector3(startNode.Position.x * spacing, 0.5f, startNode.Position.y * spacing), _playerTokenPrefab);
        }

        private void RefreshActionPowerHud()
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
                    if (node.NodeView == null)
                    {
                        yield break;
                    }
                    yield return _view.MoveTo(node.NodeView.transform.position, _moveSpeed);
                    Model.CompleteStep(node);
                    RefreshActionPowerHud();
                }
                completed = true;
            }
            finally
            {
                Model.CancelMovement();
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
            StopAllCoroutines();
            _view?.ResetPosition();
            _model?.CancelMovement();
            _isFeedbackPlaying = false;
            ClearHover();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}
