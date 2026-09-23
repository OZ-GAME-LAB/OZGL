using System.Collections.Generic;
using DG.Tweening;
using OzGameLab01.UI;
using UnityEngine;
using UnityEngine.UI;

namespace OzGameLab01.Controllers
{
    /// <summary>
    /// 보드 튜토리얼의 강조 화면 상태를 전담하는 Presenter입니다.
    /// Sequence Controller는 강조 구현을 알지 않고 Step 시작/종료 신호만 전달합니다.
    /// </summary>
    public sealed class TutorialHighlightPresenter
    {
        private readonly Object logContext;
        private TutorialTargetRegistry targetRegistry;
        private ReadySceneView readySceneView;

        private Sequence buttonSequence;
        private Button highlightedButton;
        private TutorialSequenceStepData buttonOwnerStep;
        private RectTransform highlightedRect;
        private Graphic highlightedGraphic;
        private Vector3 originalScale;
        private Color originalColor;

        private TutorialOutlinePulseView outlineHighlight;
        private TutorialSequenceStepData outlineOwnerStep;

        private readonly List<TutorialOutlinePulseView> formationHighlights =
            new();
        private TutorialSequenceStepData formationOwnerStep;

        public TutorialHighlightPresenter(
            TutorialTargetRegistry targetRegistry,
            ReadySceneView readySceneView,
            Object logContext)
        {
            this.targetRegistry = targetRegistry;
            this.readySceneView = readySceneView;
            this.logContext = logContext;
        }

        public void Configure(
            TutorialTargetRegistry registry,
            ReadySceneView sceneView)
        {
            targetRegistry = registry;
            readySceneView = sceneView;
        }

        public void ShowGuideStep(TutorialSequenceStepData step)
        {
            if (step == null)
                return;

            if (step.HighlightButton)
                StartButtonHighlight(step);

            ShowActionStep(step);
        }

        public void ShowActionStep(TutorialSequenceStepData step)
        {
            if (step == null)
                return;

            if (step.OutlineHighlight)
                StartOutlineHighlight(step);

            if (step.HighlightFormationSlots)
                StartFormationSlotHighlight(step);
        }

        public void HandleGuideDismissed(TutorialSequenceStepData step)
        {
            if (step == null)
                return;

            if (buttonOwnerStep != step ||
                !step.KeepButtonHighlightUntilClicked)
                StopButtonHighlight();

            if (outlineOwnerStep == step &&
                !step.KeepOutlineUntilReadyViewHidden)
                StopOutlineHighlight();

            if (formationOwnerStep == step &&
                !step.KeepFormationSlotHighlightUntilReadyViewHidden)
                StopFormationSlotHighlight();
        }

        public void HandleReadyViewHidden(ReadySceneViewType viewType)
        {
            if (outlineOwnerStep != null &&
                outlineOwnerStep.KeepOutlineUntilReadyViewHidden &&
                outlineOwnerStep.OutlineReleaseView == viewType)
                StopOutlineHighlight();

            if (formationOwnerStep != null &&
                formationOwnerStep.KeepFormationSlotHighlightUntilReadyViewHidden &&
                formationOwnerStep.FormationSlotHighlightReleaseView == viewType)
                StopFormationSlotHighlight();
        }

        public void CompleteSequence()
        {
            if (buttonOwnerStep == null ||
                !buttonOwnerStep.KeepButtonHighlightUntilClicked)
                StopButtonHighlight();
        }

        public void StopAll()
        {
            StopButtonHighlight();
            StopOutlineHighlight();
            StopFormationSlotHighlight();
        }

        private void StartButtonHighlight(TutorialSequenceStepData step)
        {
            StopButtonHighlight();

            if (targetRegistry == null ||
                !targetRegistry.TryGetButton(
                    step.HighlightButtonKey,
                    out Button button))
                return;

            highlightedRect = button.transform as RectTransform;
            highlightedGraphic = button.targetGraphic;
            if (highlightedRect == null)
                return;

            highlightedButton = button;
            buttonOwnerStep = step;

            if (step.KeepButtonHighlightUntilClicked)
                highlightedButton.onClick.AddListener(HandleButtonClicked);

            originalScale = highlightedRect.localScale;
            if (highlightedGraphic != null)
                originalColor = highlightedGraphic.color;

            buttonSequence = DOTween.Sequence()
                .SetUpdate(step.IgnoreTimeScale);
            buttonSequence.Append(
                highlightedRect
                    .DOScale(
                        originalScale * step.HighlightScale,
                        step.HighlightHalfDuration)
                    .SetEase(step.HighlightEase));

            if (highlightedGraphic != null)
            {
                buttonSequence.Join(
                    highlightedGraphic
                        .DOColor(
                            step.HighlightColor,
                            step.HighlightHalfDuration)
                        .SetEase(step.HighlightEase));
            }

            buttonSequence.Append(
                highlightedRect
                    .DOScale(originalScale, step.HighlightHalfDuration)
                    .SetEase(step.HighlightEase));

            if (highlightedGraphic != null)
            {
                buttonSequence.Join(
                    highlightedGraphic
                        .DOColor(originalColor, step.HighlightHalfDuration)
                        .SetEase(step.HighlightEase));
            }

            buttonSequence.SetLoops(-1, LoopType.Restart);
        }

