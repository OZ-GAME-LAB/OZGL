using DG.Tweening;
using TMPro;
using UnityEngine;

namespace OzGameLab01.UI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class RollingNumberView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TMP_Text valueText;
        [SerializeField] private TMP_Text nextValueText;

        [Header("Animation")]
        [SerializeField, Min(0f)] private float duration = 0.2f;
        [SerializeField] private Ease ease = Ease.OutCubic;

        private RectTransform viewport;
        private Sequence sequence;

        // 애니메이션 진행 중에도 가장 최근 요청 값을 보관합니다.
        private int targetValue;
        private bool hasValue;

#if UNITY_EDITOR
        [Header("Editor Test")]
        [SerializeField] private int testValue = 1;
#endif

        #region Lifecycle

        private void OnDisable()
        {
            StopTween();

            if (hasValue && valueText != null && nextValueText != null)
                ShowImmediately(targetValue);
        }

        private void OnDestroy()
        {
            StopTween();
        }

        #endregion

        #region Public API

        public void SetValue(int value, bool immediate = false)
        {
            if (valueText == null || nextValueText == null)
                return;

            if (viewport == null)
                viewport = transform as RectTransform;

            if (hasValue && targetValue == value && !immediate)
                return;

            bool hadValue = hasValue;
            int previousValue = targetValue;

            StopTween();

            targetValue = value;
            hasValue = true;

            if (!hadValue || immediate ||
                !isActiveAndEnabled || duration <= 0f)
            {
                ShowImmediately(value);
                return;
            }

            float height = viewport.rect.height;

            // 레이아웃이 아직 계산되지 않았으면 즉시 표시합니다.
            if (height <= 0f)
            {
                ShowImmediately(value);
                return;
            }

            // 연속 변경 시 이전 목표 값을 출발점으로 정리합니다.
            ShowImmediately(previousValue);

            nextValueText.text = value.ToString();
            nextValueText.rectTransform.anchoredPosition = new Vector2(0f, -height);
            nextValueText.gameObject.SetActive(true);

            sequence = DOTween.Sequence();
            sequence.SetUpdate(true);

            sequence.Append(
                valueText.rectTransform
                    .DOAnchorPosY(height, duration)
                    .SetEase(ease));

            sequence.Join(
                nextValueText.rectTransform
                    .DOAnchorPosY(0f, duration)
                    .SetEase(ease));

            sequence.OnComplete(() =>
            {
                sequence = null;
                ShowImmediately(targetValue);
            });
        }

        #endregion

        #region Animation Helpers

        private void ShowImmediately(int value)
        {
            valueText.text = value.ToString();
            valueText.rectTransform.anchoredPosition = Vector2.zero;

            nextValueText.rectTransform.anchoredPosition = Vector2.zero;
            nextValueText.gameObject.SetActive(false);
        }

        private void StopTween()
        {
            sequence?.Kill(false);
            sequence = null;
        }

        #endregion

#if UNITY_EDITOR
        #region Inspector Test

        [ContextMenu("Test/Set Value Immediately")]
        private void TestSetValueImmediately()
        {
            if (!Application.isPlaying)
                return;

            SetValue(testValue, immediate: true);
        }

        [ContextMenu("Test/Animate To Test Value")]
        private void TestAnimateToValue()
        {
            if (!Application.isPlaying)
                return;

            // 최초 호출은 즉시 표시되므로 시작 값을 먼저 준비합니다.
            if (!hasValue)
                SetValue(0, immediate: true);

            SetValue(testValue);
        }

        [ContextMenu("Test/Increase By One")]
        private void TestIncreaseByOne()
        {
            if (!Application.isPlaying)
                return;

            if (!hasValue)
                SetValue(testValue, immediate: true);

            SetValue(targetValue + 1);
        }

        #endregion
#endif
    }
}