using System;
using OzGameLab01.UI;
using UnityEngine;
using UnityEngine.UI;

namespace OzGameLab01.Controllers
{
    /// <summary>
    /// 전투 튜토리얼의 런타임 Canvas, Guide, 입력 차단 및 Focus 표시를 전담합니다.
    /// 전투 시퀀스 진행이나 완료 조건 판단은 소유하지 않습니다.
    /// </summary>
    public sealed class CombatTutorialOverlayPresenter : IDisposable
    {
        private TutorialController guidePresenter;
        private RectTransform overlayRoot;
        private GameObject inputBlocker;
        private RectTransform focusRect;
        private Image focusImage;
        private Button focusButton;
        private TutorialOutlinePulseView focusPulse;
        private RectTransform activeTarget;
        private CombatTutorialStepData activeStep;

        public bool IsGuideVisible =>
            guidePresenter != null && guidePresenter.IsGuideVisible;

        public event Action GuideDismissed;
        public event Action TargetClicked;

        public void Initialize(
            Transform owner,
            TutorialGuideView guidePrefab,
            int sortingOrder)
        {
            if (overlayRoot != null)
                return;

            GameObject canvasObject = new(
                "CombatTutorialCanvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(owner, false);

            overlayRoot = canvasObject.GetComponent<RectTransform>();
            StretchToParent(overlayRoot);

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = sortingOrder;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            inputBlocker = CreateGraphicObject(
                "InputBlocker",
                overlayRoot,
                new Color(0f, 0f, 0f, 0.6f),
                raycastTarget: true);
            StretchToParent(inputBlocker.GetComponent<RectTransform>());
            inputBlocker.SetActive(false);

            guidePresenter = canvasObject.AddComponent<TutorialController>();
            guidePresenter.ConfigureRuntime(overlayRoot, guidePrefab);
            guidePresenter.GuideDismissed += HandleGuideDismissed;

            CreateFocusTarget();
        }

        public void ShowStep(
            CombatTutorialStepData step,
            RectTransform target)
        {
            if (step == null)
                return;

            activeStep = step;
            activeTarget = target;

            bool hasTarget = target != null;
            bool needsTarget = step.HighlightTarget || step.AllowsTargetClick;
            bool allowGuideDismiss =
                step.AllowsGuideDismiss ||
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
                }
                else
                {
                    SetInputBlockerVisible(true);
                }
            }

            if (hasTarget && needsTarget)
                ShowFocus(step, target);
            else
                HideFocus();
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
            HideFocus();
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
            if (guidePresenter != null)
                guidePresenter.GuideDismissed -= HandleGuideDismissed;

            if (focusButton != null)
                focusButton.onClick.RemoveListener(HandleTargetClicked);

            if (overlayRoot != null)
                UnityEngine.Object.Destroy(overlayRoot.gameObject);

            guidePresenter = null;
            overlayRoot = null;
            inputBlocker = null;
            focusRect = null;
            focusImage = null;
            focusButton = null;
            focusPulse = null;
            activeTarget = null;
            activeStep = null;
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

            focusPulse = focusObject.AddComponent<TutorialOutlinePulseView>();
            focusObject.SetActive(false);
        }

        private void ShowFocus(
            CombatTutorialStepData step,
            RectTransform target)
        {
            if (focusRect == null)
                return;

            activeTarget = target;
            focusRect.gameObject.SetActive(true);
            focusRect.SetAsLastSibling();
            focusImage.raycastTarget = step.AllowsTargetClick;
            focusButton.enabled = step.AllowsTargetClick;

            SyncFocusRect(target);

            if (step.HighlightTarget)
            {
                focusPulse.PlayHighlight(
                    step.OutlineColor,
                    step.OutlineMinDistance,
                    step.OutlineMaxDistance,
                    step.OutlineMinAlpha,
                    step.OutlineMaxAlpha,
                    step.OutlineHalfDuration,
                    step.OutlineEase,
                    ignoreTimeScale: true);
            }
            else
            {
                focusPulse.StopHighlight();
            }
        }

        private void HideFocus()
        {
            activeTarget = null;
            focusPulse?.StopHighlight();

            if (focusRect != null)
                focusRect.gameObject.SetActive(false);
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
