using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace OzGameLab01.UI.Common
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Button))]
    public sealed class UITabButtonFeedback : MonoBehaviour,IPointerEnterHandler,IPointerExitHandler
    {
        [Header("References")]
        [SerializeField] private Transform visualRoot;
        [SerializeField] private Image background;

        [Header("Tween")]
        [SerializeField] private Ease transitionEase = Ease.OutCubic;

        [Header("Scale")]
        [SerializeField, Min(1f)] private float expandedScale = 1.03f;
        [SerializeField, Min(0f)] private float duration = 0.25f;

        [Header("Background")]
        [SerializeField] private Color normalColor = Color.black;
        [SerializeField] private Color selectedColor = new(0.7f, 0.1f, 0.1f);
        [SerializeField] private Color unselectedColor = Color.gray;

        private Button button;
        private Vector3 baseScale;
        private Sequence transitionSequence;

        private bool initialized;
        private bool isHovered;
        private bool isSelected;
        private bool hasSelection;

        private bool CanInteract => isActiveAndEnabled && button != null && button.IsActive() && button.IsInteractable();

        private void Awake()
        {
            Initialize();
        }

        private void OnEnable()
        {
            Initialize();

            isHovered = false;
            RefreshVisuals(true);
        }

        private void OnDisable()
        {
            StopTransition();
            isHovered = false;

            if (initialized && visualRoot != null)
                visualRoot.localScale = baseScale;
        }

        private void OnDestroy()
        {
            StopTransition();
        }

        private void Update()
        {
            if (isHovered && !CanInteract)
            {
                isHovered = false;
                RefreshVisuals();
            }
        }

        private void Initialize()
        {
            if (initialized)
                return;

            button = GetComponent<Button>();

            if (visualRoot == null)
                visualRoot = transform;

            baseScale = visualRoot.localScale;
            initialized = true;
        }

        /// <summary>
        /// 선택 상태를 설정합니다.
        /// 비활성 중 전달된 상태는 다음 활성화 시 반영합니다.
        /// </summary>
        public void SetSelected(bool selected, bool immediate = false)
        {
            Initialize();

            if (hasSelection && isSelected == selected && !immediate)
                return;

            hasSelection = true;
            isSelected = selected;

            RefreshVisuals(immediate);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!CanInteract)
                return;

            isHovered = true;
            RefreshVisuals();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            isHovered = false;
            RefreshVisuals();
        }

        private void RefreshVisuals(bool immediate = false)
        {
            StopTransition();

            if (!isActiveAndEnabled)
                return;

            bool expanded = isSelected || (isHovered && CanInteract);

            Vector3 targetScale = baseScale * (expanded ? expandedScale : 1f);
            Color targetColor = !hasSelection ? normalColor : isSelected ? selectedColor : unselectedColor;

            if (immediate || duration <= 0f)
            {
                ApplyVisuals(targetScale, targetColor);
                return;
            }

            if (visualRoot == null && background == null)
                return;

            transitionSequence = DOTween.Sequence();
            transitionSequence.SetUpdate(true);

            if (visualRoot != null)
            {
                transitionSequence.Insert(0f,visualRoot.DOScale(targetScale, duration).SetEase(transitionEase));
            }

            if (background != null)
            {
                transitionSequence.Insert(0f,background.DOColor(targetColor, duration).SetEase(transitionEase));
            }
        }

        private void ApplyVisuals(Vector3 scale, Color color)
        {
            if (visualRoot != null)
                visualRoot.localScale = scale;

            if (background != null)
                background.color = color;
        }

        private void StopTransition()
        {
            if (transitionSequence == null)
                return;

            transitionSequence.Kill(false);
            transitionSequence = null;
        }
    }
}