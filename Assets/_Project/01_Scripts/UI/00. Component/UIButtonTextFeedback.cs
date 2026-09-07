using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace OzGameLab01.UI.Common
{
    /// <summary>
    /// 텍스트 피드백를 제공하는 UI 버튼 컴포넌트입니다.
    /// 텍스트의 색상을 변경하여 마우스 오버 및 클릭 시 시각적 피드백을 제공하는 용임
    /// </summary>
    public sealed class UIButtonTextFeedback : MonoBehaviour,IPointerEnterHandler,IPointerExitHandler
    {
        private const float DEFAULT_FADE_DURATION = 0.15f;

        private static readonly Color DEFAULT_NORMAL_COLOR = Color.black;
        private static readonly Color DEFAULT_HOVER_COLOR = Color.white;

        [Header("References")]
        [SerializeField] private Button button;
        [SerializeField] private TMP_Text label;

        [Header("Color")]
        [SerializeField] private Color normalColor = DEFAULT_NORMAL_COLOR;
        [SerializeField] private Color hoverColor = DEFAULT_HOVER_COLOR;
        [SerializeField] private float fadeDuration = DEFAULT_FADE_DURATION;

        private Coroutine colorRoutine;
        private bool hasFeedback;

        private bool CanInteract => isActiveAndEnabled && button != null
            && button.IsActive() && button.IsInteractable() && label != null;

        private void Awake()
        {
            if (button == null)
                button = GetComponent<Button>();
        }

        private void OnEnable() => ResetFeedback();

        private void OnDisable() => ResetFeedback();

        private void Update()
        {
            // PointerExit 없이 입력이 차단되어도 호버 색상을 복원합니다.
            if (hasFeedback && !CanInteract)
                ResetFeedback();
        }

        private void ResetFeedback()
        {
            if (colorRoutine != null)
                StopCoroutine(colorRoutine);

            colorRoutine = null;
            hasFeedback = false;
            if (label != null)
                label.color = normalColor;
        }

        #region Pointer Events

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!CanInteract)
            {
                return;
            }

            PlayColor(hoverColor);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!CanInteract)
            {
                ResetFeedback();
                return;
            }

            PlayColor(normalColor);
        }

        #endregion

        #region Private Methods

        private void PlayColor(Color targetColor)
        {
            hasFeedback = true;
            if (colorRoutine != null)
            {
                StopCoroutine(colorRoutine);
            }

            colorRoutine = null;
            if (fadeDuration <= 0f)
            {
                label.color = targetColor;
                return;
            }

            colorRoutine = StartCoroutine(ColorRoutine(targetColor));
        }

        private IEnumerator ColorRoutine(Color targetColor)
        {
            Color startColor = label.color;
            float elapsed = 0f;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;

                float normalizedTime = Mathf.Clamp01(elapsed / fadeDuration);
                label.color = Color.Lerp(startColor, targetColor, normalizedTime);

                yield return null;
            }

            label.color = targetColor;
            colorRoutine = null;
        }

        #endregion
    }
}
