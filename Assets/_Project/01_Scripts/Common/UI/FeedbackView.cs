using DG.Tweening;
using TMPro;
using UnityEngine;

namespace OzGameLab01.UI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class FeedbackView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TMP_Text messageText;

        [Header("Fade")]
        [SerializeField, Min(0f)] private float fadeInDuration = 0.2f;
        [SerializeField, Min(0f)] private float fadeOutDuration = 0.15f;

        [Header("Input")]
        [SerializeField] private bool blockBackgroundInput = false;

        private CanvasGroup canvasGroup;
        private Tween fadeTween;
        private bool wantsVisible;

        #region Properties

        public bool IsVisible => gameObject.activeSelf;

        public string Message
        {
            get => messageText != null ? messageText.text : string.Empty;
            private set
            {
                if (messageText != null)
                    messageText.text = value ?? string.Empty;
            }
        }

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            Initialize();
        }

        private void OnEnable()
        {
            Initialize();

            wantsVisible = true;
            canvasGroup.alpha = 0f;
            ApplyInputState();

            FadeTo(1f, fadeInDuration, false);
        }

        private void OnDisable()
        {
            StopFade();
            wantsVisible = false;

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }
        }

        private void OnDestroy()
        {
            StopFade();
        }

        #endregion

        #region Public API

        public void Show()
        {
            Initialize();

            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
                return;
            }

            if (!isActiveAndEnabled || wantsVisible)
                return;

            wantsVisible = true;
            ApplyInputState();
            FadeTo(1f, fadeInDuration, false);
        }

        public void Show(string message)
        {
            SetMessage(message);
            Show();
        }

        public void Hide()
        {
            if (!isActiveAndEnabled || !wantsVisible)
                return;

            wantsVisible = false;
            FadeTo(0f, fadeOutDuration, true);
        }

        /// <summary>
        /// 초기화나 화면 교체 시 연출 없이 즉시 숨깁니다.
        /// </summary>
        public void HideImmediate()
        {
            StopFade();
            wantsVisible = false;
            gameObject.SetActive(false);
        }

        public void SetMessage(string message)
        {
            Message = message;
        }

        #endregion

        #region Initialization

        private void Initialize()
        {
            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();
        }

        #endregion

        #region Input

        private void ApplyInputState()
        {
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = blockBackgroundInput;
        }

        #endregion

        #region Animation

        private void FadeTo(float alpha, float duration, bool hideOnComplete)
        {
            StopFade();

            if (duration <= 0f)
            {
                canvasGroup.alpha = alpha;

                if (hideOnComplete)
                    gameObject.SetActive(false);

                return;
            }

            fadeTween = canvasGroup
                .DOFade(alpha, duration)
                .SetEase(Ease.InOutSine)
                .SetUpdate(true)
                .OnComplete(() =>
                {
                    fadeTween = null;

                    if (hideOnComplete)
                        gameObject.SetActive(false);
                });
        }

        private void StopFade()
        {
            if (fadeTween == null)
                return;

            fadeTween.Kill(false);
            fadeTween = null;
        }

        #endregion

        #region Editor Validation

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (messageText == null)
                messageText = GetComponentInChildren<TMP_Text>(true);
        }
#endif

        #endregion

        #region Inspector Test

#if UNITY_EDITOR
        [ContextMenu("Test/Show Feedback")]
        private void TestShowFeedback()
        {
            if (Application.isPlaying)
                Show("피드백 테스트");
        }

        [ContextMenu("Test/Hide Feedback")]
        private void TestHideFeedback()
        {
            if (Application.isPlaying)
                Hide();
        }
#endif

        #endregion
    }
}