        private void HandleButtonClicked()
        {
            StopButtonHighlight();
        }

        private void StopButtonHighlight()
        {
            if (highlightedButton != null)
                highlightedButton.onClick.RemoveListener(HandleButtonClicked);

            buttonSequence?.Kill();
            buttonSequence = null;

            if (highlightedRect != null)
                highlightedRect.localScale = originalScale;

            if (highlightedGraphic != null)
                highlightedGraphic.color = originalColor;

            highlightedButton = null;
            buttonOwnerStep = null;
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

            outlineHighlight = highlightView;
            outlineOwnerStep = step;
            PlayOutline(outlineHighlight, step);
        }

        private void StopOutlineHighlight()
        {
            outlineHighlight?.StopHighlight();
            outlineHighlight = null;
            outlineOwnerStep = null;
        }

        private void StartFormationSlotHighlight(
            TutorialSequenceStepData step)
        {
            StopFormationSlotHighlight();

            UnitView unitView = readySceneView != null
                ? readySceneView.UnitView
                : null;
            if (unitView == null)
            {
                Debug.LogWarning(
                    $"[TutorialHighlightPresenter] UnitView를 찾지 못해 슬롯 그룹 강조를 시작할 수 없습니다. Step: {step.StepName}",
                    logContext);
                return;
            }

            Transform slotRoot = unitView.SlotContentRoot;
            if (slotRoot == null)
                return;

            UnitSlotItemView[] slots =
                slotRoot.GetComponentsInChildren<UnitSlotItemView>(true);
            for (int i = 0; i < slots.Length; i++)
            {
                UnitSlotItemView slot = slots[i];
                if (slot == null ||
                    !IsFormationSlotInGroup(
                        slot,
                        step.FormationSlotHighlightGroup) ||
                    slot.SlotIcon == null)
                    continue;

                TutorialOutlinePulseView highlight =
                    slot.SlotIcon.GetComponent<TutorialOutlinePulseView>();
                if (highlight == null)
                {
                    highlight = slot.SlotIcon.gameObject
                        .AddComponent<TutorialOutlinePulseView>();
                }

                formationHighlights.Add(highlight);
                PlayOutline(highlight, step);
            }

            if (formationHighlights.Count == 0)
            {
                Debug.LogWarning(
                    $"[TutorialHighlightPresenter] 강조할 {step.FormationSlotHighlightGroup} 슬롯을 찾지 못했습니다. Step: {step.StepName}",
                    logContext);
                return;
            }

            formationOwnerStep = step;
        }

        private void StopFormationSlotHighlight()
        {
            for (int i = 0; i < formationHighlights.Count; i++)
                formationHighlights[i]?.StopHighlight();

            formationHighlights.Clear();
            formationOwnerStep = null;
        }

        private static void PlayOutline(
            TutorialOutlinePulseView highlight,
            TutorialSequenceStepData step)
        {
            highlight.PlayHighlight(
                step.OutlineColor,
                step.OutlineMinDistance,
                step.OutlineMaxDistance,
                step.OutlineMinAlpha,
                step.OutlineMaxAlpha,
                step.OutlineHalfDuration,
                step.OutlineEase,
                step.OutlineIgnoreTimeScale);
        }

        private static bool IsFormationSlotInGroup(
            UnitSlotItemView slot,
            TutorialFormationSlotHighlightGroup group)
        {
            return group switch
            {
                TutorialFormationSlotHighlightGroup.AllFormationSlots => true,
                TutorialFormationSlotHighlightGroup.BattleFormationSlots =>
                    slot.IsBattleSlot,
                TutorialFormationSlotHighlightGroup.SupportFormationSlots =>
                    slot.IsSupportSlot,
                _ => false
            };
        }
    }
}
