using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace OzGameLab01.UI
{
    [DisallowMultipleComponent]
    public sealed class UnitSlotItemView : MonoBehaviour,
        IPointerClickHandler,
        IPointerEnterHandler,
        IPointerExitHandler,
        IDropHandler
    {
        [Header("References")]
        [SerializeField] private RectTransform rectTransform;
        [SerializeField] private Image slotIcon;

        [Header("Settings")]
        [SerializeField] private int slotIndex;

        private bool isOccupied;
        private bool isInteractable = true;

        #region Properties

        public RectTransform RectTransform => rectTransform;
        public Image SlotIcon => slotIcon;

        public int SlotIndex => slotIndex;
        public bool IsOccupied => isOccupied;

        public bool IsInteractable
        {
            get => isInteractable;
            set => SetInteractable(value);
        }

        public event Action<UnitSlotItemView, PointerEventData> Clicked; //슬롯 아이템 클릭 이벤트
        public event Action<UnitSlotItemView, PointerEventData> PointerEntered; //슬롯 아이템 포인터 진입 이벤트
        public event Action<UnitSlotItemView, PointerEventData> PointerExited; //슬롯 아이템 포인터 이탈 이벤트
        public event Action<UnitSlotItemView, PointerEventData> Dropped; //슬롯 아이템 드롭 이벤트

        #endregion

        #region API

        public void SetSlotIndex(int value)
        {
            slotIndex = Mathf.Max(0, value);
        }

        public void SetOccupied(bool value)
        {
            isOccupied = value;
        }

        public void SetInteractable(bool value)
        {
            isInteractable = value;

            if (slotIcon != null)
            {
                slotIcon.raycastTarget = value;
            }
        }

        public void SetIcon(Sprite sprite)
        {
            if (slotIcon != null)
            {
                slotIcon.sprite = sprite;
            }
        }

        public void SetIconVisible(bool visible)
        {
            if (slotIcon != null)
            {
                slotIcon.enabled = visible;
            }
        }

        public void SetIconColor(Color color)
        {
            if (slotIcon != null)
            {
                slotIcon.color = color;
            }
        }

        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!isInteractable)
            {
                return;
            }

            Clicked?.Invoke(this, eventData);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!isInteractable)
            {
                return;
            }

            PointerEntered?.Invoke(this, eventData);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!isInteractable)
            {
                return;
            }

            PointerExited?.Invoke(this, eventData);
        }

        public void OnDrop(PointerEventData eventData)
        {
            if (!isInteractable)
            {
                return;
            }

            Dropped?.Invoke(this, eventData);
        }

        #endregion
    }
}