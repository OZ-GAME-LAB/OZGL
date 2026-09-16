using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace OzGameLab01.UI
{
    public enum UnitSlotType
    {
        Battle,
        Support
    }

    [DisallowMultipleComponent]
    public sealed class UnitSlotItemView : MonoBehaviour,IPointerClickHandler,IPointerEnterHandler,IPointerExitHandler,IDropHandler
    {
        [Header("References")]
        [SerializeField] private PlacementSlotFeedbackView placementFeedback;
        [SerializeField] private RectTransform rectTransform;
        [SerializeField] private Image slotIcon;

        [Header("Settings")]
        [SerializeField] private int slotIndex;
        [SerializeField] private UnitSlotType slotType;

        private bool _isOccupied;
        private bool _isInteractable = true;

        #region Properties

        public RectTransform RectTransform => rectTransform != null ? rectTransform : transform as RectTransform;

        public Image SlotIcon => slotIcon;

        public int SlotIndex => slotIndex;
        public UnitSlotType SlotType => slotType;

        public bool IsBattleSlot => slotType == UnitSlotType.Battle;
        public bool IsSupportSlot => slotType == UnitSlotType.Support;

        public bool IsOccupied => _isOccupied;

        public bool IsInteractable
        {
            get => _isInteractable;
            set => SetInteractable(value);
        }

        public event Action<UnitSlotItemView, PointerEventData> Clicked;
        public event Action<UnitSlotItemView, PointerEventData> PointerEntered;
        public event Action<UnitSlotItemView, PointerEventData> PointerExited;
        public event Action<UnitSlotItemView, PointerEventData> Dropped;

        #endregion

        #region Lifecycle

        private void Awake()
        {
            ResolveReferences();
        }

        private void OnDisable()
        {
            ClearPlacementFeedback(immediate: true);
        }

        #endregion

        #region Initialization

        private void ResolveReferences()
        {
            if (rectTransform == null)
                rectTransform = transform as RectTransform;

            if (placementFeedback == null)
                placementFeedback = GetComponent<PlacementSlotFeedbackView>();
        }

        #endregion

        #region Public API

        public void SetPlacementFeedback(PlacementFeedbackState state,bool immediate = false)
        {
            ResolveReferences();

            if (placementFeedback == null)
                return;

            if (!isActiveAndEnabled || !isInteractable)
            {
                placementFeedback.Clear(immediate: true);
                return;
            }

            placementFeedback.SetState(state, immediate);
        }

        public void ClearPlacementFeedback(bool immediate = false)
        {
            ResolveReferences();

            if (placementFeedback != null)
                placementFeedback.Clear(immediate);
        }

        public void SetSlotIndex(int value)
        {
            slotIndex = Mathf.Max(0, value);
        }

        public void SetSlotType(UnitSlotType value)
        {
            slotType = value;
        }

        public void SetOccupied(bool value)
        {
            _isOccupied = value;
        }

        public void SetInteractable(bool value)
        {
            _isInteractable = value;

            if (slotIcon != null)
                slotIcon.raycastTarget = value;

            if (!value)
                ClearPlacementFeedback(immediate: true);
        }

        public void SetIcon(Sprite sprite)
        {
            if (slotIcon != null)
                slotIcon.sprite = sprite;
        }

        public void SetIconVisible(bool visible)
        {
            if (slotIcon != null)
                slotIcon.enabled = visible;
        }

        public void SetIconColor(Color color)
        {
            if (slotIcon != null)
                slotIcon.color = color;
        }

        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }

        #endregion

        #region Pointer Events

        public void OnPointerClick(PointerEventData eventData)
        {

            if (!isActiveAndEnabled || !_isInteractable)
            {
                return;
            }   

            Clicked?.Invoke(this, eventData);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {

            if (!isActiveAndEnabled || !_isInteractable)
               {    
                return;
               }
               
            PointerEntered?.Invoke(this, eventData);
        }

        public void OnPointerExit(PointerEventData eventData)
        {

            if (!isActiveAndEnabled || !_isInteractable)
               { 
                return;
               }
            PointerExited?.Invoke(this, eventData);
        }

        public void OnDrop(PointerEventData eventData)
        {

            if (!isActiveAndEnabled || !_isInteractable)
               {
                return;
               }
            Dropped?.Invoke(this, eventData);
        }

        #endregion

#if UNITY_EDITOR
        #region Editor Validation

        private void OnValidate()
        {
            ResolveReferences();
            slotIndex = Mathf.Max(0, slotIndex);
        }

        #endregion

        #region Inspector Test

        [ContextMenu("Test/Placement/Show Valid")]
        private void TestShowValid()
        {
            if (!Application.isPlaying)
                return;

            SetPlacementFeedback(PlacementFeedbackState.Valid);
        }

        [ContextMenu("Test/Placement/Show Invalid")]
        private void TestShowInvalid()
        {
            if (!Application.isPlaying)
                return;

            SetPlacementFeedback(PlacementFeedbackState.Invalid);
        }

        [ContextMenu("Test/Placement/Clear")]
        private void TestClear()
        {
            if (!Application.isPlaying)
                return;

            ClearPlacementFeedback();
        }

        [ContextMenu("Test/Placement/Clear Immediately")]
        private void TestClearImmediately()
        {
            if (!Application.isPlaying)
                return;

            ClearPlacementFeedback(immediate: true);
        }

        #endregion
#endif
    }
}