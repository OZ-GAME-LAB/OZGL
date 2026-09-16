using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace OzGameLab01.UI
{
    [DisallowMultipleComponent]
    public sealed class UnitItemView : MonoBehaviour, IPointerClickHandler, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [Header("References")]
        [SerializeField] private RectTransform rectTransform;
        [SerializeField] private RectTransform visualRoot;
        [SerializeField] private Image unitIcon;
        [SerializeField] private Image selectionFrame;
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Drag Settings")]
        [SerializeField] private float draggingAlpha = 0.65f;

        refactor/formation_system
        private bool _isInteractable = true;
        private bool _isSelected;
        private bool _isDragging;

 [Header("Press Feedback")]
        [SerializeField, Min(1f)] private float pressedScale = 1.08f;
        [SerializeField, Min(0f)] private float pressDuration = 0.08f;
        [SerializeField, Min(0f)] private float releaseDuration = 0.12f;
        [Header("Placement Feedback")]
        [SerializeField, Min(1f)] private float placementScale = 1.08f;
        [SerializeField, Min(0f)] private float placementGrowDuration = 0.08f;
        [SerializeField, Min(0f)] private float placementReturnDuration = 0.14f;
        [SerializeField] private Ease placementGrowEase = Ease.OutCubic;
        [SerializeField] private Ease placementReturnEase = Ease.OutSine;
        private Tween scaleTween;
        private Vector3 baseScale;
        private bool isPressed;
        private int pressedPointerId;
        private PointerEventData.InputButton pressedButton;
       
        private bool _isInteractable = true;
        private bool _isSelected;
        private bool _isDragging;
        refactor/architecture_refactor

        #region Properties

        public RectTransform RectTransform => rectTransform;
        public Image UnitIcon => unitIcon;
        public Image SelectionFrame => selectionFrame;
        public CanvasGroup CanvasGroup => canvasGroup;

        public bool IsInteractable
        {
            get => _isInteractable;
            set => SetInteractable(value);
        }

        public bool IsSelected
        {
            get => _isSelected;
            set => SetSelected(value);
        }

        public bool IsDragging => _isDragging;

        public event Action<UnitItemView, PointerEventData> Clicked; //유닛 아이템 클릭 이벤트
        public event Action<UnitItemView, PointerEventData> PointerEntered; //유닛 아이템 포인터 진입 이벤트
        public event Action<UnitItemView, PointerEventData> PointerExited; //유닛 아이템 포인터 이탈 이벤트
        public event Action<UnitItemView, PointerEventData> BeginDragged; //유닛 아이템 드래그 시작 이벤트
        public event Action<UnitItemView, PointerEventData> Dragged; //유닛 아이템 드래그 이벤트
        public event Action<UnitItemView, PointerEventData> EndDragged; //유닛 아이템 드래그 종료 이벤트

        #endregion

        #region Lifecycle

        private void Awake()
        {
            ResolveReferences();
            baseScale = visualRoot != null ? visualRoot.localScale : Vector3.one;
            if (selectionFrame != null)
                selectionFrame.raycastTarget = false;
        }

        private void OnEnable()
        {
            ResetPressFeedback();
        }

        private void OnDisable()
        {
            ResetPressFeedback();
            SetDragging(false);
        }

        private void OnDestroy()
        {
            StopScaleTween();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            ResolveReferences();
        }
#endif

        private void ResolveReferences()
        {
            if (rectTransform == null)
            {
                rectTransform = transform as RectTransform;
            }

            if (visualRoot == null)
            {
                visualRoot = transform.Find("VisualRoot") as RectTransform;
            }

            Transform iconTransform = transform.Find("VisualRoot/UnitIcon") ?? transform.Find("UnitIcon");
            if (iconTransform != null)
                unitIcon = iconTransform.GetComponent<Image>();

            if (selectionFrame == null)
            {
                Transform highlight = transform.Find("VisualRoot/Highlight");
                if (highlight != null)
                    selectionFrame = highlight.GetComponent<Image>();
            }

            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }
        }

        #endregion

        #region API

        public void SetIcon(Sprite sprite)
        {
            if (unitIcon != null)
            {
                unitIcon.sprite = sprite;
            }
        }

        public void SetIconColor(Color color)
        {
            if (unitIcon != null)
            {
                unitIcon.color = color;
            }
        }

        public void SetIconVisible(bool visible)
        {
            if (unitIcon != null)
            {
                unitIcon.enabled = visible;
            }
        }

        public void SetSelected(bool value)
        {
            _isSelected = value;

            RefreshHighlight();
        }

        public void SetInteractable(bool value)
        {

            _isInteractable = value;
            if (!value)
                ResetPressFeedback();

            if (canvasGroup != null)
            {
                canvasGroup.interactable = value;
                canvasGroup.blocksRaycasts = value && !_isDragging;
            }
        }

        public void SetDragging(bool value)
        {

            _isDragging = value;

            if (value)
                ResetPressFeedback();


            if (canvasGroup != null)
            {
                canvasGroup.alpha = value ? draggingAlpha : 1f;
                canvasGroup.blocksRaycasts = _isInteractable && !value;
            }
        }

        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!_isInteractable)
            {
                return;
            }

            Clicked?.Invoke(this, eventData);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!isActiveAndEnabled || !isInteractable || isDragging || isPressed || (eventData.button != PointerEventData.InputButton.Left && eventData.button != PointerEventData.InputButton.Right))
                return;

            isPressed = true;
            pressedPointerId = eventData.pointerId;
            pressedButton = eventData.button;
            RefreshHighlight();
            AnimateScale(baseScale * pressedScale, pressDuration);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!isPressed || eventData.pointerId != pressedPointerId || eventData.button != pressedButton)
                return;

            ReleasePress();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!_isInteractable)
            {
                return;
            }

            PointerEntered?.Invoke(this, eventData);
        }

        public void OnPointerExit(PointerEventData eventData)
        {

            if (isPressed && eventData.pointerId == pressedPointerId)
                ReleasePress();

            if (!_isInteractable)

            {
                return;
            }

            PointerExited?.Invoke(this, eventData);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!_isInteractable)
            {
                return;
            }

            SetDragging(true);
            BeginDragged?.Invoke(this, eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_isInteractable)
            {
                return;
            }

            Dragged?.Invoke(this, eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!_isDragging)
            {
                return;
            }

            SetDragging(false);
            EndDragged?.Invoke(this, eventData);
        }

        /// <summary>
        /// 배치 성공 시 외부에서 호출합니다.
        /// 슬롯으로 이동하고 드래그 상태를 정리한 다음 호출하세요.
        /// </summary>
        public void PlayPlacementFeedback()
        {
            if (!isActiveAndEnabled || visualRoot == null || isDragging)
                return;

            ResetPressFeedback();

            if (placementGrowDuration <= 0f && placementReturnDuration <= 0f)
                return;

            Vector3 expandedScale = baseScale * placementScale;

            Sequence placementSequence = DOTween.Sequence();
            placementSequence.SetUpdate(true);

            scaleTween = placementSequence;

            if (placementGrowDuration > 0f)
            {
                placementSequence.Append(
                    visualRoot
                        .DOScale(expandedScale, placementGrowDuration)
                        .SetEase(placementGrowEase));
            }
            else
            {
                visualRoot.localScale = expandedScale;
            }

            if (placementReturnDuration > 0f)
            {
                placementSequence.Append(
                    visualRoot
                        .DOScale(baseScale, placementReturnDuration)
                        .SetEase(placementReturnEase));
            }

            placementSequence.OnComplete(() =>
            {
                visualRoot.localScale = baseScale;
                scaleTween = null;
            });
        }

        /// <summary>
        /// 누름·배치 연출을 중단하고 기본 크기로 즉시 복원합니다.
        /// </summary>
        public void ResetVisualFeedback()
        {
            ResetPressFeedback();
        }

        #endregion

        #region Press Feedback

        private void RefreshHighlight()
        {
            if (selectionFrame != null)
                selectionFrame.gameObject.SetActive(isSelected || isPressed);
        }

        private void ReleasePress()
        {
            isPressed = false;
            RefreshHighlight();
            AnimateScale(baseScale, releaseDuration);
        }

        private void AnimateScale(Vector3 target, float duration)
        {
            StopScaleTween();
            if (visualRoot == null)
                return;

            if (duration <= 0f)
            {
                visualRoot.localScale = target;
                return;
            }

            scaleTween = visualRoot.DOScale(target, duration)
                .SetEase(Ease.OutCubic)
                .SetUpdate(true);
        }

        private void ResetPressFeedback()
        {
            StopScaleTween();
            isPressed = false;
            if (visualRoot != null)
                visualRoot.localScale = baseScale;
            RefreshHighlight();
        }

        private void StopScaleTween()
        {
            scaleTween?.Kill(false);
            scaleTween = null;
        }

        #endregion

#if UNITY_EDITOR
        #region Placement Inspector Test

        [ContextMenu("Test/Placement/Play Feedback")]
        private void TestPlayPlacementFeedback()
        {
            if (!Application.isPlaying)
                return;

            PlayPlacementFeedback();
        }

        [ContextMenu("Test/Placement/Reset Feedback")]
        private void TestResetPlacementFeedback()
        {
            if (!Application.isPlaying)
                return;

            ResetVisualFeedback();
        }

        #endregion
#endif
    }
}
