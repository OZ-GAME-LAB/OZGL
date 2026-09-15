using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace OzGameLab01.UI
{
    [DisallowMultipleComponent]
    public sealed class EndTurnButtonFeedbackView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform attentionVisual;
        [SerializeField] private Graphic attentionGraphic;
        [SerializeField] private CanvasGroup highlight;
        [SerializeField] private Image iconImage;

        [Header("Attention")]
        [SerializeField, Min(1f)] private float attentionScale = 1.08f;
        [SerializeField] private Color attentionColor = new (1f, 0.65f, 0.2f, 1f);
        [SerializeField, Range(0f, 1f)] private float highlightAlpha = 1f;

        [Header("Tween")]
        [SerializeField, Min(0f)] private float transitionDuration = 0.25f;
        [SerializeField] private Ease transitionEase = Ease.OutCubic;

        [Header("Icon Rotation")]
        [SerializeField, Min(0.1f)] private float iconRotationDuration = 8f;

        private Vector3 normalScale;
        private Color normalColor;
        private Quaternion normalIconRotation;

        private Sequence transitionSequence;
        private Tween iconRotationTween;
        private bool initialized;

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
            StopIconRotation();

            if (initialized)
            {
                ApplyVisuals(normalScale, normalColor, 0f);
                ResetIconRotation();
            }
        }

        private void OnDestroy()
        {
            StopTransition();
            StopIconRotation();
        }

        #endregion

        #region Initialization

        private void Initialize()
        {
            if (initialized)
                return;

            normalScale = attentionVisual != null ? attentionVisual.localScale : Vector3.one;
            normalColor = attentionGraphic != null ? attentionGraphic.color : Color.white;
            normalIconRotation = iconImage != null ? iconImage.rectTransform.localRotation : Quaternion.identity;

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

            if (IsAttentionActive == active && !immediate)
                return;

            IsAttentionActive = active;
            RefreshVisuals(immediate);
        }

        #endregion

        #region Attention Animation

        private void RefreshVisuals(bool immediate)
        {
            StopTransition();
            StopIconRotation();

            if (!isActiveAndEnabled)
                return;

            Vector3 targetScale = normalScale * (IsAttentionActive ? attentionScale : 1f);
            Color targetColor = IsAttentionActive ? attentionColor : normalColor;
            float targetAlpha = IsAttentionActive ? highlightAlpha : 0f;

            RefreshIconRotation(immediate);

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

        #region Icon Rotation

        private void RefreshIconRotation(bool immediate)
        {
            if (iconImage == null)
                return;

            RectTransform iconTransform = iconImage.rectTransform;

            if (IsAttentionActive)
            {
                iconRotationTween = iconTransform
                    .DOLocalRotate(
                        new Vector3(0f, 0f, -360f),
                        Mathf.Max(0.1f, iconRotationDuration),
                        RotateMode.LocalAxisAdd)
                    .SetEase(Ease.Linear)
                    .SetLoops(-1, LoopType.Restart)
                    .SetUpdate(true);

                return;
            }

            if (immediate || transitionDuration <= 0f)
            {
                ResetIconRotation();
                return;
            }

            iconRotationTween = iconTransform
                .DOLocalRotateQuaternion(
                    normalIconRotation,
                    transitionDuration)
                .SetEase(transitionEase)
                .SetUpdate(true);
        }

        private void ResetIconRotation()
        {
            if (iconImage != null)
                iconImage.rectTransform.localRotation = normalIconRotation;
        }

        private void StopIconRotation()
        {
            if (iconRotationTween == null)
                return;

            iconRotationTween.Kill(false);
            iconRotationTween = null;
        }

        #endregion

        #region Inspector Test

#if UNITY_EDITOR
        [ContextMenu("Preview/Attention On")]
        private void PreviewAttentionOn()
        {
            if (Application.isPlaying)
                SetAttention(true);
        }

        [ContextMenu("Preview/Attention Off")]
        private void PreviewAttentionOff()
        {
            if (Application.isPlaying)
                SetAttention(false);
        }
#endif

        #endregion
    }
}