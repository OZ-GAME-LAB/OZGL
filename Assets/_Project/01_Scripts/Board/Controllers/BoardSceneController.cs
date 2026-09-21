using OzGameLab01.Combat;
using OzGameLab01.Data;
using OzGameLab01.Managers;
using OzGameLab01.Player;
using OzGameLab01.Save;
using OzGameLab01.UI;
using OzGameLab01.Map;
using System;
using UnityEngine;
using OzGameLab01.Board.Models;
using OzGameLab01.Board.Views;
using OzGameLab01.Board.Controllers;
using OzGameLab01.Common;

namespace OzGameLab01.Controllers
{
    public sealed class BoardSceneController : MonoBehaviour, IBoardNotificationSource
    {
        [Header("보드 씬 연결")]
        [SerializeField] private BoardPlayerController _boardPlayerController;
        [SerializeField] private MapGenerator _mapGenerator;

        [Header("시간 시스템")]
        [SerializeField, Min(1)] private int _morningTurns = 8;
        [SerializeField, Min(1)] private int _lunchTurns = 2;
        [SerializeField, Min(1)] private int _eveningTurns = 5;

        [Header("시간대 명암")]
        [SerializeField] private Material _timeOfDayOverlayMaterial;
        [SerializeField] private BoardTimeOfDayPalette _timeOfDayPalette;
        [SerializeField, Min(0f)] private float _timeOfDayOverlayPadding = 0.5f;
        [SerializeField] private float _timeOfDayOverlayHeight = 0.02f;

        // 버튼 입력의 BoardUIController 전담

        [SerializeField] private NightEventPopupView _nightEventPopup;
        [SerializeField] private TimeStatusHUDView _timeStatusHud;

        [Header("전투 편성 확인")]
        [Tooltip("전투 시작 전 최소 편성 인원을 확인할 유닛 편성 컨트롤러입니다. 비워두면 씬에서 자동으로 찾습니다.")]
        [SerializeField] private UnitFormationController _unitFormationController;

        [Header("Event UI")]
        [SerializeField] private OzGameLab01.Events.EventSession _eventUIPanel;

        // ==================== 외부 시스템 통지용 이벤트 ====================

        public event Action<int> TurnEnded;
        public event Action<int> NightReached;
        public event Action<int> DayReached;
        public event Action PlayerTurnReady;
        public event Action<UnitData> UnitAcquired;
        public event Action<BoardTimeOfDay> TimeOfDayChanged;
        public BoardTimeOfDay CurrentTimeOfDay =>
            BoardTurnRules.GetTimeOfDay(BoardRunData.TurnCount, _morningTurns, _lunchTurns, _eveningTurns);
        // [추가] 저장 대기 중 중복 타이틀 이동 요청 방지
        private bool _isReturningToTitle;
        private MapNode _pendingEventNode;
        private IDisposable _eventCompletionSubscription;
        private BoardSceneFeedbackView _feedback;
        private BoardTimeOfDayOverlayView _timeOfDayOverlay;
        private MapNode _pendingBattleNode;
        private bool _pendingBattleIsBoss;
        private bool _pendingBattleIsElite;

        public event Action ForcedFormationRequested;
        public bool HasPendingBattleFormation => _pendingBattleNode != null;
        public event Action<BoardNotification> Notification;

        // 상태 확정 후 값 스냅샷 발행
        private void Publish(BoardNotificationKind kind, MapNode node = null, int unitId = 0, NodeType? tileType = null)
        {
            bool isEvening = BoardTurnRules.IsNight(BoardRunData.TurnCount, _morningTurns, _lunchTurns, _eveningTurns);

            Notification?.Invoke(new BoardNotification(kind, node != null ? node.Position : BoardRunData.PlayerPosition, tileType ?? (node != null ? node.Type : NodeType.Normal), BoardRunData.TurnCount, BoardRunData.RemainingDiceValue, unitId, BoardRunData.UnusedActionPoints, isEvening));
        }

