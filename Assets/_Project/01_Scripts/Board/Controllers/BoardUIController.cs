using UnityEngine;
using OzGameLab01.Dice.Contracts;
using OzGameLab01.Board.Models;
using OzGameLab01.Board.Views;
using TMPro;
using OzGameLab01.UI;
using OzGameLab01.Data;
using OzGameLab01.Map;

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
        [SerializeField] private MapGenerator mapGenerator;
        [Min(0f)] [SerializeField] private float autoRollViewOpenDelay = 0.5f;
        [Min(0f)] [SerializeField] private float timeOfDayFeedbackDuration = 1.5f;
        [SerializeField] private string nightMessage = "Night Has Come";
        [SerializeField] private string dayMessage = "Day Has Come";

        [Header("Settings")]
        public float rollViewCloseDelay = 1.0f;
        public float warningTextDuration = 2.0f;

        private Coroutine _automaticRollViewRoutine;
        private Coroutine _timeOfDayFeedbackRoutine;
        private bool _isMapPresentationReady;
        private BoardFeedbackView _feedbackView;
        private System.IDisposable _diceSubscription;
        private bool _started;

        private void Awake()
        {
            _feedbackView = new BoardFeedbackView(resultText, warningText);
        }

        private void Start()
        {
            _started = true;
            BindEvents();
        }

        private void OnEnable()
        {
            if (_started) { BindEvents(); }
        }

        private void BindEvents()
        {
            if (mapRouteDirector == null) mapRouteDirector = FindFirstObjectByType<MapRouteDirector>();
            if (boardCameraController == null) boardCameraController = FindFirstObjectByType<BoardCameraController>();
            if (mapGenerator == null) mapGenerator = FindFirstObjectByType<MapGenerator>();

            if (mapGenerator != null)
            {
                mapGenerator.PresentationCompleted += HandleMapPresentationCompleted;
            }

            _ = Managers.DiceManager.Instance.Facade; // 씬 직접 실행 시 시스템 구성
            _diceSubscription?.Dispose();
            _diceSubscription = SystemBus.Messages.Subscribe<DiceRolled>(message => HandleDiceRolled(message.Value));

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
                    readySceneView.MainView.SetClockHandAngle(BoardTurnRules.ClockAngle(BoardRunData.TurnCount));
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
                boardSceneController.DayReached += HandleDayReached;
                boardSceneController.PlayerTurnReady += HandlePlayerTurnReady;
            }

            // 즉시 생성 경로는 MapGenerator.Start에서 먼저 완료될 수 있습니다.
            if (mapGenerator != null && mapGenerator.IsPresentationComplete)
            {
                HandleMapPresentationCompleted();
            }
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            _automaticRollViewRoutine = null;
            _timeOfDayFeedbackRoutine = null;
            _isMapPresentationReady = false;
            _feedbackView?.HideWarning();
            if (mapGenerator != null)
            {
                mapGenerator.PresentationCompleted -= HandleMapPresentationCompleted;
            }

            _diceSubscription?.Dispose();
            _diceSubscription = null;

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
                boardSceneController.DayReached -= HandleDayReached;
                boardSceneController.PlayerTurnReady -= HandlePlayerTurnReady;
            }
        }

        public void ToggleRollView()
        {
            if (readySceneView == null || readySceneView.RollView == null) return;

            bool isActive = !readySceneView.RollView.IsVisible;
            if (isActive)
            {
                if (!_isMapPresentationReady || _timeOfDayFeedbackRoutine != null)
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

        private void HandleRollButtonClicked(RollView view)
        {
            SystemBus.Messages.Request<DiceRollRequested, DiceRollResult>(default);
        }

        private void HandleDiceRolled(int diceValue)
        {
            CancelAutomaticRollView();

            if (readySceneView == null)
                return;

            RollView view = readySceneView.RollView;

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
            StartCoroutine(UpdateTurnUIRoutine());
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

                float angle = BoardTurnRules.ClockAngle(BoardRunData.TurnCount);
                readySceneView.MainView.SetClockHandAngle(angle);
            }
        }

        private void HandleNightReached(int turnCount)
        {
            Debug.Log($"[BoardUIController] {turnCount}턴 째 밤이 되었습니다!");
            ShowTimeOfDayFeedback(nightMessage);
        }

        private void HandleDayReached(int turnCount)
        {
            Debug.Log($"[BoardUIController] {turnCount}턴 째 낮이 되었습니다!");
            ShowTimeOfDayFeedback(dayMessage);
        }

        private void HandlePlayerTurnReady()
        {
            ScheduleAutomaticRollView();
        }

        private void HandleMapPresentationCompleted()
        {
            _isMapPresentationReady = true;
            ScheduleAutomaticRollView();
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
            ScheduleAutomaticRollView();
        }

        private void ScheduleAutomaticRollView()
        {
            if (!_isMapPresentationReady)
            {
                return;
            }

            CancelAutomaticRollView();
            _automaticRollViewRoutine = StartCoroutine(OpenRollViewAfterDelayRoutine());
        }

        private void CancelAutomaticRollView()
        {
            if (_automaticRollViewRoutine == null)
            {
                return;
            }

            StopCoroutine(_automaticRollViewRoutine);
            _automaticRollViewRoutine = null;
        }

        private System.Collections.IEnumerator OpenRollViewAfterDelayRoutine()
        {
            yield return new WaitForSecondsRealtime(autoRollViewOpenDelay);
            _automaticRollViewRoutine = null;
            TryOpenRollView();
        }

        private bool TryOpenRollView()
        {
            if (!_isMapPresentationReady || _timeOfDayFeedbackRoutine != null ||
                readySceneView == null || readySceneView.RollView == null)
            {
                return false;
            }

            if (readySceneView.RollView.IsVisible)
            {
                return true;
            }

            var diceSnapshot = SystemBus.Messages.Request<DiceSnapshotRequested, DiceSnapshot>(default);
            BoardPlayerController player = BoardPlayerController.Instance;
            if (!BoardTurnRules.CanOpenRoll(true, diceSnapshot.HasRolledThisTurn, player != null && player.IsMoving, player != null ? player.CurrentDiceValue : 0))
            {
                return false;
            }

            readySceneView.HideAllOverlayViews();
            _feedbackView.SetDiceResult("?");
            readySceneView.RollView.SetInteractable(true);
            readySceneView.ShowRollView();
            return true;
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

        private System.Collections.IEnumerator HideWarningRoutine()
        {
            yield return new WaitForSeconds(warningTextDuration);
            _feedbackView.HideWarning();
        }

        private System.Collections.IEnumerator CloseRollViewRoutine()
        {
            yield return new WaitForSeconds(rollViewCloseDelay);
            if (readySceneView != null) readySceneView.HideRollView();
        }
    }
}
