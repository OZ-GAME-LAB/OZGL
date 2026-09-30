using System;
using System.Collections.Generic;
using DG.Tweening;
using OzGameLab01.UI;
using OzGameLab01.UI.Battle;
using UnityEngine;
using UnityEngine.UI;

namespace OzGameLab01.Controllers
{
    /// <summary>
    /// Combat 씬에 배치된 튜토리얼 Canvas와 Guide를 사용하고,
    /// 런타임 입력 차단 및 Focus 표시를 전담합니다.
    /// 전투 시퀀스 진행이나 완료 조건 판단은 소유하지 않습니다.
    /// </summary>
    public sealed class CombatTutorialOverlayPresenter : IDisposable
    {
        private TutorialController guidePresenter;
        private bool ownsGuidePresenter;
        private RectTransform overlayRoot;
        private Canvas overlayCanvas;
        private int minimumSortingOrder;
        private TutorialGuideView guideView;
        private GameObject inputBlocker;
        private RectTransform focusRect;
        private Image focusImage;
        private Button focusButton;
        private readonly Image[] focusBorders = new Image[4];
        private Tween focusBorderTween;
        private RectTransform activeTarget;
        private RectTransform stepTarget;
        private CombatTutorialStepData activeStep;
        private IReadOnlyList<PlayerSlotItemView> activeFormationSlots;
        private bool targetPresentationDeferred;
        private readonly List<TutorialOutlinePulseView> formationSlotHighlights =
            new();

        public bool IsGuideVisible =>
            guidePresenter != null && guidePresenter.IsGuideVisible;

        public event Action GuideDismissed;
        public event Action TargetClicked;

        public void Initialize(
            RectTransform sceneOverlayRoot,
            TutorialGuideView guideView,
            int sortingOrder)
        {
            if (overlayRoot != null)
                return;

            if (sceneOverlayRoot == null)
                return;

            Canvas canvas = sceneOverlayRoot.GetComponent<Canvas>();
            if (canvas == null)
            {
                Debug.LogError(
                    "[CombatTutorialOverlayPresenter] CombatTutorialCanvas에 Canvas 컴포넌트가 없습니다.",
                    sceneOverlayRoot);
                return;
            }

            overlayRoot = sceneOverlayRoot;
            this.guideView = guideView;
            overlayCanvas = canvas;
            minimumSortingOrder = Mathf.Max(sortingOrder, canvas.sortingOrder);
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            EnsureTopmostSortingOrder();

            CanvasScaler scaler = overlayRoot.GetComponent<CanvasScaler>();
            if (scaler != null)
            {
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                scaler.matchWidthOrHeight = 0.5f;
            }

            if (guideView != null)
            {
                if (guideView.transform.parent != overlayRoot)
                {
                    Debug.LogError(
                        "[CombatTutorialOverlayPresenter] TutorialGuideView는 CombatTutorialCanvas의 직속 자식이어야 합니다.",
                        guideView);
                }

                RectTransform guideRect = guideView.transform as RectTransform;
                if (guideRect != null)
                {
                    StretchToParent(guideRect);
                    guideRect.localRotation = Quaternion.identity;
                    guideRect.localScale = Vector3.one;
                }
            }

            inputBlocker = CreateGraphicObject(
                "InputBlocker",
                overlayRoot,
                new Color(0f, 0f, 0f, 0.6f),
                raycastTarget: true);
            StretchToParent(inputBlocker.GetComponent<RectTransform>());
            inputBlocker.transform.SetAsFirstSibling();
            inputBlocker.SetActive(false);

            guidePresenter = overlayRoot.GetComponent<TutorialController>();
            if (guidePresenter == null)
            {
                guidePresenter = overlayRoot.gameObject
                    .AddComponent<TutorialController>();
                ownsGuidePresenter = true;
            }

            guidePresenter.ConfigureSceneViews(overlayRoot, guideView);
            guidePresenter.GuideView?.SetDismissOnAnyClick(true);
            guidePresenter.GuideDismissed += HandleGuideDismissed;

            CreateFocusTarget();
        }

