using System;
using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using OzGameLab01.UI;
using OzGameLab01.UI.Battle;
using OzGameLab01.UI.Settings;

namespace OzGameLab01.Controllers
{
    public class BattleUIController : MonoBehaviour
    {
        [Header("Views (UI 연결)")]
        public BattleUIView battleUIView;
        public SettingsView settingsView;
        public ConfirmPopupView surrenderPopup;

        [Header("Scene Controller (로직 연결)")]
        public CombatSceneController combatSceneController;

        // 인스펙터 연결 여부와 무관하게 직접 물고 있을 숨겨진 뷰들
        private BattleControlView _controlView;
        private BattleTimerView _timerView;
        
        private float _battleTimer = 0f;
        private bool _isFastForward = false; // 배속 상태 저장용 변수

        private void Awake()
        {
            // 이벤트 시스템 체크 (버튼 클릭 불가 원인 1순위)
            if (EventSystem.current == null)
            {
                Debug.LogWarning("[BattleUIController] 씬에 EventSystem이 없습니다! UI 버튼이 작동하지 않습니다. EventSystem을 추가해주세요.");
            }

            // 1. 최상단 UI 및 컨트롤러들을 찾습니다.
            if (battleUIView == null) battleUIView = FindFirstObjectByType<BattleUIView>(FindObjectsInactive.Include);
            if (settingsView == null) settingsView = FindFirstObjectByType<SettingsView>(FindObjectsInactive.Include);
            if (surrenderPopup == null) surrenderPopup = FindFirstObjectByType<ConfirmPopupView>(FindObjectsInactive.Include);
            if (combatSceneController == null) combatSceneController = FindFirstObjectByType<CombatSceneController>(FindObjectsInactive.Include);

            // 2. BattleUIView 내부 깊숙이 있는 컨트롤 뷰와 타이머 뷰를 직접 찾아냅니다!
            if (battleUIView != null)
            {
                _controlView = battleUIView.GetComponentInChildren<BattleControlView>(true);
                _timerView = battleUIView.GetComponentInChildren<BattleTimerView>(true);
            }
        }

        private void OnEnable()
        {
            if (_controlView != null)
            {
                _controlView.SettingsClicked += HandleSettingsClicked;
                _controlView.SpeedClicked += HandleSpeedClicked; // 배속 버튼 구독
            }

            if (battleUIView != null)
            {
                battleUIView.EndBattleClicked += HandleEndBattleClicked;
                battleUIView.RewardSelected += HandleRewardSelected;
            }

            if (settingsView != null)
            {
                settingsView.BackClicked += HandleSettingsBackClicked;
                settingsView.ReturnToMainClicked += HandleReturnToMainClicked;
            }

            if (surrenderPopup != null)
            {
                surrenderPopup.ConfirmClicked += HandleSurrenderConfirmClicked;
                surrenderPopup.CancelClicked += HandleSurrenderCancelClicked;
            }

            if (combatSceneController != null)
            {
                combatSceneController.OnBattleResolved += HandleBattleResolved;
            }
        }

        private void OnDisable()
        {
            if (_controlView != null)
            {
                _controlView.SettingsClicked -= HandleSettingsClicked;
                _controlView.SpeedClicked -= HandleSpeedClicked;
            }

            if (battleUIView != null)
            {
                battleUIView.EndBattleClicked -= HandleEndBattleClicked;
                battleUIView.RewardSelected -= HandleRewardSelected;
            }

            if (settingsView != null)
            {
                settingsView.BackClicked -= HandleSettingsBackClicked;
                settingsView.ReturnToMainClicked -= HandleReturnToMainClicked;
            }

            if (surrenderPopup != null)
            {
                surrenderPopup.ConfirmClicked -= HandleSurrenderConfirmClicked;
                surrenderPopup.CancelClicked -= HandleSurrenderCancelClicked;
            }

            if (combatSceneController != null)
            {
                combatSceneController.OnBattleResolved -= HandleBattleResolved;
            }
        }

        private void Update()
        {
            // 3. 누락되어 있던 전투 타이머 동기화 로직 추가!
            if (combatSceneController != null && !combatSceneController.IsResolved)
            {
                // UnscaledDeltaTime을 쓰면 배속에 무관하게 흐르는 현실 시간을 잴 수 있고,
                // DeltaTime을 쓰면 배속 시 게임 시간도 빨리 흐릅니다. (보통 전투 타이머는 배속 시 빨리 흐릅니다)
                _battleTimer += Time.deltaTime;

                if (_timerView != null)
                {
                    TimeSpan time = TimeSpan.FromSeconds(_battleTimer);
                    // mm:ss (예: 01:23) 형식으로 텍스트 업데이트
                    _timerView.SetTimeText(time.ToString(@"mm\:ss"));
                }
            }
        }

        #region 버튼 클릭 이벤트 처리

