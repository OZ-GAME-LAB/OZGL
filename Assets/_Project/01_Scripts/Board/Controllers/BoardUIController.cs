using System;
using DG.Tweening;
using UnityEngine;
using OzGameLab01.Dice.Contracts;
using OzGameLab01.Board.Models;
using OzGameLab01.Board.Views;
using TMPro;
using OzGameLab01.UI;
using OzGameLab01.Data;
using OzGameLab01.Map;
using OzGameLab01.Board.Contracts;

namespace OzGameLab01.Controllers
{
    public class BoardUIController : MonoBehaviour
    {
        [Header("Master View")]
        public ReadySceneView readySceneView;

        [Header("Scene Controller")]
        public BoardSceneController boardSceneController;

        [Header("UI Dependencies")]
        public TextMeshProUGUI resultText;
        public TextMeshProUGUI warningText;

        [Header("Locate")]
        public MapRouteDirector mapRouteDirector;
        public BoardCameraController boardCameraController;

        [Header("Turn Sequence")]
        [Tooltip("끄면 턴 시작 시 RollView를 자동으로 열지 않습니다. 튜토리얼 Step 등에서 RequestRollViewOpen을 호출해 열 수 있습니다.")]
        [SerializeField] private bool automaticRollViewEnabled = true;
        [Min(0f)][SerializeField] private float initialAutoRollViewDelay = 0.5f;
        [Min(0f)][SerializeField] private float turnAutoRollViewDelay = 0.5f;
        [Min(0f)][SerializeField] private float timeOfDayFeedbackDuration = 1.5f;
        [SerializeField] private string nightMessage = "Night Has Come";
        [SerializeField] private string noonMessage = "Noon Has Come";
        [SerializeField] private string dayMessage = "Day Has Come";

        [Header("Clock Rotation (Bow String Anim)")]
        [Tooltip("비워두면 MainView 하위에서 RotatingVisual 오브젝트를 자동으로 찾습니다.")]
        [SerializeField] private RectTransform clockRotatingVisual;
        [SerializeField] private float dayClockAngle = 45f;
        [SerializeField] private float nightClockAngle = 220f;
        
        [Header("1. Catch (덜컥)")]
        [SerializeField] private float catchAngleOffset = 15f;
        [SerializeField, Min(0f)] private float catchDuration = 0.1f;
        [SerializeField] private Ease catchEase = Ease.OutQuint;

        [Header("2. Pullback (시위 당김)")]
        [SerializeField] private float pullbackAngleOffset = -30f;
        [SerializeField, Min(0f)] private float pullbackDuration = 0.6f;
        [SerializeField] private Ease pullbackEase = Ease.InOutSine;

        [Header("3. Shoot (발사)")]
        [SerializeField, Min(0f)] private float shootDuration = 0.4f;
        [SerializeField] private Ease shootEase = Ease.OutBack;

        [Header("Settings")]
        public float rollViewCloseDelay = 1.0f;
        public float warningTextDuration = 2.0f;

        private Coroutine _automaticRollViewRoutine;
        private Coroutine _timeOfDayFeedbackRoutine;
        private BoardFeedbackView _feedbackView;
        private System.IDisposable _diceSubscription;
        private Sequence _clockRotationSequence;
        private bool _started;


        public static event Action OnRollViewClosed;
        private void Awake()
        {
            _feedbackView = new BoardFeedbackView(resultText, warningText);
        }

        private void Start()
        {
            _started = true;
            BindEvents();
            ScheduleAutomaticRollView(initialAutoRollViewDelay);
        }

        private void OnEnable()
        {
            if (_started)
            {
                BindEvents();
                ScheduleAutomaticRollView(initialAutoRollViewDelay);
            }
        }