        public void ShowStep(
            CombatTutorialStepData step,
            RectTransform target,
            IReadOnlyList<PlayerSlotItemView> formationSlots = null)
        {
            if (step == null)
                return;

            EnsureTopmostSortingOrder();

            activeStep = step;
            stepTarget = target;
            activeFormationSlots = formationSlots;

            bool hasFormationSlots =
                formationSlots != null && formationSlots.Count > 0;
            bool hasTarget = target != null || hasFormationSlots;
            // TargetClicked 전용 단계는 가이드를 먼저 읽고 닫은 뒤에만
            // 타깃 강조와 클릭 영역을 노출합니다.
            targetPresentationDeferred =
                step.ShowGuide &&
                step.AllowsTargetClick &&
                !step.AllowsGuideDismiss &&
                hasTarget;
            bool allowGuideDismiss =
                step.AllowsGuideDismiss ||
                targetPresentationDeferred ||
                (step.AllowsTargetClick && !hasTarget);

            if (guidePresenter != null)
            {
                guidePresenter.SetGuideDismissEnabled(allowGuideDismiss);
                if (step.ShowGuide)
                {
                    SetInputBlockerVisible(false);
                    guidePresenter.ShowGuide(
                        step.CharacterSprite,
                        step.CharacterName,
                        step.Dialogue,
                        step.ShowCharacter);
                    RefreshGuideLayout();
                }
                else
                {
                    SetInputBlockerVisible(true);
                }
            }

            if (targetPresentationDeferred)
            {
                HideFocus();
                StopFormationSlotHighlights();
                return;
            }

            ShowTargetPresentation(step, target, formationSlots);
        }

        private void ShowTargetPresentation(
            CombatTutorialStepData step,
            RectTransform target,
            IReadOnlyList<PlayerSlotItemView> formationSlots)
        {
            bool hasFormationSlots =
                formationSlots != null && formationSlots.Count > 0;
            bool hasTarget = target != null || hasFormationSlots;
            bool needsTarget = step.HighlightTarget || step.AllowsTargetClick;

            if (hasFormationSlots)
            {
                if (step.HighlightTarget)
                    ShowFormationSlotHighlights(step, formationSlots);

                // 슬롯 전체 영역을 투명 클릭 대상으로 사용합니다. 각 슬롯의 강조는
                // SlotVisual에 별도로 적용하므로 큰 사각형 테두리는 표시하지 않습니다.
                if (step.AllowsTargetClick && target != null)
                    ShowFocus(step, target, showBorder: false);
                else
                    HideFocus();
            }
            else if (hasTarget && needsTarget)
            {
                ShowFocus(step, target);
            }
            else
            {
                HideFocus();
            }
        }

        public void Tick()
        {
            if (activeTarget != null &&
                focusRect != null &&
                focusRect.gameObject.activeSelf)
            {
                SyncFocusRect(activeTarget);
            }
        }

        public void DismissGuide()
        {
            guidePresenter?.DismissGuide();
        }

        public void HideStep()
        {
            activeStep = null;
            stepTarget = null;
            activeFormationSlots = null;
            targetPresentationDeferred = false;
            HideFocus();
            StopFormationSlotHighlights();
            SetInputBlockerVisible(false);
            guidePresenter?.SetGuideDismissEnabled(true);
        }

        public void HideAllImmediate()
        {
            HideStep();
            guidePresenter?.HideAllImmediate();
        }

        public void Dispose()
        {
            StopFocusBorder();
            StopFormationSlotHighlights();

            if (guidePresenter != null)
                guidePresenter.GuideDismissed -= HandleGuideDismissed;

            if (focusButton != null)
                focusButton.onClick.RemoveListener(HandleTargetClicked);

            if (inputBlocker != null)
                UnityEngine.Object.Destroy(inputBlocker);

            if (focusRect != null)
                UnityEngine.Object.Destroy(focusRect.gameObject);

            if (ownsGuidePresenter && guidePresenter != null)
                UnityEngine.Object.Destroy(guidePresenter);

            guidePresenter = null;
            ownsGuidePresenter = false;
            overlayRoot = null;
            overlayCanvas = null;
            guideView = null;
            inputBlocker = null;
            focusRect = null;
            focusImage = null;
            focusButton = null;
            for (int i = 0; i < focusBorders.Length; i++)
                focusBorders[i] = null;
            activeTarget = null;
            stepTarget = null;
            activeStep = null;
            activeFormationSlots = null;
            targetPresentationDeferred = false;
        }

        private void EnsureTopmostSortingOrder()
        {
            if (overlayCanvas == null)
                return;

            int requiredOrder = minimumSortingOrder;
            Canvas[] canvases = UnityEngine.Object.FindObjectsByType<Canvas>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            for (int i = 0; i < canvases.Length; i++)
            {
                Canvas other = canvases[i];
                if (other == null ||
                    other == overlayCanvas ||
                    other.transform.IsChildOf(overlayRoot))
                {
                    continue;
                }

                if (other.sortingOrder >= requiredOrder)
                {
                    requiredOrder = Mathf.Min(other.sortingOrder + 1, 32767);
                }
            }

            overlayCanvas.sortingOrder = requiredOrder;
        }

