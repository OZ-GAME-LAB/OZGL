using System;
using System.Collections.Generic;
using DG.Tweening;
using OzGameLab01.UI;
using UnityEngine;
using UnityEngine.UI;

namespace OzGameLab01.Controllers
{
    [DisallowMultipleComponent]
    public sealed class TutorialSequenceController : MonoBehaviour
    {
        [Header("Controllers")]
        [SerializeField] private TutorialController tutorialController;
        [SerializeField] private ReadySceneView readySceneView;
        [SerializeField] private BoardUIController boardUIController;
        [SerializeField] private TutorialTargetRegistry targetRegistry;

        [Header("Sequence Order")]
        [SerializeField] private bool playOnStart = true;
        [Tooltip("각 Step SO를 실행할 순서대로 등록합니다.")]
        [SerializeField] private List<TutorialSequenceStepData> steps = new();

        private int nextStepIndex;
        private TutorialSequenceStepData activeStep;
        private TutorialSequenceStepData pendingStep;
        private Button waitingTriggerButton;
        private string waitingTriggerButtonKey;

        private Tween pendingShowTween;
        private Sequence highlightSequence;
        private RectTransform highlightedRect;
        private Graphic highlightedGraphic;
        private Vector3 originalHighlightScale;
        private Color originalHighlightColor;

        private TutorialOutlinePulseView activeOutlineHighlight;
        private TutorialSequenceStepData outlineOwnerStep;

        public bool IsPlaying { get; private set; }
        public int NextStepIndex => nextStepIndex;
        public string ActiveStepName => activeStep != null
            ? activeStep.StepName
            : string.Empty;

        public event Action SequenceCompleted;

        private void OnEnable()
        {
            if (tutorialController != null)
                tutorialController.GuideDismissed += HandleGuideDismissed;

            if (readySceneView != null)
            {
                readySceneView.ViewVisibilityChanged += HandleReadyViewVisibilityChanged;

                if (readySceneView.UnitView != null)
                    readySceneView.UnitView.Hidden += HandleUnitViewHidden;
            }
        }

        private void Start()
        {
            if (boardUIController == null)
                boardUIController = FindFirstObjectByType<BoardUIController>();

            if (playOnStart)
                PlaySequence();
        }

        private void OnDisable()
        {
            if (tutorialController != null)
                tutorialController.GuideDismissed -= HandleGuideDismissed;

            if (readySceneView != null)
            {
                readySceneView.ViewVisibilityChanged -= HandleReadyViewVisibilityChanged;

                if (readySceneView.UnitView != null)
                    readySceneView.UnitView.Hidden -= HandleUnitViewHidden;
            }

            UnbindWaitingButton();
            CancelPendingStep();
            StopButtonHighlight();
            StopOutlineHighlight();
        }

        [ContextMenu("Play Tutorial Sequence")]
        public void PlaySequence()
        {
            StopSequence();

            if (tutorialController == null || steps.Count == 0)
                return;

            nextStepIndex = 0;
            IsPlaying = true;

            PrepareNextStepTrigger();
            TryShowNextStep(TutorialStepTrigger.SequenceStarted);
        }

        [ContextMenu("Stop Tutorial Sequence")]
        public void StopSequence()
        {
            IsPlaying = false;
            activeStep = null;

            UnbindWaitingButton();
            CancelPendingStep();
            StopButtonHighlight();
            StopOutlineHighlight();

            if (tutorialController != null && tutorialController.IsGuideVisible)
                tutorialController.HideAllImmediate();
        }

        public void NotifyManualTrigger(string triggerKey)
        {
            TutorialSequenceStepData step = GetNextStep();

            if (step == null || step.Trigger != TutorialStepTrigger.Manual)
                return;

            if (!string.Equals(
                    step.ManualTriggerKey,
                    triggerKey,
                    StringComparison.Ordinal))
                return;

            ScheduleNextStep();
        }

        public void NotifyButtonClicked(string buttonKey)
        {
            TutorialSequenceStepData step = GetNextStep();

            if (step == null || step.Trigger != TutorialStepTrigger.ButtonClicked)
                return;

            if (!string.Equals(
                    step.TriggerButtonKey,
                    buttonKey,
                    StringComparison.Ordinal))
                return;

            ScheduleNextStep();
        }

