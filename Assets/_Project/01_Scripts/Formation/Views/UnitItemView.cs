using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace OzGameLab01.UI
{
    [DisallowMultipleComponent]
    public sealed class UnitItemView : MonoBehaviour,IPointerClickHandler,IPointerEnterHandler,IPointerExitHandler,IBeginDragHandler,IDragHandler,IEndDragHandler
    {
        [Header("References")]
        [SerializeField] private RectTransform rectTransform;
        [SerializeField] private Image unitIcon;
        [SerializeField] private Image selectionFrame;
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Drag Settings")]
        [SerializeField] private float draggingAlpha = 0.65f;

        private bool _isInteractable = true;
        private bool _isSelected;
        private bool _isDragging;

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

        private void OnDisable()
        {
            SetDragging(false);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (rectTransform == null)
            {
                rectTransform = transform as RectTransform;
            }

            if (unitIcon == null)
            {
                unitIcon = GetComponentInChildren<Image>(true);
            }

            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }
        }
#endif

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

            if (selectionFrame != null)
            {
                selectionFrame.gameObject.SetActive(value);
            }
        }

        public void SetInteractable(bool value)
        {
            _isInteractable = value;

            if (canvasGroup != null)
            {
                canvasGroup.interactable = value;
                canvasGroup.blocksRaycasts = value && !_isDragging;
            }
        }

        public void SetDragging(bool value)
        {
            _isDragging = value;

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

        #endregion
    }
}