        private void BindEvents()
        {
            if (mapRouteDirector == null) mapRouteDirector = FindFirstObjectByType<MapRouteDirector>();
            if (boardCameraController == null) boardCameraController = FindFirstObjectByType<BoardCameraController>();

            _ = Managers.DiceManager.Instance.Facade; // 씬 직접 실행 시 시스템 구성
            _diceSubscription?.Dispose();
            _diceSubscription = SystemBus.Messages.Subscribe<DiceRolled>(message => HandleDiceRolled(message.Value));
            BoardPlayerController.OnPlayerFinishedMoving -= HandlePlayerFinishedMoving;
            BoardPlayerController.OnPlayerFinishedMoving += HandlePlayerFinishedMoving;
            BoardPlayerController.OnPlayerStepCompleted -= HandlePlayerStepCompleted;
            BoardPlayerController.OnPlayerStepCompleted += HandlePlayerStepCompleted;

            if (readySceneView != null)
            {
                if (readySceneView.RollView != null)
                    readySceneView.RollView.RollClicked += HandleRollButtonClicked;

                if (readySceneView.MainView != null)
                {
                    readySceneView.MainView.UnitClicked += HandleUnitButtonClicked;
                    readySceneView.MainView.SettingsClicked += HandleSettingsButtonClicked;
                    readySceneView.MainView.LocateClicked += HandleLocateButtonClicked;
                    readySceneView.MainView.EndTurnClicked += HandleEndTurnButtonClicked;

                    // [수정됨] 시작할 때 "현재 플레이 중인 턴(경과 턴 + 1)"을 표시합니다.
                    int initialTurn = BoardTurnRules.DisplayTurn(BoardRunData.TurnCount);
                    readySceneView.MainView.SetCurrentTurn(initialTurn);
                    ApplyInitialClockRotation();

                    RefreshEndTurnFeedback(true);
                }

                if (readySceneView.SettingsView != null)
                {
                    readySceneView.SettingsView.BackClicked += HandleSettingsBackClicked;
                    readySceneView.SettingsView.ReturnToMainClicked += HandleReturnToTitleClicked;
                }

                if (readySceneView.UnitView != null)
                {
                    readySceneView.UnitView.CloseClicked += HandleUnitCloseClicked;
                }
            }

            if (boardSceneController != null)
            {
                boardSceneController.TurnEnded += HandleTurnEnded;
                boardSceneController.NightReached += HandleNightReached;
                boardSceneController.NoonReached += HandleNoonReached;
                boardSceneController.DayReached += HandleDayReached;
                boardSceneController.PlayerTurnReady += HandlePlayerTurnReady;
                boardSceneController.UnitAcquired += HandleUnitAcquired;
            }

        }

        private void OnDisable()
        {
            StopAllCoroutines();
            StopClockRotation();
            _automaticRollViewRoutine = null;
            readySceneView?.MainView?.SetInteractable(true);
            _timeOfDayFeedbackRoutine = null;
            _feedbackView?.HideWarning();

            _diceSubscription?.Dispose();
            _diceSubscription = null;
            BoardPlayerController.OnPlayerFinishedMoving -= HandlePlayerFinishedMoving;
            BoardPlayerController.OnPlayerStepCompleted -= HandlePlayerStepCompleted;

            if (readySceneView != null)
            {
                if (readySceneView.RollView != null)
                    readySceneView.RollView.RollClicked -= HandleRollButtonClicked;

                if (readySceneView.MainView != null)
                {
                    readySceneView.MainView.UnitClicked -= HandleUnitButtonClicked;
                    readySceneView.MainView.SettingsClicked -= HandleSettingsButtonClicked;
                    readySceneView.MainView.LocateClicked -= HandleLocateButtonClicked;
                    readySceneView.MainView.EndTurnClicked -= HandleEndTurnButtonClicked;
                }

                if (readySceneView.SettingsView != null)
                {
                    readySceneView.SettingsView.BackClicked -= HandleSettingsBackClicked;
                    readySceneView.SettingsView.ReturnToMainClicked -= HandleReturnToTitleClicked;
                }

                if (readySceneView.UnitView != null)
                {
                    readySceneView.UnitView.CloseClicked -= HandleUnitCloseClicked;
                }
            }

            if (boardSceneController != null)
            {
                boardSceneController.TurnEnded -= HandleTurnEnded;
                boardSceneController.NightReached -= HandleNightReached;
                boardSceneController.NoonReached -= HandleNoonReached;
                boardSceneController.DayReached -= HandleDayReached;
                boardSceneController.PlayerTurnReady -= HandlePlayerTurnReady;
                boardSceneController.UnitAcquired -= HandleUnitAcquired;
            }
        }

