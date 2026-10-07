using DG.Tweening;
using UnityEngine;

namespace OzGameLab01.UI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class OverlayTransitionView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private CanvasGroup rootGroup;
        [SerializeField] private CanvasGroup dimmerGroup;
        [SerializeField] private CanvasGroup panelGroup;
        [SerializeField] private RectTransform panelRoot;

        [Header("Animation")]
        [SerializeField, Min(0f)] private float showDuration = 0.2f;
        [SerializeField, Min(0f)] private float hideDuration = 0.15f;
        [SerializeField, Min(0f)] private float slideDistance = 20f;
        [SerializeField] private Ease showEase = Ease.OutCubic;
        [SerializeField] private Ease hideEase = Ease.InCubic;

        private Sequence transition;
        private Vector2 restingPosition;
        private bool initialized;
        private bool wantsVisible;

        #region Properties

        public bool WantsVisible => wantsVisible;

        #endregion

        #region Lifecycle

        private void Awake()
        {
            Initialize();
        }

        private void OnEnable()
        {
            Initialize();

            wantsVisible = true;
            StopTransition();
            ApplyHiddenVisuals();
            PlayShow();
        }

        private void OnDisable()
        {
            StopTransition();
            wantsVisible = false;

            if (initialized)
            {
                SetInput(false, false);
                ApplyHiddenVisuals();
            }
        }

        private void OnDestroy()
        {
            StopTransition();
        }

        #endregion

        #region Public API

        public void Show()
        {
            Initialize();

            if (!gameObject.activeSelf)
            {
                // OnEnable에서 열기 연출을 실행합니다.
                gameObject.SetActive(true);
                return;
            }

            if (!isActiveAndEnabled || wantsVisible)
                return;

            // 닫는 도중 다시 열면 현재 상태에서 이어갑니다.
            wantsVisible = true;
            PlayShow();
        }

        public void Hide()
        {
            Initialize();

            if (!gameObject.activeSelf)
                return;

            if (!isActiveAndEnabled)
            {
                HideImmediate();
                return;
            }

            if (!wantsVisible)
                return;

            wantsVisible = false;
            PlayHide();
        }

        public void HideImmediate()
        {
            Initialize();
            StopTransition();

            wantsVisible = false;
            SetInput(false, false);
            ApplyHiddenVisuals();

            gameObject.SetActive(false);
        }

        #endregion

        #region Initialization

        private void Initialize()
        {
            if (initialized)
                return;

            if (rootGroup == null)
                rootGroup = GetComponent<CanvasGroup>();

            if (panelRoot != null)
                restingPosition = panelRoot.anchoredPosition;

            initialized = true;
        }

        #endregion

        #region Animation

        private void PlayShow()
        {
            StopTransition();

            // 전환 중에는 배경 클릭을 막고 패널 조작은 잠급니다.
            SetInput(false, true);

            if (showDuration <= 0f)
            {
                CompleteShow();
                return;
            }

            transition = DOTween.Sequence();
            transition.SetUpdate(true);

            // 이미지 참조가 없어도 지정한 시간 후 완료합니다.
            transition.AppendInterval(showDuration);

            if (dimmerGroup != null)
            {
                transition.Insert(
                    0f,
                    dimmerGroup
                        .DOFade(1f, showDuration)
                        .SetEase(showEase));
            }

            if (panelGroup != null)
            {
                transition.Insert(
                    0f,
                    panelGroup
                        .DOFade(1f, showDuration)
                        .SetEase(showEase));
            }

            if (panelRoot != null)
            {
                transition.Insert(
                    0f,
                    panelRoot
                        .DOAnchorPos(restingPosition, showDuration)
                        .SetEase(showEase));
            }

            transition.OnComplete(CompleteShow);
        }

        private void PlayHide()
        {
            StopTransition();

            SetInput(false, true);

            if (hideDuration <= 0f)
            {
                CompleteHide();
                return;
            }

            transition = DOTween.Sequence();
            transition.SetUpdate(true);
            transition.AppendInterval(hideDuration);

            if (dimmerGroup != null)
            {
                transition.Insert(
                    0f,
                    dimmerGroup
                        .DOFade(0f, hideDuration)
                        .SetEase(hideEase));
            }

            if (panelGroup != null)
            {
                transition.Insert(
                    0f,
                    panelGroup
                        .DOFade(0f, hideDuration)
                        .SetEase(hideEase));
            }

            if (panelRoot != null)
            {
                transition.Insert(
                    0f,
                    panelRoot
                        .DOAnchorPos(GetHiddenPosition(), hideDuration)
                        .SetEase(hideEase));
            }

            transition.OnComplete(CompleteHide);
        }

        private void CompleteShow()
        {
            transition = null;

            if (dimmerGroup != null)
                dimmerGroup.alpha = 1f;

            if (panelGroup != null)
                panelGroup.alpha = 1f;

            if (panelRoot != null)
                panelRoot.anchoredPosition = restingPosition;

            SetInput(true, true);
        }

        private void CompleteHide()
        {
            transition = null;
            SetInput(false, false);
            gameObject.SetActive(false);
        }

        private void StopTransition()
        {
            transition?.Kill(false);
            transition = null;
        }

        #endregion

        #region Visual Helpers

        private Vector2 GetHiddenPosition()
        {
            return restingPosition + Vector2.down * slideDistance;
        }

        private void ApplyHiddenVisuals()
        {
            if (dimmerGroup != null)
                dimmerGroup.alpha = 0f;

            if (panelGroup != null)
                panelGroup.alpha = 0f;

            if (panelRoot != null)
                panelRoot.anchoredPosition = GetHiddenPosition();
        }

        private void SetInput(bool interactable, bool blocksRaycasts)
        {
            if (rootGroup == null)
                return;

            rootGroup.interactable = interactable;
            rootGroup.blocksRaycasts = blocksRaycasts;
        }

        #endregion

#if UNITY_EDITOR
        #region Inspector Test

        [ContextMenu("Test/Show")]
        private void TestShow()
        {
            if (!Application.isPlaying)
                return;

            Show();
        }

        [ContextMenu("Test/Replay Open")]
        private void TestReplayOpen()
        {
            if (!Application.isPlaying)
                return;

            HideImmediate();
            Show();
        }

        [ContextMenu("Test/Hide")]
        private void TestHide()
        {
            if (!Application.isPlaying)
                return;

            Hide();
        }

        [ContextMenu("Test/Hide Immediately")]
        private void TestHideImmediately()
        {
            if (!Application.isPlaying)
                return;

            HideImmediate();
        }

        #endregion
#endif
    }
}