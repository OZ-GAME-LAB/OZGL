using System;
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace OzGameLab01.UI
{
    [DisallowMultipleComponent]
    public sealed class TutorialHintView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RectTransform contentRoot;
        [SerializeField] private TMP_Text messageText;

        [Header("Show")]
        [SerializeField, Min(0f)] private float showDuration = 0.25f;
        [SerializeField] private Vector2 showOffset = new(0f, -15f);
        [SerializeField, Range(0.1f, 1f)] private float showStartScale = 0.95f;

        [Header("Hide")]
        [SerializeField, Min(0f)] private float hideDuration = 0.2f;

        [Header("Initial State")]
        [SerializeField] private bool startHidden = true;

        private RectTransform rootRect;
        private Canvas rootCanvas;

        private Vector2 basePosition;
        private Vector3 baseScale;

        private Sequence currentSequence;


        #region Properties

        public bool IsVisible => gameObject.activeSelf && canvasGroup != null && canvasGroup.alpha > 0f;
        public bool IsAnimating => currentSequence != null && currentSequence.IsActive();

        #endregion


        #region Unity Lifecycle

        private void Awake()
        {
            rootRect = transform as RectTransform;
            rootCanvas = GetComponentInParent<Canvas>();

            CacheDefaultTransform();

            if (startHidden)
                ApplyHiddenState(false);
        }

        private void OnDisable()
        {
            KillTween();
        }

        #endregion


        #region Public API

        /// <summary>
        /// 설명 문구를 변경합니다.
        /// </summary>
        public void SetText(string text)
        {
            if (messageText == null)
                return;

            messageText.text = text ?? string.Empty;
        }


        /// <summary>
        /// 설명창의 Canvas 내부 위치를 직접 지정합니다.
        /// </summary>
        public void SetPosition(Vector2 anchoredPosition)
        {
            if (contentRoot == null)
                return;

            basePosition = anchoredPosition;
            contentRoot.anchoredPosition = basePosition;
        }


        /// <summary>
        /// 지정한 RectTransform의 중앙 위치로 설명창을 이동합니다.
        /// 다른 UI 요소를 기준으로 설명창을 배치할 때 사용합니다.
        /// </summary>
        public void SetAnchor(RectTransform anchor)
        {
            SetAnchor(anchor,Vector2.zero);
        }


        /// <summary>
        /// 지정한 RectTransform의 중앙 위치에 추가 오프셋을 적용합니다.
        /// </summary>
        public void SetAnchor(RectTransform anchor,Vector2 anchoredOffset)
        {
            if (anchor == null ||
                rootRect == null ||
                rootCanvas == null ||
                contentRoot == null)
            {
                return;
            }

            Canvas anchorCanvas = anchor.GetComponentInParent<Canvas>();

            Camera anchorCamera = GetCanvasCamera(anchorCanvas);
            Camera rootCamera = GetCanvasCamera(rootCanvas);

            Vector3 anchorWorldPosition = anchor.TransformPoint(anchor.rect.center);

            Vector2 screenPosition = RectTransformUtility.WorldToScreenPoint(anchorCamera,anchorWorldPosition);

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rootRect,screenPosition,rootCamera,out Vector2 localPosition))
            {
                return;
            }

            SetPosition(localPosition + anchoredOffset);
        }


        /// <summary>
        /// 설명창 크기를 변경합니다.
        /// </summary>
        public void SetSize(Vector2 size)
        {
            if (contentRoot == null)
                return;

            contentRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,size.x);
            contentRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,size.y);
        }


        /// <summary>
        /// 등장 연출을 재생합니다.
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
                    .DOAnchorPos(basePosition, showDuration)
                    .SetEase(Ease.OutCubic));

            currentSequence.Join(
                contentRoot
                    .DOScale(baseScale, showDuration)
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
                    .DOAnchorPos(
                        basePosition + showOffset,
                        hideDuration)
                    .SetEase(Ease.InCubic));

            currentSequence.Join(
                contentRoot
                    .DOScale(
                        baseScale * showStartScale,
                        hideDuration)
                    .SetEase(Ease.InQuad));

            currentSequence.OnComplete(() =>
            {
                currentSequence = null;

                ResetContentTransform();

                gameObject.SetActive(false);

                onComplete?.Invoke();
            });
        }


        /// <summary>
        /// 연출 없이 즉시 표시합니다.
        /// </summary>
        public void ShowImmediate()
        {
            KillTween();

            gameObject.SetActive(true);

            canvasGroup.alpha = 1f;

            ResetContentTransform();
        }


        /// <summary>
        /// 연출 없이 즉시 숨깁니다.
        /// </summary>
        public void HideImmediate()
        {
            KillTween();
            ApplyHiddenState(true);
        }


        /// <summary>
        /// 현재 재생 중인 View 연출을 중단합니다.
        /// </summary>
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

            basePosition = contentRoot.anchoredPosition;
            baseScale = contentRoot.localScale;
        }


        private void PrepareShowState()
        {
            if (canvasGroup != null)
                canvasGroup.alpha = 0f;

            if (contentRoot == null)
                return;

            contentRoot.anchoredPosition = basePosition + showOffset;
            contentRoot.localScale = baseScale * showStartScale;
        }


        private void ResetContentTransform()
        {
            if (contentRoot == null)
                return;

            contentRoot.anchoredPosition = basePosition;
            contentRoot.localScale = baseScale;
        }


        private void ApplyHiddenState(bool deactivate)
        {
            if (canvasGroup != null)
                canvasGroup.alpha = 0f;

            ResetContentTransform();

            if (deactivate)
                gameObject.SetActive(false);
        }


        private Camera GetCanvasCamera(Canvas targetCanvas)
        {
            if (targetCanvas == null)
                return null;

            return targetCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : targetCanvas.worldCamera;
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
