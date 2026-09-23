using System;
using System.Collections.Generic;
using DG.Tweening;
using OzGameLab01.Combat;
using OzGameLab01.Managers;
using OzGameLab01.UI;
using OzGameLab01.UI.Battle;
using UnityEngine;

namespace OzGameLab01.Controllers
{
    [DefaultExecutionOrder(-500)]
    [DisallowMultipleComponent]
    public sealed class CombatTutorialSequenceController : MonoBehaviour
    {
        [Header("Scene References (Optional)")]
        [SerializeField] private CombatSceneController combatSceneController;
        [SerializeField] private CombatSession combatSession;
        [SerializeField] private CombatUIView combatUIView;

        [Header("Presentation")]
        [SerializeField] private TutorialGuideView guidePrefab;
        [SerializeField] private int overlaySortingOrder = 30000;

        [Header("Sequence")]
        [SerializeField] private bool playAutomatically = true;
        [SerializeField] private bool playOnce = true;
        [SerializeField] private bool ignoreCompletedProgress;
        [SerializeField] private string progressKey = "Tutorial.CombatIntro.v1";
        [SerializeField] private List<CombatTutorialStepData> steps = new();

        private CombatTutorialOverlayPresenter overlayPresenter;
        private TutorialSequenceState<CombatTutorialStepData> sequenceState;
        private Tween pendingShowTween;
        private bool combatEventsBound;
        private bool tutorialPauseHeld;
        private bool targetCompletionRequested;

        public bool IsPlaying => sequenceState != null && sequenceState.IsPlaying;
        public string ActiveStepName => sequenceState?.ActiveStep != null
            ? sequenceState.ActiveStep.StepName
            : string.Empty;

        public event Action SequenceCompleted;

        private void Awake()
        {
            EnsureSequenceState();
            ResolveReferences();
            overlayPresenter = new CombatTutorialOverlayPresenter();
            overlayPresenter.Initialize(transform, guidePrefab, overlaySortingOrder);
            TryAcquireInitialPause();
        }

        private void OnEnable()
        {
            ResolveReferences();
            BindBattleReady();

            if (overlayPresenter != null)
            {
                overlayPresenter.GuideDismissed += HandleGuideDismissed;
                overlayPresenter.TargetClicked += HandleFocusClicked;
            }
        }

        private void Start()
        {
            if (combatSession != null && combatSession.IsBattleReady)
                HandleBattleReady();
        }

        private void LateUpdate()
        {
            overlayPresenter?.Tick();
        }

        private void OnDisable()
        {
            UnbindBattleReady();
            UnbindCombatEvents();

            if (overlayPresenter != null)
            {
                overlayPresenter.GuideDismissed -= HandleGuideDismissed;
                overlayPresenter.TargetClicked -= HandleFocusClicked;
            }

            StopSequenceInternal(hideGuide: true);
        }

        private void OnDestroy()
        {
            overlayPresenter?.Dispose();
            overlayPresenter = null;
        }

        [ContextMenu("Play Combat Tutorial")]
        public void PlaySequence()
        {
            EnsureSequenceState();
            if (!CanPlaySequence())
                return;

            StopSequenceInternal(hideGuide: true);

            sequenceState.Start();
            BindCombatEvents();

            if (combatSession != null && combatSession.IsBattleReady)
                TryScheduleNextStep(CombatTutorialStepTrigger.BattleReady);
        }

        [ContextMenu("Stop Combat Tutorial")]
        public void StopSequence()
        {
            StopSequenceInternal(hideGuide: true);
        }

        [ContextMenu("Reset Combat Tutorial Progress")]
        public void ResetProgress()
        {
            TutorialProgress.ResetFor(progressKey);
        }

        public void NotifyManualTrigger(string triggerKey)
        {
            CombatTutorialStepData step = GetNextStep();
            if (step == null ||
                step.Trigger != CombatTutorialStepTrigger.Manual ||
                !string.Equals(
                    step.ManualTriggerKey,
                    triggerKey,
                    StringComparison.Ordinal))
            {
                return;
            }

            ScheduleStep(step);
        }

        private void HandleBattleReady()
        {
            BindCombatEvents();

            if (playAutomatically && !IsPlaying)
                PlaySequence();
            else
                TryScheduleNextStep(CombatTutorialStepTrigger.BattleReady);
        }

