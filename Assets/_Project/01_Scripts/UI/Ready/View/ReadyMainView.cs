using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace OzGameLab01.UI
{
    [DisallowMultipleComponent]
    public sealed class ReadyMainView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Button unitButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Transform synergyContentRoot;
        [SerializeField] private Transform artifactContentRoot;

        [SerializeField] private Button endTurnButton;
        [SerializeField] private RectTransform clockHand;
        [SerializeField] private TMP_Text currentTurnValueText;
        [SerializeField] private TMP_Text bossRemainingTurnValueText;

        private readonly List<SynergyItemView> synergyItems = new List<SynergyItemView>();
        private readonly List<ArtifactInfoItemView> artifactItems = new List<ArtifactInfoItemView>();

        private bool isListening;

        #region Properties

        public Button UnitButton => unitButton;
        public Button SettingsButton => settingsButton;

        public Transform SynergyContentRoot => synergyContentRoot;
        public Transform ArtifactContentRoot => artifactContentRoot;

        public IReadOnlyList<SynergyItemView> SynergyItems => synergyItems;
        public IReadOnlyList<ArtifactInfoItemView> ArtifactItems => artifactItems;

        public bool IsVisible => gameObject.activeSelf;

        public event Action<ReadyMainView> UnitClicked; //유닛 버튼 클릭 이벤트
        public event Action<ReadyMainView> SettingsClicked; //설정 버튼 클릭 이벤트
        public event Action<ReadyMainView> EndTurnClicked; //턴 종료 버튼 클릭 이벤트

        public event Action<SynergyItemView, PointerEventData> SynergyClicked; //시너지 아이템 클릭 이벤트
        public event Action<SynergyItemView, PointerEventData> SynergyPointerEntered; //시너지 아이템 포인터 진입 이벤트
        public event Action<SynergyItemView, PointerEventData> SynergyPointerExited; //시너지 아이템 포인터 이탈 이벤트

        public event Action<ArtifactInfoItemView, PointerEventData> ArtifactClicked; //아이템 클릭 이벤트
        public event Action<ArtifactInfoItemView, PointerEventData> ArtifactPointerEntered; //아이템 포인터 진입 이벤트
        public event Action<ArtifactInfoItemView, PointerEventData> ArtifactPointerExited; //아이템 포인터 이탈 이벤트


        #endregion

        #region Lifecycle

        private void Awake()
        {
            RefreshItems();
        }

        private void OnEnable()
        {
            isListening = true;

            SubscribeButtons();
            SubscribeItems();
        }

        private void OnDisable()
        {
            UnsubscribeButtons();
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

        public void SetUnitButtonInteractable(bool value)
        {
            if (unitButton != null)
            {
                unitButton.interactable = value;
            }
        }

        public void SetSettingsButtonInteractable(bool value)
        {
            if (settingsButton != null)
            {
                settingsButton.interactable = value;
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

        public void RefreshArtifactItems()
        {
            if (isListening)
            {
                UnsubscribeArtifactItems();
            }

            artifactItems.Clear();

            if (artifactContentRoot != null)
            {
                artifactItems.AddRange(
                    artifactContentRoot.GetComponentsInChildren<ArtifactInfoItemView>(true));
            }

            if (isListening)
            {
                SubscribeArtifactItems();
            }
        }

        public void RefreshItems()
        {
            RefreshSynergyItems();
            RefreshArtifactItems();
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

        public void RegisterArtifactItem(ArtifactInfoItemView item)
        {
            if (item == null || artifactItems.Contains(item))
            {
                return;
            }

            artifactItems.Add(item);

            if (isListening)
            {
                SubscribeArtifactItem(item);
            }
        }

        public void UnregisterArtifactItem(ArtifactInfoItemView item)
        {
            if (item == null || !artifactItems.Remove(item))
            {
                return;
            }

            UnsubscribeArtifactItem(item);
        }

        public void SetEndTurnInteractable(bool value)
        {
            if (endTurnButton != null)
            {
                endTurnButton.interactable = value;
            }
        }

        public void SetCurrentTurn(int turn)
        {
            if (currentTurnValueText != null)
            {
                currentTurnValueText.text = turn.ToString();
            }
        }

        public void SetBossRemainingTurn(int remainingTurns)
        {
            if (bossRemainingTurnValueText != null)
            {
                bossRemainingTurnValueText.text = remainingTurns.ToString();
            }
        }

        public void SetClockHandAngle(float angle)
        {
            if (clockHand != null)
            {
                clockHand.localEulerAngles = new Vector3(0f, 0f, angle);
            }
        }

        #endregion

        #region Private Methods

        private void SubscribeButtons()
        {
            if (unitButton != null)
            {
                unitButton.onClick.AddListener(HandleUnitClick);
            }

            if (settingsButton != null)
            {
                settingsButton.onClick.AddListener(HandleSettingsClick);
            }

            if (endTurnButton != null)
            {
                endTurnButton.onClick.AddListener(HandleEndTurnClick);
            }
        }

        private void UnsubscribeButtons()
        {
            if (unitButton != null)
            {
                unitButton.onClick.RemoveListener(HandleUnitClick);
            }

            if (settingsButton != null)
            {
                settingsButton.onClick.RemoveListener(HandleSettingsClick);
            }

            if (endTurnButton != null)
            {
                endTurnButton.onClick.RemoveListener(HandleEndTurnClick);
            }
        }

        private void SubscribeItems()
        {
            SubscribeSynergyItems();
            SubscribeArtifactItems();
        }

        private void UnsubscribeItems()
        {
            UnsubscribeSynergyItems();
            UnsubscribeArtifactItems();
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

        private void SubscribeArtifactItems()
        {
            foreach (ArtifactInfoItemView item in artifactItems)
            {
                SubscribeArtifactItem(item);
            }
        }

        private void UnsubscribeArtifactItems()
        {
            foreach (ArtifactInfoItemView item in artifactItems)
            {
                UnsubscribeArtifactItem(item);
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

        private void SubscribeArtifactItem(ArtifactInfoItemView item)
        {
            if (item == null)
            {
                return;
            }

            item.Clicked += HandleArtifactClick;
            item.PointerEntered += HandleArtifactPointerEnter;
            item.PointerExited += HandleArtifactPointerExit;
        }

        private void UnsubscribeArtifactItem(ArtifactInfoItemView item)
        {
            if (item == null)
            {
                return;
            }

            item.Clicked -= HandleArtifactClick;
            item.PointerEntered -= HandleArtifactPointerEnter;
            item.PointerExited -= HandleArtifactPointerExit;
        }

        private void HandleUnitClick()
        {
            UnitClicked?.Invoke(this);
        }

        private void HandleSettingsClick()
        {
            SettingsClicked?.Invoke(this);
        }

        private void HandleEndTurnClick()
        {
            EndTurnClicked?.Invoke(this);
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

        private void HandleArtifactClick(ArtifactInfoItemView item, PointerEventData eventData)
        {
            ArtifactClicked?.Invoke(item, eventData);
        }

        private void HandleArtifactPointerEnter(ArtifactInfoItemView item, PointerEventData eventData)
        {
            ArtifactPointerEntered?.Invoke(item, eventData);
        }

        private void HandleArtifactPointerExit(ArtifactInfoItemView item, PointerEventData eventData)
        {
            ArtifactPointerExited?.Invoke(item, eventData);
        }

        #endregion
    }
}