        private void Awake()
        {
            _feedback = new BoardSceneFeedbackView(_nightEventPopup, _timeStatusHud);
            _timeOfDayOverlay = new BoardTimeOfDayOverlayView(_timeOfDayOverlayMaterial);
            if (_boardPlayerController == null)
            {
                _boardPlayerController = BoardPlayerController.Instance;
            }

            if (_boardPlayerController == null)
            {
                Debug.LogError("[BoardSceneController] BoardPlayerController를 찾을 수 없습니다.", this);
                enabled = false;
                return;
            }

            if (_mapGenerator == null)
            {
                _mapGenerator = FindFirstObjectByType<MapGenerator>();
            }

            if (_eventUIPanel == null)
            {
                _eventUIPanel = FindFirstObjectByType<OzGameLab01.Events.EventSession>(FindObjectsInactive.Include);
                if (_eventUIPanel != null)
                {
                    _eventUIPanel.gameObject.SetActive(false);
                }
            }

            // [수정] 버튼 클릭 처리는 BoardUIController가 전담
            if (_unitFormationController == null)
            {
                _unitFormationController = FindFirstObjectByType<UnitFormationController>(FindObjectsInactive.Include);
            }
        }

        private void Start()
        {
            UpdateTimeStatusHud();

            if (_mapGenerator != null && _mapGenerator.IsPresentationComplete)
            {
                RefreshTimeOfDayOverlay();
            }
        }

        private void OnEnable()
        {
            if (_boardPlayerController != null)
                _boardPlayerController.PlayerArrived += HandlePlayerArrived;

            if (_mapGenerator != null)
                _mapGenerator.PresentationCompleted += HandleMapPresentationCompleted;
        }

        private void OnDisable()
        {
            if (_boardPlayerController != null)
                _boardPlayerController.PlayerArrived -= HandlePlayerArrived;

            if (_mapGenerator != null)
                _mapGenerator.PresentationCompleted -= HandleMapPresentationCompleted;

            UnsubscribeEventCompletion();
        }

        private void OnDestroy()
        {
            _timeOfDayOverlay?.Dispose();
            _timeOfDayOverlay = null;
        }

       public void EndTurn()
{
    if (_boardPlayerController == null || !_boardPlayerController.EndTurn())
    {
        return;
    }

    SystemBus.Messages.Request<OzGameLab01.Dice.Contracts.DiceResetRequested, bool>(default);

    TurnEnded?.Invoke(BoardRunData.UnusedActionPoints);

    BoardRunData.AdvanceTurn();

    RefreshTimeOfDayOverlay();
    Publish(BoardNotificationKind.TurnAdvanced);

    bool hasTimeOfDayChanged = BoardTurnRules.ChangesPhase(BoardRunData.TurnCount, _morningTurns, _lunchTurns, _eveningTurns);

    if (hasTimeOfDayChanged)
    {
        switch (CurrentTimeOfDay)
        {
            case BoardTimeOfDay.Day:
                DayReached?.Invoke(BoardRunData.TurnCount);
                break;

            case BoardTimeOfDay.Night:
                NightReached?.Invoke(BoardRunData.TurnCount);
                break;
        }
    }

    UpdateTimeStatusHud();

    if (!hasTimeOfDayChanged)
    {
        PlayerTurnReady?.Invoke();
    }
}

       private void UpdateTimeStatusHud()
{
    int turnsUntilNextPhase = BoardTurnRules.TurnsUntilPhase(BoardRunData.TurnCount, _morningTurns, _lunchTurns, _eveningTurns);

    _feedback.ShowTurns(turnsUntilNextPhase);
}
        private void HandleMapPresentationCompleted()
        {
            RefreshTimeOfDayOverlay();
        }

        private void RefreshTimeOfDayOverlay()
        {
            BoardTimeOfDay timeOfDay = CurrentTimeOfDay;

            if (_mapGenerator != null && _timeOfDayOverlay != null && _timeOfDayPalette != null)
            {
                _timeOfDayOverlay.Show(
                    _mapGenerator.GeneratedWorldBounds,
                    _timeOfDayOverlayHeight,
                    _timeOfDayOverlayPadding,
                    _timeOfDayPalette.GetTint(timeOfDay));
            }

            TimeOfDayChanged?.Invoke(timeOfDay);
        }