        private void HandleEnemySkillUsed(Unit _, OzGameLab01.Data.SkillData __)
        {
            TryScheduleNextStep(CombatTutorialStepTrigger.EnemySkillUsed);
        }

        private void TryScheduleNextStep(CombatTutorialStepTrigger trigger)
        {
            CombatTutorialStepData step = GetNextStep();
            if (step == null || step.Trigger != trigger)
                return;

            ScheduleStep(step);
        }

        private CombatTutorialStepData GetNextStep()
        {
            return sequenceState?.PeekNext();
        }

        private void ScheduleStep(CombatTutorialStepData step)
        {
            if (step == null)
                return;

            if (!sequenceState.TryQueue(step))
                return;

            if (step.PauseCombat)
                AcquireTutorialPause();

            if (step.ShowDelay <= 0f)
            {
                ShowPendingStep();
                return;
            }

            pendingShowTween = DOVirtual.DelayedCall(
                step.ShowDelay,
                ShowPendingStep,
                ignoreTimeScale: true);
        }

        private void ShowPendingStep()
        {
            if (!IsPlaying || sequenceState.PendingStep == null)
                return;

            pendingShowTween = null;
            CombatTutorialStepData step = sequenceState.ActivatePending();
            if (step == null)
                return;

            targetCompletionRequested = false;

            bool hasTarget = TryResolveTarget(step.Target, out RectTransform target);
            bool needsTarget = step.HighlightTarget || step.AllowsTargetClick;

            if (needsTarget && !hasTarget)
            {
                Debug.LogWarning(
                    $"[CombatTutorialSequenceController] 전투 튜토리얼 대상을 찾지 못했습니다. Step: {step.StepName}, Target: {step.Target}",
                    this);
            }

            overlayPresenter?.ShowStep(step, hasTarget ? target : null);

            if (!step.ShowGuide && !step.AllowsTargetClick)
                CompleteActiveStep();
        }

        private void HandleGuideDismissed()
        {
            CombatTutorialStepData step = sequenceState.ActiveStep;
            if (step == null)
                return;

            if (targetCompletionRequested || step.AllowsGuideDismiss)
                CompleteActiveStep();
        }

        private void HandleFocusClicked()
        {
            CombatTutorialStepData step = sequenceState.ActiveStep;
            if (step == null || !step.AllowsTargetClick)
                return;

            targetCompletionRequested = true;

            if (step.ShowGuide &&
                overlayPresenter != null &&
                overlayPresenter.IsGuideVisible)
            {
                overlayPresenter.DismissGuide();
                return;
            }

            CompleteActiveStep();
        }

        private void CompleteActiveStep()
        {
            if (sequenceState.ActiveStep == null)
                return;

            CombatTutorialStepData completedStep =
                sequenceState.ClearActive();
            targetCompletionRequested = false;

            overlayPresenter?.HideStep();

            CombatTutorialStepData nextStep = sequenceState.PeekNext();
            bool nextStartsImmediately = nextStep != null &&
                                         nextStep.Trigger ==
                                         CombatTutorialStepTrigger.PreviousStepCompleted;
            bool keepPausedForNext = nextStartsImmediately &&
                                     nextStep.PauseCombat;

            if (completedStep.ResumeCombatOnComplete && !keepPausedForNext)
                ReleaseTutorialPause();

            if (nextStartsImmediately)
            {
                ScheduleStep(nextStep);
                return;
            }

            if (nextStep == null)
            {
                CompleteSequence();
                return;
            }

            if (tutorialPauseHeld)
            {
                Debug.LogWarning(
                    $"[CombatTutorialSequenceController] 다음 Step이 이벤트를 기다리는 동안 전투가 정지 상태입니다. 이전 Step의 Resume Combat On Complete 설정을 확인하세요. Step: {completedStep.StepName}",
                    this);
            }
        }

        private void CompleteSequence()
        {
            sequenceState.Complete();
            ReleaseTutorialPause();
            UnbindCombatEvents();

            if (playOnce && !string.IsNullOrWhiteSpace(progressKey))
                TutorialProgress.MarkCompletedFor(progressKey);

            SequenceCompleted?.Invoke();
        }

