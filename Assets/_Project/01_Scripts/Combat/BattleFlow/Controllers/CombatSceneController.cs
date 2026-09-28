using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using OzGameLab01.Combat;
using OzGameLab01.Data;
using OzGameLab01.Managers;
using OzGameLab01.Save;
using OzGameLab01.Common;

namespace OzGameLab01.Controllers
{
    /// <summary>
    /// 전투 씬(03_Combat)의 핵심 로직을 담당합니다.
    /// UI 관련 처리는 BattleUIController로 위임하고, 승패 판정만 처리합니다.
    /// </summary>
    public sealed class CombatSceneController : MonoBehaviour
    {
        [Flags]
        public enum PauseReason
        {
            None = 0,
            UserInterface = 1 << 0,
            Tutorial = 1 << 1,
            BattleInfo = 1 << 2
        }

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
        private bool _outcomeDirty;
        private readonly HashSet<Unit> _pendingDeadUnits = new HashSet<Unit>();
        private bool _isReturningToTitle; // [추가] 런 종료 저장 중 중복 타이틀 이동 요청 방지
        private PauseReason _pauseReasons;
        private CombatSession _combatSession;

        public BattleState CurrentState { get; private set; } = BattleState.Running;
        public bool IsPaused => _pauseReasons != PauseReason.None;
        public PauseReason ActivePauseReasons => _pauseReasons;
        public float CurrentTimeScale => _fastForward ? 2f : 1f;

        public bool IsResolved => _resolved;
        public bool IsBossVictory => _wasBossBattle && _victory;

        private void Awake()
        {
            _combatSession = FindFirstObjectByType<CombatSession>(FindObjectsInactive.Include);
            SetPauseReason(PauseReason.BattleInfo, true);
        }

        /// <summary>
        /// 전투 정보 확인 상태를 종료하고 전투 시간 진행을 허용합니다.
        /// </summary>
        public void BeginBattle()
        {
            SetPauseReason(PauseReason.BattleInfo, false);
        }

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
            SetPauseReason(PauseReason.UserInterface, paused);
        }

        public void SetPauseReason(PauseReason reason, bool paused)
        {
            if (_resolved || reason == PauseReason.None)
            {
                return;
            }

            if (paused)
                _pauseReasons |= reason;
            else
                _pauseReasons &= ~reason;

            CurrentState = IsPaused
                ? BattleState.Paused
                : BattleState.Running;
            ApplyTimeScale();
        }

        /// <summary>
        /// 전투 씬을 재시작합니다. 시간 배율 복구도 전투 상태 소유자인 이 클래스가 담당합니다.
        /// </summary>
        public void RestartBattle()
        {
            ResetTimeScale();
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        private void ApplyTimeScale()
        {
            Time.timeScale = IsPaused || _resolved ? 0f : CurrentTimeScale;
        }

        private void Start()
        {
            // CombatSession resets the bus in Awake. Subscribe after all Awake calls.
            PassiveEventBus.OnSelfDeath += MarkOutcomeDirty;
            _outcomeDirty = true;
        }

        private void MarkOutcomeDirty(Unit unit)
        {
            _outcomeDirty = true;
            if (unit != null && unit.IsDead)
            {
                _pendingDeadUnits.Add(unit);
            }
        }

        private void LateUpdate()
        {
            if (_combatSession != null && !_combatSession.IsBattleRunning) return;
            if (_resolved || !_outcomeDirty) return;
            _outcomeDirty = false;
            // Evaluate after synchronous death/follow-up effects have finished.
            if (SystemBus.Get<CombatFacade>() is CombatFacade facade && facade.TryGetBattleOutcome(out bool victory))
                ResolveBattle(victory);
        }

        /// <summary>
        /// 승패를 판정하고 전투를 종료시킵니다.
        /// </summary>
        private void ResolveBattle(bool victory, bool forceBossVictory = false)
        {
            if (_resolved) return;

            _resolved = true;
            _wasBossBattle = forceBossVictory ||
                (BoardRunData.HasCurrentBattle && BoardRunData.IsBossBattle);
            _victory = victory;
            CurrentState = BattleState.Resolved;
            ApplyTimeScale();

            foreach (Unit deadUnit in _pendingDeadUnits)
            {
                deadUnit?.CompleteDeathPresentation();
            }
            _pendingDeadUnits.Clear();

            if (victory)
            {
                _combatSession?.SaveAllyHealthToRunData();
                BoardRunData.CompleteCurrentBattle();
            }

            // UI 컨트롤러에게 결과창을 띄우라고 신호를 보냅니다.
            PassiveEventBus.RaiseBattleEnd(victory);
            OnBattleResolved?.Invoke(victory);
        }

#if UNITY_EDITOR
        public void DebugResolveBossVictory()
        {
            ResolveBattle(true, true);
        }
#endif

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
            transitioner.LoadCombatReturnScene();
        }

        /// <summary>
        /// 보스전 승리 후 현재 게임 진행을 종료하고, 종료된 런의 Continue 데이터를
        /// 제거한 뒤 타이틀 씬으로 이동합니다.
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

            // 튜토리얼 전투는 저장된 메인 런과 분리된 임시 런입니다.
            // 튜토리얼에서 타이틀로 나갈 때 기존 Continue 데이터를 삭제하지 않습니다.
            if (TutorialSessionState.IsActive)
            {
                transitioner.LoadTitleScene();
                _isReturningToTitle = false;
                return;
            }

            // [수정] 런타임 상태와 저장 파일의 Continue 데이터를 함께 초기화 (BoardRunData.Clear()를 포함)
            SaveFacade saveFacade = SystemBus.Get<SaveFacade>();
            if (saveFacade == null)
            {
                Debug.LogError("[CombatSceneController] SaveFacade를 찾을 수 없어 런 데이터를 정리할 수 없습니다.", this);
                _isReturningToTitle = false;
                return;
            }

            saveFacade.ClearCurrentRun();
            bool saved = await saveFacade.SaveAsync();
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
            _pauseReasons = PauseReason.None;
            CurrentState = BattleState.Running;
            Time.timeScale = 1f;
        }

        private void OnDestroy()
        {
            PassiveEventBus.OnSelfDeath -= MarkOutcomeDirty;
            // 결과/일시정지 상태에서 에디터가 씬을 닫거나 재시작해도 다음 씬에 정지 상태가 전파되지 않도록 합니다.
            Time.timeScale = 1f;
        }
    }
}