        private void HandleSpeedClicked(BattleControlView view)
        {
            _isFastForward = !_isFastForward; // 상태 토글 (1배속 <-> 2배속)

            // 설정창 등이 열려있어서 일시정지 상태인 경우가 아니라면 즉시 배속을 적용합니다.
            if (Time.timeScale > 0f)
            {
                Time.timeScale = _isFastForward ? 2f : 1f;
            }

            // 버튼의 텍스트가 있다면 업데이트 해줍니다. (버튼 내부에 TMP_Text가 있다고 가정)
            if (view != null && view.SpeedButton != null)
            {
                TMP_Text buttonText = view.SpeedButton.GetComponentInChildren<TMP_Text>();
                if (buttonText != null)
                {
                    buttonText.text = _isFastForward ? "x2" : "x1";
                }
            }
        }

        private void HandleSettingsClicked(BattleControlView view)
        {
            Time.timeScale = 0f; // 전투 일시정지
            settingsView?.Show();
        }

        private void HandleSettingsBackClicked(SettingsView view)
        {
            settingsView?.Hide();
            if (combatSceneController != null && !combatSceneController.IsResolved)
            {
                // 일시정지를 풀 때, 플레이어가 설정해둔 배속 상태(1배속 또는 2배속)로 정확히 복원합니다.
                Time.timeScale = _isFastForward ? 2f : 1f;
            }
        }

        private void HandleReturnToMainClicked(SettingsView view)
        {
            surrenderPopup?.Show("Would you like to surrender and return to the board?");
        }

        private void HandleSurrenderConfirmClicked(ConfirmPopupView popup)
        {
            surrenderPopup?.Hide();
            settingsView?.Hide();

            if (combatSceneController != null)
            {
                combatSceneController.ReturnToBoard();
            }
        }

        private void HandleSurrenderCancelClicked(ConfirmPopupView popup)
        {
            surrenderPopup?.Hide();
        }

        private void HandleEndBattleClicked(BattleResultView view)
        {
            if (combatSceneController != null)
            {
                if (combatSceneController.IsBossVictory)
                {
                    combatSceneController.ReturnToTitle();
                }
                else
                {
                    combatSceneController.ReturnToBoard();
                }
            }
        }

        #endregion

        #region 게임 로직 이벤트 처리

        private void HandleBattleResolved(bool victory)
        {
            if (battleUIView != null)
            {
                if (victory)
                {
                    if (combatSceneController != null && combatSceneController.IsBossVictory)
                    {
                        if (battleUIView.ResultView != null)
                        {
                            battleUIView.ResultView.SetResultText("Victory");
                            battleUIView.ResultView.SetOptionalMessage(string.Empty);
                            battleUIView.ResultView.SetEndBattleButtonText("Return To Title");
                        }

                        battleUIView.ShowResultView();
                        return;
                    }

                    // 승리 시 보상 창(RewardView)을 먼저 띄웁니다.
                    if (battleUIView.RewardView != null)
                    {
                        battleUIView.RewardView.ClearRewardOptions();

                        // 기획 데이터 연결 전까지 임시 보상 3개 생성
                        // 보상 데이터가 준비되기 전까지 사용하던 임시 보상 생성은 제거합니다.
                        battleUIView.RewardView.Hide();
                    }

                    if (battleUIView.ResultView != null)
                    {
                        battleUIView.ResultView.SetResultText("Victory!");
                        battleUIView.ResultView.SetOptionalMessage("Reward data is not configured yet.");
                        battleUIView.ResultView.SetEndBattleButtonText("Return To Board");
                    }

                    battleUIView.ShowResultView();
                }
                else
                {
                    // 패배 시 바로 결과 창(ResultView)을 띄웁니다.
                    if (battleUIView.ResultView != null)
                    {
                        battleUIView.ResultView.SetResultText("Defeat...");
                        battleUIView.ResultView.SetOptionalMessage("Better luck next time...");
                        battleUIView.ResultView.SetEndBattleButtonText("Return To Board");
                    }
                    battleUIView.ShowResultView();
                }
            }
        }

        private void HandleRewardSelected(RewardOptionItemView option)
        {
            // 보상을 선택하면 보상창이 닫히고 결과창으로 넘어갑니다.
            if (battleUIView != null)
            {
                if (battleUIView.ResultView != null)
                {
                    battleUIView.ResultView.SetResultText("Victory!");
                    battleUIView.ResultView.SetEndBattleButtonText("Return To Board");
                    
                    // 선택한 보상의 설명을 결과창에 표기
                    string selectedDesc = option.DescriptionText != null ? option.DescriptionText.text : "Reward";
                    battleUIView.ResultView.SetOptionalMessage($"Obtained: {selectedDesc}");
                }
                
                battleUIView.ShowResultView();
            }
        }

        #endregion
    }
}
