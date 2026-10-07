using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace OzGameLab01.UI
{
    public enum PlacementFeedbackState
    {
        None,
        Valid,
        Invalid
    }

    [DisallowMultipleComponent]
    public sealed class PlacementSlotFeedbackView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Image validHighlight;
        [SerializeField] private Image invalidHighlight;

        [Header("Animation")]
        [SerializeField, Min(0f)] private float duration = 0.12f;
        [SerializeField] private Ease ease = Ease.OutCubic;

        private Sequence transition;
        private PlacementFeedbackState currentState;
        private bool highlightOrderDirty;

        #region Properties

        public PlacementFeedbackState CurrentState => currentState;

        #endregion

        #region Lifecycle

        private void Awake()
        {
            ConfigureHighlights();
        }

        private void OnEnable()
        {
            Clear(immediate: true);
            highlightOrderDirty = true;
        }

        private void OnDisable()
        {
            Clear(immediate: true);
        }

        private void OnDestroy()
        {
            StopTransition();
        }

        private void OnTransformChildrenChanged()
        {
            highlightOrderDirty = true;
        }

        private void LateUpdate()
        {
            if (!highlightOrderDirty)
                return;

            BringHighlightToFront(validHighlight);
            BringHighlightToFront(invalidHighlight);

            highlightOrderDirty = false;
        }

        #endregion

        #region Public API

        public void SetState(PlacementFeedbackState state,bool immediate = false)
        {
            ConfigureHighlights();

            if (!isActiveAndEnabled)
                state = PlacementFeedbackState.None;

            highlightOrderDirty = true;

            if (currentState == state && !immediate)
                return;

            StopTransition();
            currentState = state;

            float validAlpha = state == PlacementFeedbackState.Valid ? 1f : 0f;
            float invalidAlpha = state == PlacementFeedbackState.Invalid ? 1f : 0f;

            if (immediate || !isActiveAndEnabled || duration <= 0f)
            {
                ApplyAlpha(validHighlight, validAlpha);
                ApplyAlpha(invalidHighlight, invalidAlpha);
                return;
            }

            if (validHighlight == null && invalidHighlight == null)
                return;

            transition = DOTween.Sequence();
            transition.SetUpdate(true);

            if (validHighlight != null)
            {
                transition.Insert(
                    0f,
                    validHighlight
                        .DOFade(validAlpha, duration)
                        .SetEase(ease));
            }

            if (invalidHighlight != null)
            {
                transition.Insert(
                    0f,
                    invalidHighlight
                        .DOFade(invalidAlpha, duration)
                        .SetEase(ease));
            }

            transition.OnComplete(() => transition = null);
        }

        public void Clear(bool immediate = false)
        {
            SetState(PlacementFeedbackState.None, immediate);
        }

        #endregion

        #region Visual Helpers

        private void ConfigureHighlights()
        {
            ConfigureHighlight(validHighlight);
            ConfigureHighlight(invalidHighlight);
        }

        private void ConfigureHighlight(Image highlight)
        {
            if (highlight == null)
                return;

            highlight.raycastTarget = false;
        }

        private void BringHighlightToFront(Image highlight)
        {
            if (highlight == null)
                return;

            Transform highlightTransform = highlight.transform;

            if (highlightTransform.parent != transform)
                return;

            if (highlightTransform.GetSiblingIndex() == transform.childCount - 1)
                return;

            highlightTransform.SetAsLastSibling();
        }

        private void ApplyAlpha(Image highlight, float alpha)
        {
            if (highlight == null)
                return;

            Color color = highlight.color;
            color.a = alpha;
            highlight.color = color;
        }

        private void StopTransition()
        {
            transition?.Kill(false);
            transition = null;
        }

        #endregion

#if UNITY_EDITOR
        #region Inspector Test

        [ContextMenu("Test/Show Valid")]
        private void TestShowValid()
        {
            if (!Application.isPlaying)
                return;

            SetState(PlacementFeedbackState.Valid);
        }

        [ContextMenu("Test/Show Invalid")]
        private void TestShowInvalid()
        {
            if (!Application.isPlaying)
                return;

            SetState(PlacementFeedbackState.Invalid);
        }

        [ContextMenu("Test/Clear")]
        private void TestClear()
        {
            if (!Application.isPlaying)
                return;

            Clear();
        }

        [ContextMenu("Test/Clear Immediately")]
        private void TestClearImmediately()
        {
            if (!Application.isPlaying)
                return;

            Clear(immediate: true);
        }

        #endregion
#endif
    }
}