        private bool TryResolveTarget(
            CombatTutorialTarget targetType,
            out RectTransform target)
        {
            target = null;

            ResolveReferences();
            CombatMainView mainView = combatUIView != null
                ? combatUIView.MainView
                : null;
            if (mainView == null)
                return false;

            switch (targetType)
            {
                case CombatTutorialTarget.EnemyHpUI:
                    target = mainView.EnemyHeaderView != null
                        ? mainView.EnemyHeaderView.HealthHighlightTarget
                        : null;
                    break;

                case CombatTutorialTarget.EnemyCombatArea:
                    target = mainView.EnemyCombatArea;
                    break;

                case CombatTutorialTarget.EnemySkillArea:
                    target = mainView.EnemySkillArea;
                    break;
            }

            return target != null;
        }

        private void ResolveReferences()
        {
            if (combatSceneController == null)
            {
                combatSceneController =
                    FindFirstObjectByType<CombatSceneController>(
                        FindObjectsInactive.Include);
            }

            if (combatSession == null)
            {
                combatSession = FindFirstObjectByType<CombatSession>(
                    FindObjectsInactive.Include);
            }

            if (combatUIView == null)
            {
                combatUIView = FindFirstObjectByType<CombatUIView>(
                    FindObjectsInactive.Include);
            }
        }

        private bool CanPlaySequence()
        {
            SceneTransitioner transitioner = SceneTransitioner.Instance;
            if (transitioner == null || !transitioner.IsTutorialCombat)
                return false;

            EnsureSequenceState();
            if (!sequenceState.HasConfiguredSteps)
                return false;

            if (playOnce &&
                !ignoreCompletedProgress &&
                TutorialProgress.IsCompletedFor(progressKey))
            {
                return false;
            }

            if (guidePrefab == null)
            {
                for (int i = 0; i < steps.Count; i++)
                {
                    if (steps[i] != null && steps[i].ShowGuide)
                    {
                        Debug.LogError(
                            "[CombatTutorialSequenceController] Guide Prefab이 연결되지 않았습니다.",
                            this);
                        return false;
                    }
                }
            }

            return true;
        }

        private void TryAcquireInitialPause()
        {
            if (!playAutomatically || !CanPlaySequence())
                return;

            CombatTutorialStepData firstStep =
                sequenceState.GetFirstConfiguredStep();

            if (firstStep != null &&
                firstStep.Trigger == CombatTutorialStepTrigger.BattleReady &&
                firstStep.PauseCombat)
            {
                AcquireTutorialPause();
            }
        }

        private void AcquireTutorialPause()
        {
            ResolveReferences();
            if (combatSceneController == null)
                return;

            combatSceneController.SetPauseReason(
                CombatSceneController.PauseReason.Tutorial,
                true);
            tutorialPauseHeld = true;
        }

        private void ReleaseTutorialPause()
        {
            if (!tutorialPauseHeld)
                return;

            if (combatSceneController != null)
            {
                combatSceneController.SetPauseReason(
                    CombatSceneController.PauseReason.Tutorial,
                    false);
            }

            tutorialPauseHeld = false;
        }

        private void BindBattleReady()
        {
            if (combatSession == null)
                return;

            combatSession.BattleReady -= HandleBattleReady;
            combatSession.BattleReady += HandleBattleReady;
        }

        private void UnbindBattleReady()
        {
            if (combatSession != null)
                combatSession.BattleReady -= HandleBattleReady;
        }

        private void BindCombatEvents()
        {
            if (combatEventsBound)
                return;

            PassiveEventBus.OnEnemySkillUsed += HandleEnemySkillUsed;
            combatEventsBound = true;
        }

        private void UnbindCombatEvents()
        {
            if (!combatEventsBound)
                return;

            PassiveEventBus.OnEnemySkillUsed -= HandleEnemySkillUsed;
            combatEventsBound = false;
        }

        private void StopSequenceInternal(bool hideGuide)
        {
            sequenceState?.Stop();
            targetCompletionRequested = false;

            if (pendingShowTween != null)
            {
                pendingShowTween.Kill();
                pendingShowTween = null;
            }

            if (hideGuide)
                overlayPresenter?.HideAllImmediate();
            else
                overlayPresenter?.HideStep();

            ReleaseTutorialPause();
            UnbindCombatEvents();
        }

        private void EnsureSequenceState()
        {
            sequenceState ??=
                new TutorialSequenceState<CombatTutorialStepData>(steps);
        }
    }
}
