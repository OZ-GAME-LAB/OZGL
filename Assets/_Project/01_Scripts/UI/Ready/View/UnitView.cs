using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace OzGameLab01.UI
{
    [DisallowMultipleComponent]
    public sealed class UnitView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Button closeButton;
        [SerializeField] private Transform unitContentRoot;
        [SerializeField] private Transform synergyContentRoot;
        [SerializeField] private Transform slotContentRoot;

        private readonly List<UnitItemView> unitItems = new List<UnitItemView>();
        private readonly List<UnitSlotItemView> slotItems = new List<UnitSlotItemView>();
        private readonly List<SynergyItemView> synergyItems = new List<SynergyItemView>();

        private bool isListening;

        #region Properties

        public Button CloseButton => closeButton;

        public Transform UnitContentRoot => unitContentRoot;
        public Transform SynergyContentRoot => synergyContentRoot;
        public Transform SlotContentRoot => slotContentRoot;

        public IReadOnlyList<UnitItemView> UnitItems => unitItems;
        public IReadOnlyList<UnitSlotItemView> SlotItems => slotItems;
        public IReadOnlyList<SynergyItemView> SynergyItems => synergyItems;

        public bool IsVisible => gameObject.activeSelf;

        public event Action<UnitView> CloseClicked;

        public event Action<UnitItemView, PointerEventData> UnitClicked; //유닛 아이템 클릭 이벤트
        public event Action<UnitItemView, PointerEventData> UnitPointerEntered; //유닛 아이템 포인터 진입 이벤트
        public event Action<UnitItemView, PointerEventData> UnitPointerExited; //유닛 아이템 포인터 이탈 이벤트
        public event Action<UnitItemView, PointerEventData> UnitBeginDragged; //유닛 아이템 드래그 시작 이벤트
        public event Action<UnitItemView, PointerEventData> UnitDragged; //유닛 아이템 드래그 이벤트
        public event Action<UnitItemView, PointerEventData> UnitEndDragged; //유닛 아이템 드래그 종료 이벤트

        public event Action<UnitSlotItemView, PointerEventData> SlotClicked; //슬롯 아이템 클릭 이벤트
        public event Action<UnitSlotItemView, PointerEventData> SlotPointerEntered; //슬롯 아이템 포인터 진입 이벤트
        public event Action<UnitSlotItemView, PointerEventData> SlotPointerExited; //슬롯 아이템 포인터 이탈 이벤트
        public event Action<UnitSlotItemView, PointerEventData> SlotDropped; //슬롯 아이템 드롭 이벤트

        public event Action<SynergyItemView, PointerEventData> SynergyClicked; //시너지 아이템 클릭 이벤트
        public event Action<SynergyItemView, PointerEventData> SynergyPointerEntered; //시너지 아이템 포인터 진입 이벤트
        public event Action<SynergyItemView, PointerEventData> SynergyPointerExited; //시너지 아이템 포인터 이탈 이벤트

        #endregion

        #region Lifecycle

        private void Awake()
        {
            RefreshItems();
        }

        private void OnEnable()
        {
            isListening = true;

            SubscribeCloseButton();
            SubscribeItems();
        }

        private void OnDisable()
        {
            UnsubscribeCloseButton();
            UnsubscribeItems();

            isListening = false;
        }

        #endregion

        #region API

        public void Show()
        {
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        // 새 Unit_Item을 UnitContentRoot에 추가한 후 호출합니다.
        public void RefreshUnitItems()
        {
            if (isListening)
            {
                UnsubscribeUnitItems();
            }

            unitItems.Clear();

            if (unitContentRoot != null)
            {
                unitItems.AddRange(
                    unitContentRoot.GetComponentsInChildren<UnitItemView>(true));
            }

            if (isListening)
            {
                SubscribeUnitItems();
            }
        }

        // 슬롯 구조를 수정했을 때 호출합니다.
        public void RefreshSlotItems()
        {
            if (isListening)
            {
                UnsubscribeSlotItems();
            }

            slotItems.Clear();

            if (slotContentRoot != null)
            {
                slotItems.AddRange(
                    slotContentRoot.GetComponentsInChildren<UnitSlotItemView>(true));
            }

            if (isListening)
            {
                SubscribeSlotItems();
            }
        }

        public void RefreshItems()
        {
            RefreshUnitItems();
            RefreshSynergyItems();
            RefreshSlotItems();
        }

        // 런타임에 Unit_Item을 생성한 직후 사용할 수 있습니다.
        public void RegisterUnitItem(UnitItemView item)
        {
            if (item == null || unitItems.Contains(item))
            {
                return;
            }

            unitItems.Add(item);

            if (isListening)
            {
                SubscribeUnitItem(item);
            }
        }

        public void UnregisterUnitItem(UnitItemView item)
        {
            if (item == null || !unitItems.Remove(item))
            {
                return;
            }

            UnsubscribeUnitItem(item);
        }

        public void RegisterSlotItem(UnitSlotItemView item)
        {
            if (item == null || slotItems.Contains(item))
            {
                return;
            }

            slotItems.Add(item);

            if (isListening)
            {
                SubscribeSlotItem(item);
            }
        }

        public void UnregisterSlotItem(UnitSlotItemView item)
        {
            if (item == null || !slotItems.Remove(item))
            {
                return;
            }

            UnsubscribeSlotItem(item);
        }

        public bool TryGetSlotItem(int slotIndex, out UnitSlotItemView slotItem)
        {
            foreach (UnitSlotItemView item in slotItems)
            {
                if (item != null && item.SlotIndex == slotIndex)
                {
                    slotItem = item;
                    return true;
                }
            }

            slotItem = null;
            return false;
        }

        public void ClearUnitSelection()
        {
            foreach (UnitItemView item in unitItems)
            {
                if (item != null)
                {
                    item.SetSelected(false);
                }
            }
        }

        public void SetSlotsInteractable(bool interactable)
        {
            foreach (UnitSlotItemView item in slotItems)
            {
                if (item != null)
                {
                    item.SetInteractable(interactable);
                }
            }
        }

        public void RefreshSynergyItems()
        {
            if (isListening)
            {
                UnsubscribeSynergyItems();
            }

            synergyItems.Clear();

            if (synergyContentRoot != null)
            {
                synergyItems.AddRange(
                    synergyContentRoot.GetComponentsInChildren<SynergyItemView>(true));
            }

            if (isListening)
            {
                SubscribeSynergyItems();
            }
        }

        public void RegisterSynergyItem(SynergyItemView item)
        {
            if (item == null || synergyItems.Contains(item))
            {
                return;
            }

            synergyItems.Add(item);

            if (isListening)
            {
                SubscribeSynergyItem(item);
            }
        }

        public void UnregisterSynergyItem(SynergyItemView item)
        {
            if (item == null || !synergyItems.Remove(item))
            {
                return;
            }

            UnsubscribeSynergyItem(item);
        }

        #endregion

        #region Private Methods

        private void SubscribeCloseButton()
        {
            if (closeButton != null)
            {
                closeButton.onClick.AddListener(HandleCloseClick);
            }
        }

        private void UnsubscribeCloseButton()
        {
            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(HandleCloseClick);
            }
        }

        private void SubscribeItems()
        {
            SubscribeUnitItems();
            SubscribeSynergyItems();
            SubscribeSlotItems();
        }

        private void UnsubscribeItems()
        {
            UnsubscribeUnitItems();
            UnsubscribeSynergyItems();
            UnsubscribeSlotItems();
        }

        private void SubscribeUnitItems()
        {
            foreach (UnitItemView item in unitItems)
            {
                SubscribeUnitItem(item);
            }
        }

        private void UnsubscribeUnitItems()
        {
            foreach (UnitItemView item in unitItems)
            {
                UnsubscribeUnitItem(item);
            }
        }

        private void SubscribeSlotItems()
        {
            foreach (UnitSlotItemView item in slotItems)
            {
                SubscribeSlotItem(item);
            }
        }

        private void UnsubscribeSlotItems()
        {
            foreach (UnitSlotItemView item in slotItems)
            {
                UnsubscribeSlotItem(item);
            }
        }

        private void SubscribeUnitItem(UnitItemView item)
        {
            if (item == null)
            {
                return;
            }

            item.Clicked += HandleUnitClick;
            item.PointerEntered += HandleUnitPointerEnter;
            item.PointerExited += HandleUnitPointerExit;
            item.BeginDragged += HandleUnitBeginDrag;
            item.Dragged += HandleUnitDrag;
            item.EndDragged += HandleUnitEndDrag;
        }

        private void UnsubscribeUnitItem(UnitItemView item)
        {
            if (item == null)
            {
                return;
            }

            item.Clicked -= HandleUnitClick;
            item.PointerEntered -= HandleUnitPointerEnter;
            item.PointerExited -= HandleUnitPointerExit;
            item.BeginDragged -= HandleUnitBeginDrag;
            item.Dragged -= HandleUnitDrag;
            item.EndDragged -= HandleUnitEndDrag;
        }

        private void SubscribeSlotItem(UnitSlotItemView item)
        {
            if (item == null)
            {
                return;
            }

            item.Clicked += HandleSlotClick;
            item.PointerEntered += HandleSlotPointerEnter;
            item.PointerExited += HandleSlotPointerExit;
            item.Dropped += HandleSlotDrop;
        }

        private void UnsubscribeSlotItem(UnitSlotItemView item)
        {
            if (item == null)
            {
                return;
            }

            item.Clicked -= HandleSlotClick;
            item.PointerEntered -= HandleSlotPointerEnter;
            item.PointerExited -= HandleSlotPointerExit;
            item.Dropped -= HandleSlotDrop;
        }

        private void SubscribeSynergyItems()
        {
            foreach (SynergyItemView item in synergyItems)
            {
                SubscribeSynergyItem(item);
            }
        }

        private void UnsubscribeSynergyItems()
        {
            foreach (SynergyItemView item in synergyItems)
            {
                UnsubscribeSynergyItem(item);
            }
        }

        private void SubscribeSynergyItem(SynergyItemView item)
        {
            if (item == null)
            {
                return;
            }

            item.Clicked += HandleSynergyClick;
            item.PointerEntered += HandleSynergyPointerEnter;
            item.PointerExited += HandleSynergyPointerExit;
        }

        private void UnsubscribeSynergyItem(SynergyItemView item)
        {
            if (item == null)
            {
                return;
            }

            item.Clicked -= HandleSynergyClick;
            item.PointerEntered -= HandleSynergyPointerEnter;
            item.PointerExited -= HandleSynergyPointerExit;
        }

        private void HandleSynergyClick(SynergyItemView item, PointerEventData eventData)
        {
            SynergyClicked?.Invoke(item, eventData);
        }

        private void HandleSynergyPointerEnter(SynergyItemView item, PointerEventData eventData)
        {
            SynergyPointerEntered?.Invoke(item, eventData);
        }

        private void HandleSynergyPointerExit(SynergyItemView item, PointerEventData eventData)
        {
            SynergyPointerExited?.Invoke(item, eventData);
        }

        private void HandleCloseClick()
        {
            CloseClicked?.Invoke(this);
        }

        private void HandleUnitClick(UnitItemView item, PointerEventData eventData)
        {
            UnitClicked?.Invoke(item, eventData);
        }

        private void HandleUnitPointerEnter(UnitItemView item, PointerEventData eventData)
        {
            UnitPointerEntered?.Invoke(item, eventData);
        }

        private void HandleUnitPointerExit(UnitItemView item, PointerEventData eventData)
        {
            UnitPointerExited?.Invoke(item, eventData);
        }

        private void HandleUnitBeginDrag(UnitItemView item, PointerEventData eventData)
        {
            UnitBeginDragged?.Invoke(item, eventData);
        }

        private void HandleUnitDrag(UnitItemView item, PointerEventData eventData)
        {
            UnitDragged?.Invoke(item, eventData);
        }

        private void HandleUnitEndDrag(UnitItemView item, PointerEventData eventData)
        {
            UnitEndDragged?.Invoke(item, eventData);
        }

        private void HandleSlotClick(UnitSlotItemView item, PointerEventData eventData)
        {
            SlotClicked?.Invoke(item, eventData);
        }

        private void HandleSlotPointerEnter(UnitSlotItemView item, PointerEventData eventData)
        {
            SlotPointerEntered?.Invoke(item, eventData);
        }

        private void HandleSlotPointerExit(UnitSlotItemView item, PointerEventData eventData)
        {
            SlotPointerExited?.Invoke(item, eventData);
        }

        private void HandleSlotDrop(UnitSlotItemView item, PointerEventData eventData)
        {
            SlotDropped?.Invoke(item, eventData);
        }

        #endregion
    }
}