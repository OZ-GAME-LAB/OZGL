using UnityEngine;
using TMPro;
using OzGameLab01.UI;
using OzGameLab01.Data;

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

        [Header("Settings")]
        public float rollViewCloseDelay = 1.0f;
        public float warningTextDuration = 2.0f;

        private void Start()
        {
            if (Managers.DiceManager.Instance != null)
                Managers.DiceManager.Instance.OnDiceRolled += HandleDiceRolled;

            if (readySceneView != null)
            {
                if (readySceneView.RollView != null)
                    readySceneView.RollView.RollClicked += HandleRollButtonClicked;

                if (readySceneView.MainView != null)
                {
                    readySceneView.MainView.UnitClicked += HandleUnitButtonClicked;
                    readySceneView.MainView.SettingsClicked += HandleSettingsButtonClicked;
                    readySceneView.MainView.EndTurnClicked += HandleEndTurnButtonClicked;

                    // [수정됨] 시작할 때 "현재 플레이 중인 턴(경과 턴 + 1)"을 표시합니다.
                    int initialTurn = BoardRunData.TurnCount + 1;
                    readySceneView.MainView.SetCurrentTurn(initialTurn);
                    readySceneView.MainView.SetClockHandAngle(initialTurn * -30f);
                }

                if (readySceneView.SettingsView != null)
                {
                    readySceneView.SettingsView.BackClicked += HandleSettingsBackClicked;
                    readySceneView.SettingsView.ReturnToMainClicked += HandleSettingsBackClicked;
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
            }
        }

        private void OnDestroy()
        {
            if (Managers.DiceManager.Instance != null)
                Managers.DiceManager.Instance.OnDiceRolled -= HandleDiceRolled;

            if (readySceneView != null)
            {
                if (readySceneView.RollView != null)
                    readySceneView.RollView.RollClicked -= HandleRollButtonClicked;

                if (readySceneView.MainView != null)
                {
                    readySceneView.MainView.UnitClicked -= HandleUnitButtonClicked;
                    readySceneView.MainView.SettingsClicked -= HandleSettingsButtonClicked;
                    readySceneView.MainView.EndTurnClicked -= HandleEndTurnButtonClicked;
                }

                if (readySceneView.SettingsView != null)
                {
                    readySceneView.SettingsView.BackClicked -= HandleSettingsBackClicked;
                    readySceneView.SettingsView.ReturnToMainClicked -= HandleSettingsBackClicked;
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
            }
        }

        public void ToggleRollView()
        {
            if (readySceneView == null || readySceneView.RollView == null) return;

            bool isActive = !readySceneView.RollView.IsVisible;
            if (isActive)
            {
                if (Managers.DiceManager.Instance != null && Managers.DiceManager.Instance.HasRolledThisTurn)
                {
                    ShowWarning("턴 종료를 먼저 해주세요!");
                    return;
                }

                readySceneView.HideAllOverlayViews();
                if (resultText != null) resultText.text = "?";
                readySceneView.ShowRollView();
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

        private void HandleEndTurnButtonClicked(ReadyMainView view)
        {
            if (Managers.DiceManager.Instance != null && !Managers.DiceManager.Instance.HasRolledThisTurn)
            {
                ShowWarning("주사위를 먼저 굴려주세요!");
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

        private void HandleUnitCloseClicked(UnitView view)
        {
            if (readySceneView != null) readySceneView.HideUnitView();
        }

        private void HandleRollButtonClicked(RollView view)
        {
            if (Managers.DiceManager.Instance != null)
                Managers.DiceManager.Instance.RollDice();
        }

        private void HandleDiceRolled(int diceValue)
        {
            if (readySceneView != null && readySceneView.RollView != null && readySceneView.RollView.IsVisible)
            {
                if (resultText != null) resultText.text = diceValue.ToString();
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
                int displayTurn = BoardRunData.TurnCount + 1;

                readySceneView.MainView.SetCurrentTurn(displayTurn);

                float angle = displayTurn * -30f;
                readySceneView.MainView.SetClockHandAngle(angle);
            }
        }

        private void HandleNightReached(int turnCount)
        {
            Debug.Log($"[BoardUIController] {turnCount}턴 째 밤이 되었습니다!");
        }

        private void ShowWarning(string message)
        {
            if (warningText != null)
            {
                warningText.text = message;
                warningText.color = Color.red;
                warningText.gameObject.SetActive(true);

                StopCoroutine("HideWarningRoutine");
                StartCoroutine("HideWarningRoutine");
            }
        }

        private System.Collections.IEnumerator HideWarningRoutine()
        {
            yield return new WaitForSeconds(warningTextDuration);
            if (warningText != null) warningText.gameObject.SetActive(false);
        }

        private System.Collections.IEnumerator CloseRollViewRoutine()
        {
            yield return new WaitForSeconds(rollViewCloseDelay);
            if (readySceneView != null) readySceneView.HideRollView();
        }
    }
}