        public void NotifyReadyViewShown(ReadySceneViewType viewType)
        {
            HandleReadyViewVisibilityChanged(viewType,true);
        }

        public void NotifyReadyViewHidden(ReadySceneViewType viewType)
        {
            HandleReadyViewVisibilityChanged(viewType,false);
        }

        private void HandleGuideDismissed()
        {
            if (!IsPlaying || activeStep == null)
                return;

            TutorialSequenceStepData dismissedStep = activeStep;
            activeStep = null;
            StopButtonHighlight();

            if (outlineOwnerStep == dismissedStep &&
                !dismissedStep.KeepOutlineUntilReadyViewHidden)
                StopOutlineHighlight();

            if (nextStepIndex >= steps.Count)
            {
                CompleteSequence();
                return;
            }

            PrepareNextStepTrigger();
            TryShowNextStep(TutorialStepTrigger.PreviousGuideDismissed);
        }

        private void HandleReadyViewVisibilityChanged(
            ReadySceneViewType viewType,
            bool isVisible)
        {
            if (!isVisible &&
                outlineOwnerStep != null &&
                outlineOwnerStep.KeepOutlineUntilReadyViewHidden &&
                outlineOwnerStep.OutlineReleaseView == viewType)
                StopOutlineHighlight();

            TryShowNextReadyViewStep(viewType,isVisible);
        }

        private void HandleUnitViewHidden(UnitView view)
        {
            HandleReadyViewVisibilityChanged(
                ReadySceneViewType.Unit,
                false);
        }

        private void TryShowNextReadyViewStep(
            ReadySceneViewType viewType,
            bool isVisible)
        {
            TutorialSequenceStepData step = GetNextStep();

            if (step == null)
                return;

            TutorialStepTrigger requiredTrigger = isVisible
                ? TutorialStepTrigger.ReadyViewShown
                : TutorialStepTrigger.ReadyViewHidden;

            if (step.Trigger != requiredTrigger ||
                step.TargetReadyView != viewType)
                return;

            ScheduleNextStep();
        }

        private void TryShowNextStep(TutorialStepTrigger trigger)
        {
            TutorialSequenceStepData step = GetNextStep();

            if (step == null || step.Trigger != trigger)
                return;

            ScheduleNextStep();
        }

        private TutorialSequenceStepData GetNextStep()
        {
            if (!IsPlaying ||
                activeStep != null ||
                pendingStep != null ||
                nextStepIndex >= steps.Count)
                return null;

            return steps[nextStepIndex];
        }

        private void ScheduleNextStep()
        {
            TutorialSequenceStepData step = GetNextStep();

            if (step == null)
                return;

            UnbindWaitingButton();
            pendingStep = step;

            if (step.ShowDelay <= 0f)
            {
                ShowPendingStep();
                return;
            }

            pendingShowTween = DOVirtual.DelayedCall(
                step.ShowDelay,
                ShowPendingStep,
                step.IgnoreShowDelayTimeScale);
        }

        private void ShowPendingStep()
        {
            if (!IsPlaying || pendingStep == null)
                return;

            pendingShowTween = null;

            TutorialSequenceStepData step = pendingStep;
            activeStep = step;
            pendingStep = null;
            nextStepIndex++;

            if (!step.ShowGuide)
            {
                activeStep = null;

                if (nextStepIndex < steps.Count)
                    PrepareNextStepTrigger();
            }

            if (step.OpenRollView && step.ShowGuide && boardUIController != null)
            {
                boardUIController.RequestRollViewOpen(
                    () => ShowGuideForStep(step));
            }
            else
            {
                if (step.OpenRollView)
                    boardUIController?.RequestRollViewOpen();

                if (step.ShowGuide)
                    ShowGuideForStep(step);
            }

            if (!step.ShowGuide && step.OutlineHighlight)
                StartOutlineHighlight(step);

            if (!step.ShowGuide && nextStepIndex >= steps.Count)
                CompleteSequence();
        }

        private void ShowGuideForStep(TutorialSequenceStepData step)
        {
            if (!IsPlaying || activeStep != step)
                return;

            tutorialController.ShowGuide(
                step.CharacterSprite,
                step.CharacterName,
                step.Dialogue,
                step.ShowCharacter);

            if (step.HighlightButton)
                StartButtonHighlight(step);

            if (step.OutlineHighlight)
                StartOutlineHighlight(step);
        }

