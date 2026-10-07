using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OzGameLab01.UI
{
    [DisallowMultipleComponent]
    public sealed class UnitAcquirePopupView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RectTransform contentRoot;

        [SerializeField] private TMP_Text unitNameText;
        [SerializeField] private Image unitImage;
        [SerializeField] private TMP_Text messageText;

        [Header("Content")]
        [SerializeField] private string defaultMessage = "동료가 당신의 팀에 합류합니다!";

        [Header("Show")]
        [SerializeField, Min(0f)] private float showDuration = 0.3f;
        [SerializeField] private Vector2 showOffset = new(0f, -30f);
        [SerializeField, Range(0.1f, 1f)] private float showStartScale = 0.9f;

        [Header("Stay")]
        [SerializeField, Min(0f)] private float stayDuration = 1.5f;

        [Header("Hide")]
        [SerializeField, Min(0f)] private float hideDuration = 0.25f;

        private Sequence currentSequence;

        private Vector2 defaultPosition;
        private Vector3 defaultScale;


        #region Properties

        public bool IsVisible => gameObject.activeSelf && canvasGroup != null && canvasGroup.alpha > 0f;
        public bool IsPlaying => currentSequence != null && currentSequence.IsActive() && currentSequence.IsPlaying();
        public string UnitName => unitNameText != null ? unitNameText.text : string.Empty;
        public Sprite UnitSprite => unitImage != null ? unitImage.sprite : null;

        #endregion


        #region Unity Lifecycle

        private void Awake()
        {
            CacheDefaultTransform();
            ResetMessage();
        }


        private void OnDisable()
        {
            KillTween();
        }

        #endregion


        #region Public API

        public void SetData(
            string unitName,
            Sprite unitSprite)
        {
            SetUnitName(unitName);
            SetUnitSprite(unitSprite);
        }


        public void SetUnitName(string unitName)
        {
            if (unitNameText == null)
                return;

            unitNameText.text = unitName ?? string.Empty;
        }


        public void SetUnitSprite(Sprite sprite)
        {
            if (unitImage == null)
                return;

            unitImage.sprite = sprite;
            unitImage.enabled = sprite != null;
        }


        public void SetMessage(string message)
        {
            if (messageText == null)
                return;

            messageText.text = message ?? string.Empty;
        }


        public void ResetMessage()
        {
            SetMessage(defaultMessage);
        }


        /// <summary>
        /// 등장 연출만 재생합니다.
        /// 자동 Hide는 하지 않습니다.
        /// </summary>
        public void Show()
        {
            Show(null);
        }


        public void Show(Action onComplete)
        {
            KillTween();

            gameObject.SetActive(true);

            PrepareShowState();

            currentSequence = DOTween.Sequence()
                .SetUpdate(true);

            currentSequence.Join(
                canvasGroup
                    .DOFade(1f, showDuration)
                    .SetEase(Ease.OutQuad));

            currentSequence.Join(
                contentRoot
                    .DOAnchorPos(defaultPosition, showDuration)
                    .SetEase(Ease.OutCubic));

            currentSequence.Join(
                contentRoot
                    .DOScale(defaultScale, showDuration)
                    .SetEase(Ease.OutBack));

            currentSequence.OnComplete(() =>
            {
                currentSequence = null;
                onComplete?.Invoke();
            });
        }


        /// <summary>
        /// 퇴장 연출 후 GameObject를 비활성화합니다.
        /// </summary>
        public void Hide()
        {
            Hide(null);
        }


        public void Hide(Action onComplete)
        {
            if (!gameObject.activeSelf)
            {
                onComplete?.Invoke();
                return;
            }

            KillTween();

            currentSequence = DOTween.Sequence()
                .SetUpdate(true);

            currentSequence.Join(
                canvasGroup
                    .DOFade(0f, hideDuration)
                    .SetEase(Ease.InQuad));

            currentSequence.Join(
                contentRoot
                    .DOScale(
                        defaultScale * showStartScale,
                        hideDuration)
                    .SetEase(Ease.InQuad));

            currentSequence.OnComplete(() =>
            {
                currentSequence = null;

                gameObject.SetActive(false);

                onComplete?.Invoke();
            });
        }


        /// <summary>
        /// Show → 대기 → Hide를 순서대로 자동 재생합니다.
        /// 유닛 획득 알림의 기본 사용 API입니다.
        /// </summary>
        public void Play()
        {
            Play(null);
        }


        public void Play(Action onComplete)
        {
            KillTween();

            gameObject.SetActive(true);

            PrepareShowState();

            currentSequence = DOTween.Sequence()
                .SetUpdate(true);

            // Show
            currentSequence.Append(
                canvasGroup
                    .DOFade(1f, showDuration)
                    .SetEase(Ease.OutQuad));

            currentSequence.Join(
                contentRoot
                    .DOAnchorPos(defaultPosition, showDuration)
                    .SetEase(Ease.OutCubic));

            currentSequence.Join(
                contentRoot
                    .DOScale(defaultScale, showDuration)
                    .SetEase(Ease.OutBack));

            // Stay
            currentSequence.AppendInterval(stayDuration);

            // Hide
            currentSequence.Append(
                canvasGroup
                    .DOFade(0f, hideDuration)
                    .SetEase(Ease.InQuad));

            currentSequence.Join(
                contentRoot
                    .DOScale(
                        defaultScale * showStartScale,
                        hideDuration)
                    .SetEase(Ease.InQuad));

            currentSequence.OnComplete(() =>
            {
                currentSequence = null;

                gameObject.SetActive(false);

                onComplete?.Invoke();
            });
        }


        public void ShowImmediate()
        {
            KillTween();

            gameObject.SetActive(true);

            canvasGroup.alpha = 1f;

            ResetContentTransform();
        }


        public void HideImmediate()
        {
            KillTween();

            canvasGroup.alpha = 0f;

            ResetContentTransform();

            gameObject.SetActive(false);
        }


        public void StopAnimation()
        {
            KillTween();
        }

        #endregion


        #region Internal

        private void CacheDefaultTransform()
        {
            if (contentRoot == null)
                return;

            defaultPosition = contentRoot.anchoredPosition;
            defaultScale = contentRoot.localScale;
        }


        private void PrepareShowState()
        {
            canvasGroup.alpha = 0f;

            contentRoot.anchoredPosition = defaultPosition + showOffset;
            contentRoot.localScale = defaultScale * showStartScale;
        }


        private void ResetContentTransform()
        {
            if (contentRoot == null)
                return;

            contentRoot.anchoredPosition = defaultPosition;
            contentRoot.localScale = defaultScale;
        }


        private void KillTween()
        {
            if (currentSequence == null)
                return;

            currentSequence.Kill();
            currentSequence = null;
        }

        #endregion
    }
}