        public void ToggleRollView()
        {
            if (readySceneView == null || readySceneView.RollView == null) return;

            bool isActive = !readySceneView.RollView.IsVisible;
            if (isActive)
            {
                if (_timeOfDayFeedbackRoutine != null)
                {
                    return;
                }

                if (!TryOpenRollView())
                {
                    if (SystemBus.Messages.Request<DiceSnapshotRequested, DiceSnapshot>(default).HasRolledThisTurn)
                    {
                        ShowWarning("Please end the turn first!!");
                    }
                }
            }
            else
            {
                readySceneView.HideRollView();
            }
        }

        /// <summary>
        /// 자동 표시 설정과 무관하게 RollView 표시를 요청합니다.
        /// 플레이어 등장·이동 연출 중이면 입력 가능한 상태가 될 때까지 기다립니다.
        /// </summary>
        public void RequestRollViewOpen(Action onOpened = null)
        {
            CancelAutomaticRollView();

            if (!isActiveAndEnabled)
            {
                return;
            }

            RollViewOpenResult immediateResult = TryOpenRollViewInternal();

            if (immediateResult == RollViewOpenResult.Opened)
            {
                onOpened?.Invoke();
                return;
            }

            if (immediateResult == RollViewOpenResult.Blocked)
            {
                return;
            }

            _automaticRollViewRoutine =
                StartCoroutine(OpenRollViewWhenAvailableRoutine(0f, onOpened));
        }

        private void HandleUnitButtonClicked(ReadyMainView view)
        {
            if (readySceneView == null) return;
            readySceneView.HideAllOverlayViews();
            readySceneView.ShowUnitView();
        }

        private void HandleSettingsButtonClicked(ReadyMainView view)
        {
            if (readySceneView == null) return;
            readySceneView.HideAllOverlayViews();
            readySceneView.ShowSettingsView();
        }

        private void HandleLocateButtonClicked(ReadyMainView view)
        {
            if (mapRouteDirector == null)
            {
                mapRouteDirector = FindFirstObjectByType<MapRouteDirector>();
            }

            if (boardCameraController == null)
            {
                boardCameraController = FindFirstObjectByType<BoardCameraController>();
            }

            GameObject objectiveView = mapRouteDirector != null
                ? mapRouteDirector.CurrentObjectiveView
                : null;

            if (objectiveView == null || boardCameraController == null)
            {
                return;
            }

            boardCameraController.Locate(objectiveView.transform);
        }

        private void HandleEndTurnButtonClicked(ReadyMainView view)
        {
            if (!SystemBus.Messages.Request<DiceSnapshotRequested, DiceSnapshot>(default).HasRolledThisTurn)
            {
                ShowWarning("Please roll the dice first!");
                return;
            }

            if (boardSceneController != null)
            {
                boardSceneController.EndTurn();
            }
        }

        private void HandleSettingsBackClicked(OzGameLab01.UI.Settings.SettingsView view)
        {
            if (readySceneView != null) readySceneView.HideSettingsView();
        }

        private void HandleReturnToTitleClicked(OzGameLab01.UI.Settings.SettingsView view)
        {
            boardSceneController?.ReturnToTitle();
        }

        private void HandleUnitCloseClicked(UnitView view)
        {
            if (readySceneView != null) readySceneView.HideUnitView();
        }

        private void HandleRollButtonClicked(DiceRollView view)
        {
            SystemBus.Messages.Request<DiceRollRequested, DiceRollResult>(default);
        }