        private void CancelPendingStep()
        {
            if (pendingShowTween != null)
            {
                pendingShowTween.Kill();
                pendingShowTween = null;
            }

            pendingStep = null;
        }

        private void PrepareNextStepTrigger()
        {
            UnbindWaitingButton();

            TutorialSequenceStepData step = GetNextStep();

            if (step == null ||
                step.Trigger != TutorialStepTrigger.ButtonClicked ||
                targetRegistry == null ||
                !targetRegistry.TryGetButton(step.TriggerButtonKey,out Button button))
                return;

            waitingTriggerButton = button;
            waitingTriggerButtonKey = step.TriggerButtonKey;
            waitingTriggerButton.onClick.AddListener(HandleWaitingButtonClicked);
        }

        private void HandleWaitingButtonClicked()
        {
            NotifyButtonClicked(waitingTriggerButtonKey);
        }

        private void UnbindWaitingButton()
        {
            if (waitingTriggerButton != null)
                waitingTriggerButton.onClick.RemoveListener(HandleWaitingButtonClicked);

            waitingTriggerButton = null;
            waitingTriggerButtonKey = string.Empty;
        }

        private void StartButtonHighlight(TutorialSequenceStepData step)
        {
            StopButtonHighlight();

            if (targetRegistry == null ||
                !targetRegistry.TryGetButton(step.HighlightButtonKey,out Button button))
                return;

            highlightedRect = button.transform as RectTransform;
            highlightedGraphic = button.targetGraphic;

            if (highlightedRect == null)
                return;

            originalHighlightScale = highlightedRect.localScale;

            if (highlightedGraphic != null)
                originalHighlightColor = highlightedGraphic.color;

            highlightSequence = DOTween.Sequence()
                .SetUpdate(step.IgnoreTimeScale);

            highlightSequence.Append(
                highlightedRect
                    .DOScale(
                        originalHighlightScale * step.HighlightScale,
                        step.HighlightHalfDuration)
                    .SetEase(step.HighlightEase));

            if (highlightedGraphic != null)
            {
                highlightSequence.Join(
                    highlightedGraphic
                        .DOColor(
                            step.HighlightColor,
                            step.HighlightHalfDuration)
                        .SetEase(step.HighlightEase));
            }

            highlightSequence.Append(
                highlightedRect
                    .DOScale(
                        originalHighlightScale,
                        step.HighlightHalfDuration)
                    .SetEase(step.HighlightEase));

            if (highlightedGraphic != null)
            {
                highlightSequence.Join(
                    highlightedGraphic
                        .DOColor(
                            originalHighlightColor,
                            step.HighlightHalfDuration)
                        .SetEase(step.HighlightEase));
            }

            highlightSequence.SetLoops(-1,LoopType.Restart);
        }

        private void StopButtonHighlight()
        {
            if (highlightSequence != null)
            {
                highlightSequence.Kill();
                highlightSequence = null;
            }

            if (highlightedRect != null)
                highlightedRect.localScale = originalHighlightScale;

            if (highlightedGraphic != null)
                highlightedGraphic.color = originalHighlightColor;

            highlightedRect = null;
            highlightedGraphic = null;
        }

        private void StartOutlineHighlight(TutorialSequenceStepData step)
        {
            StopOutlineHighlight();

            if (targetRegistry == null ||
                !targetRegistry.TryGetOutline(
                    step.OutlineTargetKey,
                    out TutorialOutlinePulseView highlightView))
                return;

            activeOutlineHighlight = highlightView;
            outlineOwnerStep = step;

            activeOutlineHighlight.PlayHighlight(
                step.OutlineColor,
                step.OutlineMinDistance,
                step.OutlineMaxDistance,
                step.OutlineMinAlpha,
                step.OutlineMaxAlpha,
                step.OutlineHalfDuration,
                step.OutlineEase,
                step.OutlineIgnoreTimeScale);
        }

        private void StopOutlineHighlight()
        {
            if (activeOutlineHighlight != null)
                activeOutlineHighlight.StopHighlight();

            activeOutlineHighlight = null;
            outlineOwnerStep = null;
        }

        private void CompleteSequence()
        {
            IsPlaying = false;
            UnbindWaitingButton();
            CancelPendingStep();
            StopButtonHighlight();
            TutorialProgress.MarkCompleted();
            SequenceCompleted?.Invoke();
        }
    }
}
