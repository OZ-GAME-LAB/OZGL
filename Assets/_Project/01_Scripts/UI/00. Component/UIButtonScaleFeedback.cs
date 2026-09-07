using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace OzGameLab01.UI.Common
{
    /// <summary>
    /// UI 버튼의 시각적 피드백을 제공하는 컴포넌트입니다.
    /// 버튼의 스케일을 변경하여 마우스 오버 및 클릭 시 시각적 피드백을 제공하는 용임
    /// </summary>
    public sealed class UIButtonScaleFeedback : MonoBehaviour,IPointerEnterHandler,IPointerExitHandler,IPointerDownHandler,IPointerUpHandler
    {
        private const float DEFAULT_NORMAL_SCALE = 1f;
        private const float DEFAULT_HOVER_SCALE = 1.03f;
        private const float DEFAULT_PRESSED_SCALE = 0.97f;
        private const float DEFAULT_DURATION = 0.12f;

        [SerializeField] private Transform visualRoot;

        [Header("Scale")]
        [SerializeField] private float normalScale = DEFAULT_NORMAL_SCALE;
        [SerializeField] private float hoverScale = DEFAULT_HOVER_SCALE;
        [SerializeField] private float pressedScale = DEFAULT_PRESSED_SCALE;
        [SerializeField] private float duration = DEFAULT_DURATION;

        private Coroutine scaleRoutine;
        private bool isHovered;
        private Button button;
        private bool hasFeedback;

        private bool CanInteract => isActiveAndEnabled && button != null
            && button.IsActive() && button.IsInteractable() && visualRoot != null;

        private void Awake()
        {
            button = GetComponent<Button>();
        }

        private void OnEnable() => ResetFeedback();

        private void OnDisable() => ResetFeedback();

        private void Update()
        {
            // 호버 중 Button 또는 상위 CanvasGroup이 입력을 막는 경우도 복원합니다.
            if (hasFeedback && !CanInteract)
                ResetFeedback();
        }

        private void ResetFeedback()
        {
            if (scaleRoutine != null)
                StopCoroutine(scaleRoutine);

            scaleRoutine = null;
            isHovered = false;
            hasFeedback = false;
            if (visualRoot != null)
                visualRoot.localScale = Vector3.one * normalScale;
        }

        #region Pointer Events

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!CanInteract) return;
            isHovered = true;
            PlayScale(hoverScale);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            isHovered = false;
            if (!CanInteract)
            {
                ResetFeedback();
                return;
            }
            PlayScale(normalScale);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!CanInteract || eventData.button != PointerEventData.InputButton.Left) return;
            PlayScale(pressedScale);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;
            if (!CanInteract)
            {
                ResetFeedback();
                return;
            }
            PlayScale(isHovered ? hoverScale : normalScale);
        }

        #endregion

        #region Private Methods

        private void PlayScale(float targetScale)
        {
            hasFeedback = true;
            if (scaleRoutine != null)
            {
                StopCoroutine(scaleRoutine);
            }

            scaleRoutine = null;
            if (duration <= 0f)
            {
                visualRoot.localScale = Vector3.one * targetScale;
                return;
            }

            scaleRoutine = StartCoroutine(ScaleRoutine(targetScale));
        }

        private IEnumerator ScaleRoutine(float targetScale)
        {
            Vector3 startScale = visualRoot.localScale;
            Vector3 targetScaleVector = Vector3.one * targetScale;

            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;

                float normalizedTime = Mathf.Clamp01(elapsed / duration);
                visualRoot.localScale = Vector3.Lerp(startScale, targetScaleVector, normalizedTime);

                yield return null;
            }

            visualRoot.localScale = targetScaleVector;
            scaleRoutine = null;
        }

        #endregion
    }
}
