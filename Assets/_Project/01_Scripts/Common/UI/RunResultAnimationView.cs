using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace OzGameLab01.UI
{
    /// <summary>최종 결과 화면의 연출을 담당합니다. 통계 집계와 씬 이동은 외부에서 처리합니다.</summary>
    [DisallowMultipleComponent]
    public sealed class RunResultAnimationView : MonoBehaviour
    {
        #region Nested Types

        [Serializable]
        public sealed class SlidingPanel
        {
            public RectTransform viewport;
            public RectTransform slideRoot;
            public CanvasGroup opacity;
        }

        #endregion

        [Header("Summary")]
        [SerializeField] private RectTransform summaryMotionRoot;
        [SerializeField] private RectTransform summaryScaleRoot;
        [SerializeField] private RectTransform headerRoot;
        [SerializeField] private CanvasGroup headerOpacity;
        [SerializeField] private RectTransform statsRevealRoot;
        [SerializeField] private CanvasGroup statsOpacity;
        [SerializeField] private CanvasGroup dimBackground;
        [Header("Positions")]
        [SerializeField] private RectTransform introCenterPoint;
        [SerializeField] private RectTransform expandedCenterPoint;
        [SerializeField] private RectTransform summaryFinalPoint;
        [Header("Right panels — icons first, button last")]
        [SerializeField] private SlidingPanel[] panels = Array.Empty<SlidingPanel>();
        [SerializeField] private Button mainButton;
        [SerializeField] private RunResultContentView contentView;
        [SerializeField] private RectMask2D buttonRevealMask;
        [Header("Playback (unscaled time)")]
        [SerializeField] private bool playOnEnable = true;
        [SerializeField, Range(0.1f, 1f)] private float initialScale = 0.65f;
        [SerializeField, Min(0.01f)] private float introDuration = 0.45f;
        [SerializeField, Min(0f)] private float titleHold = 0.45f;
        [SerializeField, Min(0.01f)] private float expandDuration = 0.7f;
        [SerializeField, Min(0f)] private float statisticsHold = 0.55f;
        [SerializeField, Min(0.01f)] private float summarySlideDuration = 0.55f;
        [SerializeField, Min(0.01f)] private float panelSlideDuration = 0.55f;
        [SerializeField, Min(0f)] private float panelStagger = 0.18f;
        [Header("Events")]
        [SerializeField] private UnityEvent presentationCompleted = new UnityEvent();
        [SerializeField] private UnityEvent mainClicked = new UnityEvent();

        private Sequence sequence;
        private bool initialized;
        private float expandedHeight;
        private Vector3 restingScale;
        private Vector2 restingSummaryPosition;
        private Vector2[] restingPanelPositions;
        private bool restingButtonInteractable;

        #region Properties

        /// <summary>결과 화면의 등장 연출이 현재 재생 중인지 반환합니다.</summary>
        public bool IsPlaying => sequence != null && sequence.IsActive() && sequence.IsPlaying();

        /// <summary>연출이 완료되었거나 최종 상태로 즉시 표시되었는지 반환합니다.</summary>
        public bool IsComplete { get; private set; }

        /// <summary>오브젝트 활성화 시 등장 연출을 자동 재생할지 설정합니다.</summary>
        public bool PlayOnEnable { get => playOnEnable; set => playOnEnable = value; }

        /// <summary>외부에서 접근할 수 있는 메인으로 버튼입니다.</summary>
        public Button MainButton => mainButton;

        /// <summary>연출 완료 시 호출되는 UnityEvent입니다. 즉시 표시나 취소 시에는 호출하지 않습니다.</summary>
        public UnityEvent PresentationCompleted => presentationCompleted;

        /// <summary>메인으로 버튼 클릭 시 호출되는 UnityEvent입니다.</summary>
        public UnityEvent MainClicked => mainClicked;

        #endregion

        #region Events

        /// <summary>연출이 끝나거나 완료 API로 건너뛰었을 때 이 View를 전달합니다.</summary>
        public event Action<RunResultAnimationView> Completed;

        /// <summary>메인으로 버튼을 클릭했을 때 이 View를 전달합니다. 씬 이동은 구독자가 처리합니다.</summary>
        public event Action<RunResultAnimationView> MainButtonClicked;

        #endregion

        #region Unity Lifecycle

        private void OnEnable()
        {
            if (mainButton != null) mainButton.onClick.AddListener(HandleMainClicked);
            if (playOnEnable) Replay();
        }

        private void OnDisable()
        {
            if (mainButton != null) mainButton.onClick.RemoveListener(HandleMainClicked);
            Stop();
            if (initialized) RestoreFinalPose();
            IsComplete = false;
        }

        private void OnDestroy() => Stop();

        #endregion

        #region Public API

        /// <summary>활성화된 플레이 모드에서 기존 연출을 취소하고 처음부터 다시 재생합니다.</summary>
        public void Replay()
        {
            if (!Application.isPlaying || !isActiveAndEnabled || !Initialize()) return;
            Stop();
            IsComplete = false;
            Canvas.ForceUpdateCanvases();
            if (contentView != null)
            {
                contentView.ResetScrollPositions();
                contentView.SetScrollInteraction(false);
            }
            if (buttonRevealMask != null) buttonRevealMask.enabled = true;
            Vector2 intro = PointPosition(introCenterPoint);
            Vector2 expanded = PointPosition(expandedCenterPoint);
            Vector2 final = PointPosition(summaryFinalPoint);
            float startScale = Mathf.Clamp(initialScale, 0.1f, 1f);
            SetIntroPose(intro, startScale);
            headerOpacity.alpha = 0f;
            statsOpacity.alpha = 0f;
            dimBackground.alpha = 0f;
            statsRevealRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 0f);
            mainButton.interactable = false;
            for (int i = 0; i < panels.Length; i++)
            {
                SlidingPanel panel = panels[i];
                panel.slideRoot.anchoredPosition = restingPanelPositions[i] + Vector2.left * panel.viewport.rect.width;
                panel.opacity.alpha = 0f;
                panel.opacity.blocksRaycasts = false;
            }

            sequence = DOTween.Sequence().SetUpdate(true);
            sequence.Append(DOTween.To(() => startScale, value => SetIntroPose(intro, value), 1f, Mathf.Max(0.01f, introDuration)).SetEase(Ease.OutCubic));
            sequence.Join(headerOpacity.DOFade(1f, introDuration));
            sequence.Join(dimBackground.DOFade(1f, introDuration));
            sequence.AppendInterval(Mathf.Max(0f, titleHold));
            sequence.Append(summaryMotionRoot.DOAnchorPos(expanded, expandDuration).SetEase(Ease.InOutCubic));
            sequence.Join(DOTween.To(() => statsRevealRoot.rect.height, value => statsRevealRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, value),expandedHeight, expandDuration).SetEase(Ease.InOutCubic));
            sequence.Join(statsOpacity.DOFade(1f, expandDuration));
            sequence.AppendInterval(Mathf.Max(0f, statisticsHold));
            sequence.Append(summaryMotionRoot.DOAnchorPos(final, summarySlideDuration).SetEase(Ease.InOutCubic));
            float panelsStart = sequence.Duration();
            for (int i = 0; i < panels.Length; i++)
            {
                SlidingPanel panel = panels[i];
                float at = panelsStart + i * Mathf.Max(0f, panelStagger);
                sequence.Insert(at, panel.slideRoot.DOAnchorPos(restingPanelPositions[i], panelSlideDuration).SetEase(Ease.OutCubic));
                sequence.Insert(at, panel.opacity.DOFade(1f, panelSlideDuration * 0.65f));
            }
            sequence.OnComplete(() =>
            {
                sequence = null;
                RestoreFinalPose();
                NotifyCompleted();
            });
        }

        /// <summary>최종 상태로 전환하고 완료 이벤트를 호출합니다. 이미 완료된 경우 무시합니다.</summary>
        public void CompleteImmediately()
        {
            if (!Application.isPlaying || !isActiveAndEnabled || !Initialize() || IsComplete) return;
            Stop();
            RestoreFinalPose();
            NotifyCompleted();
        }

        /// <summary>활성화된 플레이 모드에서 연출 없이 최종 상태를 표시합니다. 완료 이벤트는 호출하지 않습니다.</summary>
        public void ShowImmediate()
        {
            if (!Application.isPlaying || !isActiveAndEnabled || !Initialize()) return;
            Stop();
            RestoreFinalPose();
            IsComplete = true;
        }

        /// <summary>결과 화면을 비활성화하고 진행 중인 연출을 취소합니다.</summary>
        public void Hide() => gameObject.SetActive(false);

        #endregion

        #region Private Methods

        private bool Initialize()
        {
            if (initialized) return true;
            if (summaryMotionRoot == null || summaryScaleRoot == null || headerRoot == null ||
                headerOpacity == null || statsRevealRoot == null || statsOpacity == null ||
                dimBackground == null || introCenterPoint == null || expandedCenterPoint == null ||
                summaryFinalPoint == null || mainButton == null || panels == null)
            {
#if UNITY_EDITOR
                Debug.LogError("[RunResultAnimationView] 모든 연출 참조를 연결해주세요.", this);
#endif
                return false;
            }
            foreach (SlidingPanel panel in panels)
            {
                if (panel == null || panel.viewport == null || panel.slideRoot == null || panel.opacity == null)
                {
#if UNITY_EDITOR
                    Debug.LogError("[RunResultAnimationView] 슬라이드 패널의 참조가 누락되었습니다.", this);
#endif
                    return false;
                }
            }
            Canvas.ForceUpdateCanvases();
            expandedHeight = statsRevealRoot.rect.height;
            restingScale = summaryScaleRoot.localScale;
            restingSummaryPosition = summaryMotionRoot.anchoredPosition;
            restingButtonInteractable = mainButton.interactable;
            restingPanelPositions = new Vector2[panels.Length];
            for (int i = 0; i < panels.Length; i++) restingPanelPositions[i] = panels[i].slideRoot.anchoredPosition;
            initialized = true;
            return true;
        }

        private Vector2 PointPosition(RectTransform point)
        {
            RectTransform parent = (RectTransform)summaryMotionRoot.parent;
            Vector2 local = parent.InverseTransformPoint(point.position);
            Vector2 anchor = Vector2.Lerp(summaryMotionRoot.anchorMin, summaryMotionRoot.anchorMax, 0.5f);
            return local - (parent.rect.min + Vector2.Scale(parent.rect.size, anchor));
        }

        private void SetIntroPose(Vector2 position, float scale)
        {
            summaryScaleRoot.localScale = restingScale * scale;
            // 상단을 기준으로 크기를 변경해도 헤더 중심이 움직이지 않도록 위치를 보정합니다.
            position.y -= headerRoot.rect.height * restingScale.y * 0.5f * (1f - scale);
            summaryMotionRoot.anchoredPosition = position;
        }

        private void RestoreFinalPose()
        {
            summaryMotionRoot.anchoredPosition = isActiveAndEnabled ? PointPosition(summaryFinalPoint) : restingSummaryPosition;
            summaryScaleRoot.localScale = restingScale;
            statsRevealRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, expandedHeight);
            headerOpacity.alpha = statsOpacity.alpha = dimBackground.alpha = 1f;
            mainButton.interactable = restingButtonInteractable;
            if (contentView != null) contentView.SetScrollInteraction(true);
            // 등장 후에는 마스크를 해제하여 타이틀 버튼과 같은 호버 확대가 잘리지 않게 합니다.
            if (buttonRevealMask != null) buttonRevealMask.enabled = false;
            for (int i = 0; i < panels.Length; i++)
            {
                panels[i].slideRoot.anchoredPosition = restingPanelPositions[i];
                panels[i].opacity.alpha = 1f;
                panels[i].opacity.blocksRaycasts = true;
            }
        }

        private void Stop()
        {
            sequence?.Kill();
            sequence = null;
        }

        private void NotifyCompleted()
        {
            IsComplete = true;
            presentationCompleted.Invoke();
            Completed?.Invoke(this);
        }

        private void HandleMainClicked()
        {
            mainClicked.Invoke();
            MainButtonClicked?.Invoke(this);
        }
        #endregion

#if UNITY_EDITOR
        #region Editor Context Menus

        [ContextMenu("Animation/Replay (Play Mode)")]
        private void ReplayFromContextMenu() => Replay();

        [ContextMenu("Animation/Show Final State (Play Mode)")]
        private void ShowFinalStateFromContextMenu() => ShowImmediate();

        #endregion
#endif
    }
}