        private void HandleDiceRolled(int diceValue)
        {
            CancelAutomaticRollView();
            RefreshEndTurnFeedback();

            if (readySceneView == null)
                return;

            DiceRollView view = readySceneView.RollView;

            if (!isActiveAndEnabled || view == null || !view.IsVisible)
                return;

            _feedbackView.SetDiceResult("?");

            view.SetInteractable(false);

            bool started = view.PlayRoll(diceValue, result =>
            {
                if (!isActiveAndEnabled || view == null || !view.IsVisible)
                    return;

                _feedbackView.SetDiceResult(result.ToString());

                StartCoroutine(CloseRollViewRoutine());
            });

            if (!started)
            {
                _feedbackView.SetDiceResult(diceValue.ToString());

                StartCoroutine(CloseRollViewRoutine());
            }
        }

        // [수정됨] 턴이 종료되면 코루틴을 통해 1프레임 대기 후 UI를 업데이트합니다.
        private void HandleTurnEnded(int unusedActionPoints)
        {
            readySceneView?.MainView?.SetActionPointState(
                EndTurnButtonFeedbackView.TurnActionPointState.Waiting,
                0,
                true);
            StartCoroutine(UpdateTurnUIRoutine());
        }

        private void HandlePlayerFinishedMoving()
        {
            RefreshEndTurnFeedback();
        }

        private void HandlePlayerStepCompleted()
        {
            RefreshEndTurnFeedback(true);
        }

        private System.Collections.IEnumerator UpdateTurnUIRoutine()
        {
            // BoardSceneController 내부에서 TurnCount를 올릴 때까지 아주 잠깐(1프레임) 기다려줍니다.
            yield return null;

            if (readySceneView != null && readySceneView.MainView != null)
            {
                // 증가가 끝난 진짜 TurnCount 값에 +1을 더해서 "이번에 시작될 턴"을 표시합니다.
                int displayTurn = BoardTurnRules.DisplayTurn(BoardRunData.TurnCount);

                readySceneView.MainView.SetCurrentTurn(displayTurn);
            }
        }

        private void HandleNightReached(int turnCount)
        {
            PlayClockTransition(nightClockAngle);
            Debug.Log($"[BoardUIController] {turnCount}턴 째 밤이 되었습니다!");
            ShowTimeOfDayFeedback(nightMessage);
        }

        private void HandleNoonReached(int turnCount)
        {
            int displayTurn = BoardTurnRules.DisplayTurn(turnCount);
            Debug.Log($"[BoardUIController] {displayTurn}턴부터 정오입니다.");
            ShowTimeOfDayFeedback(noonMessage);
        }

        private void HandleDayReached(int turnCount)
        {
            PlayClockTransition(dayClockAngle);
            Debug.Log($"[BoardUIController] {turnCount}턴 째 낮이 되었습니다!");
            ShowTimeOfDayFeedback(dayMessage);
        }

        private void ApplyInitialClockRotation()
        {
            float angle = boardSceneController != null &&
                          boardSceneController.CurrentTimeOfDay == BoardTimeOfDay.Night
                ? nightClockAngle
                : dayClockAngle;

            SetClockRotationImmediate(angle);
        }

        private void PlayClockTransition(float finalTargetAngle)
        {
            RectTransform rotatingVisual = ResolveClockRotatingVisual();
            if (rotatingVisual == null)
            {
                return;
            }

            StopClockRotation();

            float currentZ = rotatingVisual.localEulerAngles.z;
            float targetZ = finalTargetAngle;

            // 항상 양수 방향(반시계)으로 쏘아지도록 목표 각도 보정
            if (targetZ <= currentZ)
            {
                targetZ += 360f;
            }

            float catchZ = currentZ + catchAngleOffset;
            float pullbackZ = currentZ + pullbackAngleOffset;

            _clockRotationSequence = DOTween.Sequence()
                .SetUpdate(true)
                // 1. 양수 방향으로 약간 덜컥
                .Append(rotatingVisual.DOLocalRotate(
                        new Vector3(0f, 0f, catchZ),
                        catchDuration,
                        RotateMode.FastBeyond360)
                    .SetEase(catchEase))
                // 2. 음수 방향으로 천천히 시위 당기기
                .Append(rotatingVisual.DOLocalRotate(
                        new Vector3(0f, 0f, pullbackZ),
                        pullbackDuration,
                        RotateMode.FastBeyond360)
                    .SetEase(pullbackEase))
                // 3. 목표를 향해 빠른 속도로 발사
                .Append(rotatingVisual.DOLocalRotate(
                        new Vector3(0f, 0f, targetZ),
                        shootDuration,
                        RotateMode.FastBeyond360)
                    .SetEase(shootEase))
                .OnComplete(() => _clockRotationSequence = null);
        }

