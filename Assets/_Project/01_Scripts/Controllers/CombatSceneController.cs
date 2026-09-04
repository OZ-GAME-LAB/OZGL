using OzGameLab01.Combat;
using OzGameLab01.Data;
using OzGameLab01.Managers;
using OzGameLab01.UI;
using OzGameLab01.UI.Battle;
using OzGameLab01.UI.Settings;
using UnityEngine;

namespace OzGameLab01.Controllers
{
    /// <summary>
    /// 전투 씬(03_Combat)의 공통 흐름을 관리합니다.
    ///
    /// 승패를 판정해 결과 화면을 띄우고, 설정창을 통한 일시정지와
    /// 항복(보드 씬 복귀) 흐름을 처리합니다.
    ///
    /// 유닛 스폰과 시너지 적용은 CombatManager가 담당하며, 이 컨트롤러는
    /// 전투가 끝난 뒤의 화면 전환만 책임집니다.
    /// </summary>
    public sealed class CombatSceneController : MonoBehaviour
    {
        [Header("전투 화면 연결")]
        [SerializeField] private BattleMainView _battleMainView;
        [SerializeField] private BattleResultView _resultView;
        [SerializeField] private SettingsView _settingsView;
        [Tooltip("설정창의 \"메인으로 돌아가기\"를 누르면 뜨는 항복 확인 팝업입니다.")]
        [SerializeField] private ConfirmPopupView _surrenderPopup;

        private bool _resolved;

        private void Awake()
        {
            if (_battleMainView == null)
            {
                _battleMainView = FindFirstObjectByType<BattleMainView>(FindObjectsInactive.Include);
            }

            if (_resultView == null)
            {
                _resultView = FindFirstObjectByType<BattleResultView>(FindObjectsInactive.Include);
            }

            if (_settingsView == null)
            {
                _settingsView = FindFirstObjectByType<SettingsView>(FindObjectsInactive.Include);
            }

            if (_battleMainView == null || _battleMainView.ControlView == null ||
                _resultView == null || _settingsView == null || _surrenderPopup == null)
            {
                Debug.LogError(
                    "[CombatSceneController] 필요한 화면 참조가 연결되지 않았습니다. 인스펙터에서 BattleMainView / BattleResultView / SettingsView / SurrenderPopup을 확인해주세요.",
                    this);
            }
        }

        private void OnEnable()
        {
            if (_battleMainView != null && _battleMainView.ControlView != null)
            {
                _battleMainView.ControlView.SettingsClicked += HandleSettingsClicked;
            }

            if (_resultView != null)
            {
                _resultView.EndBattleClicked += HandleEndBattleClicked;
            }

            if (_settingsView != null)
            {
                _settingsView.BackClicked += HandleSettingsBackClicked;
                _settingsView.ReturnToMainClicked += HandleReturnToMainClicked;
            }

            if (_surrenderPopup != null)
            {
                _surrenderPopup.ConfirmClicked += HandleSurrenderConfirmClicked;
                _surrenderPopup.CancelClicked += HandleSurrenderCancelClicked;
            }
        }

        private void OnDisable()
        {
            if (_battleMainView != null && _battleMainView.ControlView != null)
            {
                _battleMainView.ControlView.SettingsClicked -= HandleSettingsClicked;
            }

            if (_resultView != null)
            {
                _resultView.EndBattleClicked -= HandleEndBattleClicked;
            }

            if (_settingsView != null)
            {
                _settingsView.BackClicked -= HandleSettingsBackClicked;
                _settingsView.ReturnToMainClicked -= HandleReturnToMainClicked;
            }

            if (_surrenderPopup != null)
            {
                _surrenderPopup.ConfirmClicked -= HandleSurrenderConfirmClicked;
                _surrenderPopup.CancelClicked -= HandleSurrenderCancelClicked;
            }
        }

        private void Update()
        {
            if (_resolved)
            {
                return;
            }

            bool allyAlive = false;
            bool enemyAlive = false;

            foreach (Unit unit in Unit.All)
            {
                if (unit == null || unit.IsDead)
                {
                    continue;
                }

                if (unit.TeamValue == Unit.Team.Ally)
                {
                    allyAlive = true;
                }
                else if (unit.TeamValue == Unit.Team.Enemy)
                {
                    enemyAlive = true;
                }
            }

            if (!enemyAlive)
            {
                ResolveBattle(true);
            }
            else if (!allyAlive)
            {
                ResolveBattle(false);
            }
        }

        /// <summary>
        /// 승패를 확정하고 결과 화면을 띄웁니다.
        /// 승리했을 때만 현재 전투 타일을 완료 처리합니다(항복 시에는 완료 처리하지 않아 재도전 가능).
        /// </summary>
        private void ResolveBattle(bool victory)
        {
            _resolved = true;
            Time.timeScale = 0f;

            if (victory)
            {
                BoardRunData.CompleteCurrentBattle();
            }

            if (_resultView != null)
            {
                _resultView.SetResultText(victory ? "승리!" : "패배...");
                _resultView.SetOptionalMessage(string.Empty);
                _resultView.Show();
            }
        }

        private void HandleEndBattleClicked(BattleResultView view)
        {
            ReturnToBoard();
        }

        private void HandleSettingsClicked(BattleControlView view)
        {
            if (_resolved)
            {
                return;
            }

            Time.timeScale = 0f;
            _settingsView?.Show();
        }

        private void HandleSettingsBackClicked(SettingsView view)
        {
            _settingsView?.Hide();

            if (!_resolved)
            {
                Time.timeScale = 1f;
            }
        }

        private void HandleReturnToMainClicked(SettingsView view)
        {
            _surrenderPopup?.Show("전투를 포기하고 보드로 돌아가시겠습니까?");
        }

        /// <summary>
        /// 항복을 확정합니다. 진행 중인 런과 다른 완료된 전투 타일은 그대로 유지하고,
        /// 이번 전투 타일만 미완료 상태로 남겨 재도전할 수 있게 합니다.
        /// </summary>
        private void HandleSurrenderConfirmClicked(ConfirmPopupView popup)
        {
            _surrenderPopup?.Hide();
            _settingsView?.Hide();
            ReturnToBoard();
        }

        private void HandleSurrenderCancelClicked(ConfirmPopupView popup)
        {
            _surrenderPopup?.Hide();
        }

        private void ReturnToBoard()
        {
            SceneTransitioner transitioner = SceneTransitioner.Instance;

            if (transitioner == null)
            {
                Debug.LogError(
                    "[CombatSceneController] SceneTransitioner가 없어 보드 씬으로 이동할 수 없습니다. 00_Boot 씬부터 실행했는지 확인해주세요.", this);

                return;
            }

            if (transitioner.IsTransitioning)
            {
                Debug.LogWarning("[CombatSceneController] 이미 씬 전환이 진행 중입니다.", this);

                return;
            }

            Time.timeScale = 1f;

            transitioner.LoadBoardScene();
        }
    }
}