        /// <summary>
        /// 보류 중인 전투의 편성 상태를 확인하고 전투 씬으로 진입합니다.
        /// </summary>
        public bool TryCompletePendingBattleFormation()
        {
            if (_pendingBattleNode == null)
            {
                return false;
            }

            if (_unitFormationController != null && !_unitFormationController.CanStartBattle)
            {
                ForcedFormationRequested?.Invoke();
                return false;
            }

            MapNode battleNode = _pendingBattleNode;
            bool isBoss = _pendingBattleIsBoss;
            bool isElite = _pendingBattleIsElite;
            if (!TryStartBattle(battleNode, isBoss, isElite))
            {
                return false;
            }

            ClearPendingBattleFormation();
            return true;
        }

        // 전투 타일 전용 강제 편성 요청
        private void RequestBattle(MapNode battleNode, bool isBoss, bool isElite = false)
        {
            if (_unitFormationController == null || _unitFormationController.CanStartBattle)
            {
                TryStartBattle(battleNode, isBoss, isElite);
                return;
            }

            _pendingBattleNode = battleNode;
            _pendingBattleIsBoss = isBoss;
            _pendingBattleIsElite = isElite;
            ForcedFormationRequested?.Invoke();
        }

        // 보류 전투 상태 초기화
        private void ClearPendingBattleFormation()
        {
            _pendingBattleNode = null;
            _pendingBattleIsBoss = false;
            _pendingBattleIsElite = false;
        }

        // 보류 여부와 무관한 실제 전투 진입
        private bool TryStartBattle(MapNode battleNode, bool isBoss, bool isElite)
        {
            if (!TryGetSceneTransitioner(out SceneTransitioner transitioner))
            {
                return false;
            }

            BoardRunData.BeginBattle(battleNode.Position, isBoss, isElite);
            Publish(BoardNotificationKind.BattleRequested, battleNode);
            transitioner.LoadCombatScene();
            return true;
        }
        /// <summary>
        /// [수정] 현재 런 저장 완료 후 타이틀 씬으로 이동
        /// </summary>
        public async void ReturnToTitle()
        {
            if (_isReturningToTitle)
            {
                return;
            }

            if (!TryGetSceneTransitioner(out SceneTransitioner transitioner)) return;
            _isReturningToTitle = true;
            Time.timeScale = 1f;
            // [수정] 타이틀 복귀는 런 포기가 아니라 Continue 저장 시점으로 처리
            SaveFacade saveFacade = SystemBus.Get<SaveFacade>();
            if (saveFacade == null)
            {
                Debug.LogError("[BoardSceneController] SaveFacade를 찾을 수 없어 런 데이터를 저장할 수 없습니다.", this);
                return;
            }
            saveFacade.CaptureCurrentRun();
            bool saved = await saveFacade.SaveAsync();
            if (!saved)
            {
                Debug.LogError("[BoardSceneController] 타이틀 복귀 전 런 데이터 저장에 실패했습니다.", this);
            }

            transitioner.LoadTitleScene();
        }

        private void HandlePlayerArrived(MapNode arrivedNode)
        {
            if (arrivedNode == null) return;
            BoardRunData.SavePlayerPosition(arrivedNode.Position);
            Publish(BoardNotificationKind.PlayerArrived, arrivedNode);

            if (!BoardTileRules.CanProcess(arrivedNode, BoardRunData.IsSpecialTileConsumed(arrivedNode.Position)))
            {
                NormalizeConsumedNode(arrivedNode);
                return;
            }

            switch (arrivedNode.Type)
            {
                case NodeType.Battle: HandleBattleNode(arrivedNode, false); break;
                case NodeType.Elite: HandleBattleNode(arrivedNode, true); break;
                case NodeType.Boss: HandleBossNode(arrivedNode); break;
                case NodeType.Event: HandleEventNode(arrivedNode); break;
                case NodeType.UnitAcquisition:
                    if (HandleUnitAcquisitionNode(out int acquiredUnitId))
                    {
                        ConsumeSpecialNode(arrivedNode);
                        Publish(BoardNotificationKind.UnitGranted, arrivedNode, acquiredUnitId, NodeType.UnitAcquisition);
                    }
                    break;
                case NodeType.Shop:
                    // 상점 구현 전 도착 시 일회성 소비
                    ConsumeSpecialNode(arrivedNode);
                    break;
            }
        }