        private void SetClockRotationImmediate(float angle)
        {
            RectTransform rotatingVisual = ResolveClockRotatingVisual();
            if (rotatingVisual == null)
            {
                return;
            }

            StopClockRotation();
            rotatingVisual.localEulerAngles = new Vector3(0f, 0f, angle);
        }

        private RectTransform ResolveClockRotatingVisual()
        {
            if (clockRotatingVisual != null)
            {
                return clockRotatingVisual;
            }

            if (readySceneView?.MainView == null)
            {
                return null;
            }

            RectTransform[] children =
                readySceneView.MainView.GetComponentsInChildren<RectTransform>(true);

            foreach (RectTransform child in children)
            {
                if (child.name == "RotatingVisual")
                {
                    clockRotatingVisual = child;
                    break;
                }
            }

            return clockRotatingVisual;
        }

        private void StopClockRotation()
        {
            _clockRotationSequence?.Kill(false);
            _clockRotationSequence = null;
        }

#if UNITY_EDITOR
        [ContextMenu("Clock Animation/Play Day Transition")]
        private void PreviewDayClockTransition()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("시계 전환 애니메이션은 플레이 모드에서만 재생할 수 있습니다.", this);
                return;
            }

            PlayClockTransition(dayClockAngle);
        }

        [ContextMenu("Clock Animation/Play Night Transition")]
        private void PreviewNightClockTransition()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("시계 전환 애니메이션은 플레이 모드에서만 재생할 수 있습니다.", this);
                return;
            }

            PlayClockTransition(nightClockAngle);
        }
