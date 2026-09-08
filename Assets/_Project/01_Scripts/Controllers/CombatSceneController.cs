using System;
using UnityEngine;
using OzGameLab01.Combat;
using OzGameLab01.Data;
using OzGameLab01.Managers;

namespace OzGameLab01.Controllers
{
    /// <summary>
    /// 전투 씬(03_Combat)의 핵심 로직을 담당합니다.
    /// UI 관련 처리는 BattleUIController로 위임하고, 승패 판정만 처리합니다.
    /// </summary>
    public sealed class CombatSceneController : MonoBehaviour
    {
        public enum BattleState
        {
            Running,
            Paused,
            Resolved
        }

        // 전투가 끝났을 때 UI 컨트롤러 쪽에 알려줄 이벤트
        public event Action<bool> OnBattleResolved;

        private bool _resolved;
        private bool _wasBossBattle;
        private bool _victory;
        private bool _fastForward;
        private bool _isReturningToTitle; // [추가] 런 종료 저장 중 중복 타이틀 이동 요청 방지

        public BattleState CurrentState { get; private set; } = BattleState.Running;
        public bool IsPaused => CurrentState == BattleState.Paused;
        public float CurrentTimeScale => _fastForward ? 2f : 1f;

        public bool IsResolved => _resolved;
        public bool IsBossVictory => _wasBossBattle && _victory;

        public void SetFastForward(bool enabled)
        {
            _fastForward = enabled;
            if (!_resolved && !IsPaused)
            {
                ApplyTimeScale();
            }
        }

        public void SetPaused(bool paused)
        {
            if (_resolved)
            {
                return;
            }

            CurrentState = paused ? BattleState.Paused : BattleState.Running;
            ApplyTimeScale();
        }

        private void ApplyTimeScale()
        {
            Time.timeScale = IsPaused || _resolved ? 0f : CurrentTimeScale;
        }

        private void Update()
        {
            if (_resolved)
            {
                return;
            }

            bool allyAlive = false;
            bool enemyAlive = false;

            foreach (Unit unit in BattleUnitRegistry.Units)
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
        /// 승패를 판정하고 전투를 종료시킵니다.
        /// </summary>
        private void ResolveBattle(bool victory)
        {
            if (_resolved) return;

            _resolved = true;
            _wasBossBattle = BoardRunData.HasCurrentBattle && BoardRunData.IsBossBattle;
            _victory = victory;
            CurrentState = BattleState.Resolved;
            ApplyTimeScale();

            if (victory)
            {
                BoardRunData.CompleteCurrentBattle();
            }

            // UI 컨트롤러에게 결과창을 띄우라고 신호를 보냅니다.
            OnBattleResolved?.Invoke(victory);
        }

        /// <summary>
        /// 항복했거나 결과창에서 확인을 누르면 보드 씬으로 이동합니다.
        /// </summary>
        public void ReturnToBoard()
        {
            SceneTransitioner transitioner = SceneTransitioner.Instance;

            if (transitioner == null)
            {
                Debug.LogError("[CombatSceneController] SceneTransitioner를 찾을 수 없습니다.", this);
                return;
            }

            if (transitioner.IsTransitioning)
            {
                Debug.LogWarning("[CombatSceneController] 이미 씬 전환 중입니다.", this);
                return;
            }

            ResetTimeScale();
            transitioner.LoadBoardScene();
        }

        // <summary>
        // 보스전 승리 후 현재 게임 진행을 종료하고 타이틀 씬으로 이동합니다.
        // </summary>
        //public void ReturnToTitle()
        /// <summary>
        /// [수정] 종료된 런의 Continue 데이터를 제거한 뒤 타이틀로 이동
        /// </summary>
        public async void ReturnToTitle()
        {
            if (_isReturningToTitle)
            {
                return;
            }

            SceneTransitioner transitioner = SceneTransitioner.Instance;

            if (transitioner == null)
            {
                Debug.LogError("[CombatSceneController] SceneTransitioner를 찾을 수 없습니다.", this);
                return;
            }

            if (transitioner.IsTransitioning)
            {
                Debug.LogWarning("[CombatSceneController] 이미 씬 전환 중입니다.", this);
                return;
            }

            _isReturningToTitle = true;

            ResetTimeScale();

            // [수정] 런타임 상태와 저장 파일의 Continue 데이터를 함께 초기화 (BoardRunData.Clear()를 포함)
            SaveManager saveManager = SaveManager.Instance;
            saveManager.ClearCurrentRun();
            bool saved = await saveManager.SaveAsync();
            if (!saved)
            {
                Debug.LogError("[CombatSceneController] 종료된 런 데이터 정리에 실패했습니다.", this);
            }

            transitioner.LoadTitleScene();
            _isReturningToTitle = false;
        }

        private void ResetTimeScale()
        {
            _fastForward = false;
            CurrentState = BattleState.Running;
            Time.timeScale = 1f;
        }
    }
}