        private void CreateFocusTarget()
        {
            GameObject focusObject = CreateGraphicObject(
                "FocusTarget",
                overlayRoot,
                Color.clear,
                raycastTarget: false);
            focusRect = focusObject.GetComponent<RectTransform>();
            focusRect.anchorMin = new Vector2(0.5f, 0.5f);
            focusRect.anchorMax = new Vector2(0.5f, 0.5f);
            focusRect.pivot = new Vector2(0.5f, 0.5f);
            focusImage = focusObject.GetComponent<Image>();

            Outline outline = focusObject.AddComponent<Outline>();
            outline.enabled = false;
            outline.useGraphicAlpha = false;

            focusButton = focusObject.AddComponent<Button>();
            focusButton.transition = Selectable.Transition.None;
            focusButton.targetGraphic = focusImage;
            focusButton.onClick.AddListener(HandleTargetClicked);

            CreateFocusBorders();
            focusObject.SetActive(false);
        }

        private void ShowFocus(
            CombatTutorialStepData step,
            RectTransform target,
            bool showBorder = true)
        {
            if (focusRect == null)
                return;

            activeTarget = target;
            focusRect.gameObject.SetActive(true);
            focusRect.SetAsLastSibling();
            focusImage.raycastTarget = step.AllowsTargetClick;
            focusButton.enabled = step.AllowsTargetClick;

            SyncFocusRect(target);

            if (step.HighlightTarget && showBorder)
            {
                PlayFocusBorder(step);
            }
            else
            {
                StopFocusBorder();
            }
        }

        private void ShowFormationSlotHighlights(
            CombatTutorialStepData step,
            IReadOnlyList<PlayerSlotItemView> formationSlots)
        {
            StopFormationSlotHighlights();

            for (int i = 0; i < formationSlots.Count; i++)
            {
                PlayerSlotItemView slot = formationSlots[i];
                Image slotVisual = slot != null ? slot.SlotVisualImage : null;
                if (slotVisual == null)
                    continue;

                TutorialOutlinePulseView highlight =
                    slotVisual.GetComponent<TutorialOutlinePulseView>();
                if (highlight == null)
                {
                    highlight = slotVisual.gameObject
                        .AddComponent<TutorialOutlinePulseView>();
                }

                highlight.PlayHighlight(
                    step.OutlineColor,
                    step.OutlineMinDistance,
                    step.OutlineMaxDistance,
                    step.OutlineMinAlpha,
                    step.OutlineMaxAlpha,
                    step.OutlineHalfDuration,
                    step.OutlineEase,
                    ignoreTimeScale: true);
                formationSlotHighlights.Add(highlight);
            }
        }

        private void StopFormationSlotHighlights()
        {
            for (int i = 0; i < formationSlotHighlights.Count; i++)
                formationSlotHighlights[i]?.StopHighlight();

            formationSlotHighlights.Clear();
        }

        private void HideFocus()
        {
            activeTarget = null;
            StopFocusBorder();

            if (focusRect != null)
                focusRect.gameObject.SetActive(false);
        }

        private void CreateFocusBorders()
        {
            focusBorders[0] = CreateBorder("TopBorder", focusRect);
            focusBorders[1] = CreateBorder("BottomBorder", focusRect);
            focusBorders[2] = CreateBorder("LeftBorder", focusRect);
            focusBorders[3] = CreateBorder("RightBorder", focusRect);

            RectTransform top = focusBorders[0].rectTransform;
            top.anchorMin = new Vector2(0f, 1f);
            top.anchorMax = Vector2.one;
            top.pivot = new Vector2(0.5f, 0.5f);

            RectTransform bottom = focusBorders[1].rectTransform;
            bottom.anchorMin = Vector2.zero;
            bottom.anchorMax = new Vector2(1f, 0f);
            bottom.pivot = new Vector2(0.5f, 0.5f);

            RectTransform left = focusBorders[2].rectTransform;
            left.anchorMin = Vector2.zero;
            left.anchorMax = new Vector2(0f, 1f);
            left.pivot = new Vector2(0.5f, 0.5f);

            RectTransform right = focusBorders[3].rectTransform;
            right.anchorMin = new Vector2(1f, 0f);
            right.anchorMax = Vector2.one;
            right.pivot = new Vector2(0.5f, 0.5f);
        }

        private static Image CreateBorder(string objectName, Transform parent)
        {
            GameObject borderObject = CreateGraphicObject(
                objectName,
                parent,
                Color.clear,
                raycastTarget: false);
            return borderObject.GetComponent<Image>();
        }

        private void PlayFocusBorder(CombatTutorialStepData step)
        {
            StopFocusBorder();
            ApplyFocusBorder(step, 0f);

            float value = 0f;
            focusBorderTween = DOTween
                .To(
                    () => value,
                    nextValue =>
                    {
                        value = nextValue;
                        ApplyFocusBorder(step, value);
                    },
                    1f,
                    Mathf.Max(0.01f, step.OutlineHalfDuration))
                .SetEase(step.OutlineEase)
                .SetLoops(-1, LoopType.Yoyo)
                .SetUpdate(true);
        }