#endif

        private void HandlePlayerTurnReady()
        {
            ScheduleAutomaticRollView(turnAutoRollViewDelay);
        }

        private void HandleUnitAcquired(UnitData unitData)
        {
            if (readySceneView != null)
            {
                readySceneView.PlayUnitAcquirePopup(unitData.name, null);
            }
        }

        private void ShowTimeOfDayFeedback(string message)
        {
            CancelAutomaticRollView();

            if (_timeOfDayFeedbackRoutine != null)
            {
                StopCoroutine(_timeOfDayFeedbackRoutine);
            }

            _timeOfDayFeedbackRoutine = StartCoroutine(ShowTimeOfDayFeedbackRoutine(message));
        }

        private System.Collections.IEnumerator ShowTimeOfDayFeedbackRoutine(string message)
        {
            if (readySceneView != null)
            {
                readySceneView.HideAllOverlayViews();
                readySceneView.ShowFeedbackView(message);
            }

            yield return new WaitForSecondsRealtime(timeOfDayFeedbackDuration);

            if (readySceneView != null)
            {
                readySceneView.HideFeedbackView();
            }

            _timeOfDayFeedbackRoutine = null;
            ScheduleAutomaticRollView(turnAutoRollViewDelay);
        }

        private void ScheduleAutomaticRollView(float delay)
        {
            CancelAutomaticRollView();

            if (!automaticRollViewEnabled)
            {
                return;
            }

            readySceneView?.MainView?.SetInteractable(false);

            _automaticRollViewRoutine =
                StartCoroutine(OpenRollViewWhenAvailableRoutine(delay, null));
        }

        private void CancelAutomaticRollView()
        {
            if (_automaticRollViewRoutine == null)
            {
                return;
            }

            StopCoroutine(_automaticRollViewRoutine);
            _automaticRollViewRoutine = null;

            readySceneView?.MainView?.SetInteractable(true);
        }

        private System.Collections.IEnumerator OpenRollViewWhenAvailableRoutine(
            float delay,
            Action onOpened)
        {
            if (delay > 0f)
            {
                yield return new WaitForSecondsRealtime(delay);
            }

            RollViewOpenResult result = RollViewOpenResult.Retry;

            while (isActiveAndEnabled)
            {
                result = TryOpenRollViewInternal();

                if (result != RollViewOpenResult.Retry)
                {
                    break;
                }

                yield return null;
            }

            _automaticRollViewRoutine = null;

            readySceneView?.MainView?.SetInteractable(true);

            if (result == RollViewOpenResult.Opened)
            {
                onOpened?.Invoke();
            }
        }

        private bool TryOpenRollView()
        {
            return TryOpenRollViewInternal() == RollViewOpenResult.Opened;
        }

        private RollViewOpenResult TryOpenRollViewInternal()
        {
            if (_timeOfDayFeedbackRoutine != null)
            {
                return RollViewOpenResult.Retry;
            }

            if (readySceneView == null || readySceneView.RollView == null)
            {
                return RollViewOpenResult.Blocked;
            }

            if (readySceneView.RollView.IsVisible)
            {
                return RollViewOpenResult.Opened;
            }

            DiceSnapshot diceSnapshot =
                SystemBus.Messages.Request<DiceSnapshotRequested, DiceSnapshot>(default);
            BoardDiceSnapshot boardSnapshot =
                SystemBus.Messages.Request<BoardDiceStateRequested, BoardDiceSnapshot>(default);

            if (!boardSnapshot.Available || boardSnapshot.IsMoving)
            {
                return RollViewOpenResult.Retry;
            }

            if (diceSnapshot.HasRolledThisTurn ||
                boardSnapshot.RemainingValue > 0)
            {
                return RollViewOpenResult.Blocked;
            }

            readySceneView.HideAllOverlayViews();
            _feedbackView.SetDiceResult("?");
            readySceneView.RollView.SetInteractable(true);
            readySceneView.ShowRollView();
            return RollViewOpenResult.Opened;
        }

        private enum RollViewOpenResult
        {
            Opened,
            Retry,
            Blocked
        }

        private void ShowWarning(string message)
        {
            if (warningText != null)
            {
                _feedbackView.ShowWarning(message);

                StopCoroutine("HideWarningRoutine");
                StartCoroutine("HideWarningRoutine");
            }
        }

        private void RefreshEndTurnFeedback(bool immediate = false)
        {
            if (readySceneView?.MainView == null)
            {
                return;
            }

            bool hasRolled = BoardRunData.HasRolledThisTurn;
            int rolledPoints = BoardRunData.RolledDiceValue;
            int remainingPoints = BoardRunData.RemainingDiceValue;

            EndTurnButtonFeedbackView.TurnActionPointState state;

            if (!hasRolled)
            {
                state = EndTurnButtonFeedbackView.TurnActionPointState.Waiting;
                remainingPoints = 0;
            }
            else if (remainingPoints <= 0)
            {
                state = EndTurnButtonFeedbackView.TurnActionPointState.Depleted;
            }
            else if (remainingPoints == rolledPoints)
            {
                state = EndTurnButtonFeedbackView.TurnActionPointState.Unused;
            }
            else
            {
                state = EndTurnButtonFeedbackView.TurnActionPointState.PartiallyUsed;
            }

            readySceneView.MainView.SetActionPointState(
                state,
                remainingPoints,
                immediate);
        }

        private System.Collections.IEnumerator HideWarningRoutine()
        {
            yield return new WaitForSeconds(warningTextDuration);
            _feedbackView.HideWarning();
        }

        private System.Collections.IEnumerator CloseRollViewRoutine()
        {
            yield return new WaitForSeconds(rollViewCloseDelay);
            if (readySceneView != null) readySceneView.HideRollView();
            OnRollViewClosed?.Invoke();
        }
    }
}