        private void HandleEventNode(MapNode eventNode)
        {
            OzGameLab01.Events.EventFacade eventFacade = SystemBus.Get<OzGameLab01.Events.EventFacade>();
            if (_eventUIPanel != null && eventFacade != null)
            {
                // 진행 중 이벤트의 중복 시작 방지
                if (_pendingEventNode != null) { return; }
                _pendingEventNode = eventNode;
                _eventCompletionSubscription = SystemBus.Messages.Subscribe<OzGameLab01.Events.Contracts.EventChoiceCompleted>(
                    _ => HandleEventCompleted());

                if (eventFacade.OpenRandomEvent())
                {
                    return;
                }

                UnsubscribeEventCompletion();
            }

            _feedback.Show("Event data is not configured yet.", false);
        }

        private void HandleEventCompleted()
        {
            MapNode completedNode = _pendingEventNode;
            UnsubscribeEventCompletion();
            ConsumeSpecialNode(completedNode);
        }

        private void UnsubscribeEventCompletion()
        {
            _eventCompletionSubscription?.Dispose();
            _eventCompletionSubscription = null;
            _pendingEventNode = null;
        }

        private void HandleBattleNode(MapNode battleNode, bool isElite)
        {
            if (BoardRunData.IsBattleCompleted(battleNode.Position)) return;
            RequestBattle(battleNode, false, isElite);
        }

        private void HandleBossNode(MapNode bossNode)
        {
            RequestBattle(bossNode, true);
        }

        private bool TryGetSceneTransitioner(out SceneTransitioner transitioner)
        {
            transitioner = SceneTransitioner.Instance;
            return transitioner != null && !transitioner.IsTransitioning;
        }

        private bool HandleUnitAcquisitionNode(out int acquiredUnitId)
        {
            acquiredUnitId = 0;
            UnitData selected = BoardUnitSelection.Select(OzGameLab01.Data.RuntimeContent.Catalog.Units, count => UnityEngine.Random.Range(0, count));
            if (selected == null)
            {
                Debug.LogWarning("[BoardSceneController] 획득 가능한 유닛 데이터가 없습니다.", this);
                return false;
            }
            PlayerFacade playerFacade = SystemBus.Get<PlayerFacade>();
            if (playerFacade == null)
            {
                Debug.LogWarning("[BoardSceneController] 유닛 지급 대상 인벤토리가 없습니다.", this);
                return false;
            }
            UnitData acquired = PlayerFacade.CloneUnitData(selected);
            playerFacade.AddUnit(acquired);
            UnitAcquired?.Invoke(acquired);
            acquiredUnitId = acquired.id;
            return true;
        }

        private void ConsumeSpecialNode(MapNode node)
        {
            if (node == null || BoardRunData.IsSpecialTileConsumed(node.Position)) { return; }
            NodeType consumedType = node.Type;
            if (_mapGenerator == null) { _mapGenerator = FindFirstObjectByType<MapGenerator>(); }
            if (_mapGenerator != null)
            {
                if (!_mapGenerator.ConsumeSpecialTile(node)) { return; }
            }
            else
            {
                BoardRunData.ConsumeSpecialTile(node.Position);
                node.Type = NodeType.Normal;
            }
            Publish(BoardNotificationKind.SpecialTileConsumed, node, tileType: consumedType);
        }

        private void NormalizeConsumedNode(MapNode node)
        {
            if (!BoardTileRules.IsSingleUse(node.Type)) { return; }
            if (_mapGenerator == null) { _mapGenerator = FindFirstObjectByType<MapGenerator>(); }
            if (_mapGenerator != null) { _mapGenerator.NormalizeConsumedSpecialTile(node); }
        }
    }
}