        private void ApplyFocusBorder(CombatTutorialStepData step, float value)
        {
            float horizontalThickness = Mathf.Lerp(
                Mathf.Abs(step.OutlineMinDistance.x),
                Mathf.Abs(step.OutlineMaxDistance.x),
                value);
            float verticalThickness = Mathf.Lerp(
                Mathf.Abs(step.OutlineMinDistance.y),
                Mathf.Abs(step.OutlineMaxDistance.y),
                value);

            Color color = step.OutlineColor;
            color.a = Mathf.Lerp(
                step.OutlineMinAlpha,
                step.OutlineMaxAlpha,
                value);

            SetBorder(focusBorders[0], color, 0f, verticalThickness);
            SetBorder(focusBorders[1], color, 0f, verticalThickness);
            SetBorder(focusBorders[2], color, horizontalThickness, 0f);
            SetBorder(focusBorders[3], color, horizontalThickness, 0f);
        }

        private static void SetBorder(
            Image border,
            Color color,
            float width,
            float height)
        {
            if (border == null)
                return;

            border.color = color;
            border.rectTransform.anchoredPosition = Vector2.zero;
            border.rectTransform.sizeDelta = new Vector2(width, height);
        }

        private void StopFocusBorder()
        {
            focusBorderTween?.Kill();
            focusBorderTween = null;

            for (int i = 0; i < focusBorders.Length; i++)
            {
                if (focusBorders[i] != null)
                    focusBorders[i].color = Color.clear;
            }
        }

        private void SyncFocusRect(RectTransform target)
        {
            if (target == null || overlayRoot == null || focusRect == null)
                return;

            Vector3[] worldCorners = new Vector3[4];
            target.GetWorldCorners(worldCorners);

            Canvas targetCanvas = target.GetComponentInParent<Canvas>();
            Camera targetCamera = targetCanvas != null &&
                                  targetCanvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? targetCanvas.worldCamera
                : null;

            Vector2 min = new(float.PositiveInfinity, float.PositiveInfinity);
            Vector2 max = new(float.NegativeInfinity, float.NegativeInfinity);
            for (int i = 0; i < worldCorners.Length; i++)
            {
                Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(
                    targetCamera,
                    worldCorners[i]);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    overlayRoot,
                    screenPoint,
                    null,
                    out Vector2 localPoint);

                min = Vector2.Min(min, localPoint);
                max = Vector2.Max(max, localPoint);
            }

            Vector2 padding = activeStep != null
                ? activeStep.TargetPadding
                : Vector2.zero;
            focusRect.anchoredPosition = (min + max) * 0.5f;
            focusRect.sizeDelta = max - min + padding * 2f;
        }

        private void HandleGuideDismissed()
        {
            if (targetPresentationDeferred && activeStep != null)
            {
                targetPresentationDeferred = false;
                SetInputBlockerVisible(true);
                ShowTargetPresentation(
                    activeStep,
                    stepTarget,
                    activeFormationSlots);
            }

            GuideDismissed?.Invoke();
        }

        private void HandleTargetClicked()
        {
            TargetClicked?.Invoke();
        }

        private void SetInputBlockerVisible(bool visible)
        {
            if (inputBlocker != null)
                inputBlocker.SetActive(visible);
        }

        /// <summary>
        /// 비활성 상태로 씬에 배치된 Guide가 처음 켜질 때 CanvasScaler의
        /// 초기 갱신보다 먼저 표시되더라도 자식 그래픽이 올바른 Rect를 받도록 합니다.
        /// 씬 인스턴스의 직렬화 값이나 프리팹 원본은 변경하지 않습니다.
        /// </summary>
        private void RefreshGuideLayout()
        {
            if (overlayRoot == null || guideView == null)
                return;

            RectTransform guideRect = guideView.transform as RectTransform;
            if (guideRect == null)
                return;

            StretchToParent(guideRect);
            guideRect.localRotation = Quaternion.identity;
            guideRect.localScale = Vector3.one;

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(overlayRoot);
            LayoutRebuilder.ForceRebuildLayoutImmediate(guideRect);

            Graphic[] graphics = guideView.GetComponentsInChildren<Graphic>(true);
            for (int i = 0; i < graphics.Length; i++)
                graphics[i].SetAllDirty();

            Canvas.ForceUpdateCanvases();
        }

        private static GameObject CreateGraphicObject(
            string objectName,
            Transform parent,
            Color color,
            bool raycastTarget)
        {
            GameObject result = new(
                objectName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            result.transform.SetParent(parent, false);

            Image image = result.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = raycastTarget;
            return result;
        }

        private static void StretchToParent(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
