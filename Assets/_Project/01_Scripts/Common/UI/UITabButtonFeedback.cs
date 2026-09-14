using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace OzGameLab01.UI.Common
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Button))]
    public sealed class UITabButtonFeedback : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("References")]
        [SerializeField] private Transform visualRoot;
        [SerializeField] private Image background;

        [Header("Scale")]
        [SerializeField, Min(1f)] private float expandedScale = 1.03f;
        [SerializeField, Min(0f)] private float duration = 0.12f;

        [Header("Background")]
        [SerializeField] private Color normalColor = Color.black;
        [SerializeField] private Color selectedColor = new (0.7f, 0.1f, 0.1f);
        [SerializeField] private Color unselectedColor = Color.gray;

        private Button button;
        private Vector3 baseScale;
        private Coroutine scaleRoutine;

        private bool initialized;
        private bool isHovered;
        private bool isSelected;
        private bool hasSelection;

        private bool CanInteract => button != null && button.IsActive() && button.IsInteractable();

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
            StopScaleAnimation();
            isHovered = false;

            if (initialized && visualRoot != null)
                visualRoot.localScale = baseScale;
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

        public void SetSelected(bool selected, bool immediate = false)
        {
            Initialize();

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
            if (background != null)
            {
                background.color = !hasSelection ? normalColor : isSelected ? selectedColor : unselectedColor;
            }

            bool expanded = isSelected || (isHovered && CanInteract);
            Vector3 target = baseScale * (expanded ? expandedScale : 1f);

            StopScaleAnimation();

            if (immediate || !isActiveAndEnabled || duration <= 0f)
            {
                visualRoot.localScale = target;
                return;
            }

            scaleRoutine = StartCoroutine(AnimateScale(target));
        }

        private IEnumerator AnimateScale(Vector3 target)
        {
            Vector3 start = visualRoot.localScale;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                t = Mathf.SmoothStep(0f, 1f, t);

                visualRoot.localScale = Vector3.Lerp(start, target, t);
                yield return null;
            }

            visualRoot.localScale = target;
            scaleRoutine = null;
        }

        private void StopScaleAnimation()
        {
            if (scaleRoutine != null)
                StopCoroutine(scaleRoutine);

            scaleRoutine = null;
        }
    }
}