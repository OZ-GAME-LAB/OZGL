using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OzGameLab01.UI
{
    [DisallowMultipleComponent]
    public sealed class EndTurnButtonFeedbackView : MonoBehaviour
    {
        public enum TurnActionPointState
        {
            Waiting,       // Before rolling: show the icon without a highlight.
            Unused,
            PartiallyUsed,
            Depleted
        }

        [Header("References")]
        [SerializeField] private Transform attentionVisual;
        [SerializeField] private Graphic attentionGraphic;
        [SerializeField] private CanvasGroup highlight;
        [SerializeField] private Image iconImage;

        [SerializeField] private TMP_Text actionPointText;

        [SerializeField] private Color unusedColor = new Color(0.65f, 1f, 0.25f, 1f);
        [SerializeField] private Color partiallyUsedColor = new Color(1f, 0.65f, 0.2f, 1f);
        [SerializeField] private Color depletedColor = new Color(1f, 0.2f, 0.2f, 1f);

        [Header("Attention")]
        [SerializeField, Min(1f)] private float attentionScale = 1.08f;
        [SerializeField] private Color attentionColor = new (1f, 0.65f, 0.2f, 1f);
        [SerializeField, Range(0f, 1f)] private float highlightAlpha = 1f;

        [Header("Tween")]
        [SerializeField, Min(0f)] private float transitionDuration = 0.25f;
        [SerializeField] private Ease transitionEase = Ease.OutCubic;

        private Vector3 normalScale;
        private Color normalColor;

        private Sequence transitionSequence;
        private bool initialized;
        private bool hasActionPointState;

        public TurnActionPointState ActionPointState { get; private set; } = TurnActionPointState.Waiting;
        public int RemainingPoints { get; private set; }

        #region Properties

        public bool IsAttentionActive { get; private set; }

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            Initialize();
        }

        private void OnEnable()
        {
            Initialize();
            RefreshVisuals(true);
        }

        private void OnDisable()
        {
            StopTransition();

            if (initialized)
            {
                ApplyVisuals(normalScale, normalColor, 0f);
            }
        }

        private void OnDestroy()
        {
            StopTransition();
        }

        #endregion

        #region Initialization

        private void Initialize()
        {
            if (initialized)
                return;

            normalScale = attentionVisual != null ? attentionVisual.localScale : Vector3.one;
            normalColor = attentionGraphic != null ? attentionGraphic.color : Color.white;

            if (highlight != null)
            {
                highlight.interactable = false;
                highlight.blocksRaycasts = false;
            }

            initialized = true;
        }

        #endregion

        #region Public API

        /// <summary>
        /// 턴 종료 안내 연출을 켜거나 끕니다.
        /// 게임 상태 판단과 버튼 클릭 가능 여부는 변경하지 않습니다.
        /// </summary>
        public void SetAttention(bool active, bool immediate = false)
        {
            Initialize();

            // Once explicit states are supplied, legacy calls cannot overwrite them.
            if (hasActionPointState)
                return;

            if (IsAttentionActive == active && !immediate)
                return;

            IsAttentionActive = active;
            RefreshVisuals(immediate);
        }

        public void SetActionPointState(TurnActionPointState state, int remainingPoints, bool immediate = false)
        {
            Initialize();
            if ((int)state < (int)TurnActionPointState.Waiting || (int)state > (int)TurnActionPointState.Depleted)
                state = TurnActionPointState.Waiting;

            hasActionPointState = true;
            ActionPointState = state;
            RemainingPoints = Mathf.Max(0, remainingPoints);
            IsAttentionActive = state != TurnActionPointState.Waiting;
            RefreshVisuals(immediate);
        }

        #endregion

        #region Attention Animation

        private void RefreshVisuals(bool immediate)
        {
            StopTransition();

            if (!isActiveAndEnabled)
                return;

            Vector3 targetScale = normalScale * (IsAttentionActive ? attentionScale : 1f);
            Color targetColor = IsAttentionActive ? attentionColor : normalColor;
            float targetAlpha = IsAttentionActive ? highlightAlpha : 0f;

            if (hasActionPointState)
            {
                switch (ActionPointState)
                {
                    case TurnActionPointState.Unused: targetColor = unusedColor; break;
                    case TurnActionPointState.PartiallyUsed: targetColor = partiallyUsedColor; break;
                    case TurnActionPointState.Depleted: targetColor = depletedColor; break;
                    default: targetColor = normalColor; break;
                }
            }

            bool showNumber = hasActionPointState &&
                (ActionPointState == TurnActionPointState.Unused ||
                 ActionPointState == TurnActionPointState.PartiallyUsed);
            if (actionPointText != null)
            {
                actionPointText.text = RemainingPoints.ToString();
                actionPointText.gameObject.SetActive(showNumber);
            }
            if (iconImage != null)
                iconImage.gameObject.SetActive(!showNumber);

            if (immediate || transitionDuration <= 0f)
            {
                ApplyVisuals(targetScale, targetColor, targetAlpha);
                return;
            }

            if (attentionVisual == null && attentionGraphic == null && highlight == null)
            {
                return;
            }

            transitionSequence = DOTween.Sequence();
            transitionSequence.SetUpdate(true);

            if (attentionVisual != null)
            {
                transitionSequence.Insert(0f, attentionVisual.DOScale(targetScale, transitionDuration).SetEase(transitionEase));
            }

            if (attentionGraphic != null)
            {
                transitionSequence.Insert(0f,attentionGraphic.DOColor(targetColor, transitionDuration).SetEase(transitionEase));
            }

            if (highlight != null)
            {
                transitionSequence.Insert(0f,highlight.DOFade(targetAlpha, transitionDuration).SetEase(transitionEase));
            }
        }

        private void ApplyVisuals(Vector3 scale, Color color, float alpha)
        {
            if (attentionVisual != null)
                attentionVisual.localScale = scale;

            if (attentionGraphic != null)
                attentionGraphic.color = color;

            if (highlight != null)
                highlight.alpha = alpha;
        }

        private void StopTransition()
        {
            if (transitionSequence == null)
                return;

            transitionSequence.Kill(false);
            transitionSequence = null;
        }

        #endregion

        #region Inspector Test

#if UNITY_EDITOR
        [ContextMenu("Preview/Waiting")]
        private void PreviewWaiting()
        {
            if (Application.isPlaying)
                SetActionPointState(TurnActionPointState.Waiting, 0);
        }

        [ContextMenu("Preview/Unused")]
        private void PreviewUnused() { if (Application.isPlaying) SetActionPointState(TurnActionPointState.Unused, 6); }

        [ContextMenu("Preview/Partially Used")]
        private void PreviewPartiallyUsed() { if (Application.isPlaying) SetActionPointState(TurnActionPointState.PartiallyUsed, 3); }

        [ContextMenu("Preview/Depleted")]
        private void PreviewDepleted() { if (Application.isPlaying) SetActionPointState(TurnActionPointState.Depleted, 0); }
#endif

        #endregion
    }
}
