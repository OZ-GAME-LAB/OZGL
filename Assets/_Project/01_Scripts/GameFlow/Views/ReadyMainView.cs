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
        [UnityEngine.Serialization.FormerlySerializedAs("unitButton")]
        [SerializeField] private Button _unitButton;
        [UnityEngine.Serialization.FormerlySerializedAs("settingsButton")]
        [SerializeField] private Button _settingsButton;
        [UnityEngine.Serialization.FormerlySerializedAs("locateButton")]
        [SerializeField] private Button _locateButton;
        [UnityEngine.Serialization.FormerlySerializedAs("synergyContentRoot")]
        [SerializeField] private Transform _synergyContentRoot;
        [UnityEngine.Serialization.FormerlySerializedAs("artifactContentRoot")]
        [SerializeField] private Transform _artifactContentRoot;

        [UnityEngine.Serialization.FormerlySerializedAs("endTurnButton")]
        [SerializeField] private Button _endTurnButton;
        [UnityEngine.Serialization.FormerlySerializedAs("clockHand")]
        [SerializeField] private RectTransform _clockHand;
        [UnityEngine.Serialization.FormerlySerializedAs("currentTurnNumber")]
        [SerializeField] private RollingNumberView _currentTurnNumber;
        [UnityEngine.Serialization.FormerlySerializedAs("bossRemainingTurnNumber")]
        [SerializeField] private RollingNumberView _bossRemainingTurnNumber;

        [UnityEngine.Serialization.FormerlySerializedAs("endTurnFeedback")]
        [SerializeField] private EndTurnButtonFeedbackView _endTurnFeedback;

        private readonly List<SynergyItemView> _synergyItems = new List<SynergyItemView>();
        private readonly List<ArtifactInfoItemView> _artifactItems = new List<ArtifactInfoItemView>();

        private bool _isListening;

        #region Properties

        public Button UnitButton => _unitButton;
        public Button SettingsButton => _settingsButton;
        public Button LocateButton => _locateButton;

        public Transform SynergyContentRoot => _synergyContentRoot;
        public Transform ArtifactContentRoot => _artifactContentRoot;

        public IReadOnlyList<SynergyItemView> SynergyItems => _synergyItems;
        public IReadOnlyList<ArtifactInfoItemView> ArtifactItems => _artifactItems;

        public bool IsVisible => gameObject.activeSelf;

        public event Action<ReadyMainView> UnitClicked; //유닛 버튼 클릭 이벤트
        public event Action<ReadyMainView> SettingsClicked; //설정 버튼 클릭 이벤트
        public event Action<ReadyMainView> LocateClicked; //목표 위치 확인 버튼 클릭 이벤트
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
            _isListening = true;

            SubscribeButtons();
            SubscribeItems();
        }

        private void OnDisable()
        {
            UnsubscribeButtons();
            UnsubscribeItems();

            _isListening = false;
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
            if (_unitButton != null)
            {
                _unitButton.interactable = value;
            }
        }

        public void SetSettingsButtonInteractable(bool value)
        {
            if (_settingsButton != null)
            {
                _settingsButton.interactable = value;
            }
        }

        public void RefreshSynergyItems()
        {
            if (_isListening)
            {
                UnsubscribeSynergyItems();
            }

            _synergyItems.Clear();

            if (_synergyContentRoot != null)
            {
                _synergyItems.AddRange(_synergyContentRoot.GetComponentsInChildren<SynergyItemView>(true));
            }

            if (_isListening)
            {
                SubscribeSynergyItems();
            }
        }

        public void RefreshArtifactItems()
        {
            if (_isListening)
            {
                UnsubscribeArtifactItems();
            }

            _artifactItems.Clear();

            if (_artifactContentRoot != null)
            {
                _artifactItems.AddRange(_artifactContentRoot.GetComponentsInChildren<ArtifactInfoItemView>(true));
            }

            if (_isListening)
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
            if (item == null || _synergyItems.Contains(item))
            {
                return;
            }

            _synergyItems.Add(item);

            if (_isListening)
            {
                SubscribeSynergyItem(item);
            }
        }

        public void UnregisterSynergyItem(SynergyItemView item)
        {
            if (item == null || !_synergyItems.Remove(item))
            {
                return;
            }

            UnsubscribeSynergyItem(item);
        }

        public void RegisterArtifactItem(ArtifactInfoItemView item)
        {
            if (item == null || _artifactItems.Contains(item))
            {
                return;
            }

            _artifactItems.Add(item);

            if (_isListening)
            {
                SubscribeArtifactItem(item);
            }
        }

        public void UnregisterArtifactItem(ArtifactInfoItemView item)
        {
            if (item == null || !_artifactItems.Remove(item))
            {
                return;
            }

            UnsubscribeArtifactItem(item);
        }

        public void SetEndTurnInteractable(bool value)
        {
            if (_endTurnButton != null)
            {
                _endTurnButton.interactable = value;
            }
        }

        /// <summary>
        /// 턴 종료 버튼의 안내 연출을 켜거나 끕니다.
        /// 행동력과 턴 상태는 외부에서 판단하여 전달합니다.
        /// </summary>
        /// <param name="active">안내 연출 활성 여부</param>
        /// <param name="immediate">애니메이션 없이 즉시 반영할지 여부</param>
        public void SetEndTurnAttention(bool active, bool immediate = false)
        {
            _endTurnFeedback?.SetAttention(active, immediate);
        }

        public void SetActionPointState(EndTurnButtonFeedbackView.TurnActionPointState state,int remainingPoints,bool immediate = false)
        {
            _endTurnFeedback?.SetActionPointState(state, remainingPoints, immediate);
        }

        public void SetCurrentTurn(int turn)
        {
            if (_currentTurnNumber != null)
            {
                _currentTurnNumber.SetValue(turn);
            }
        }

        public void SetBossRemainingTurn(int remainingTurns)
        {
            if (_bossRemainingTurnNumber != null)
            {
                _bossRemainingTurnNumber.SetValue(remainingTurns);
            }
        }

        public void SetClockHandAngle(float angle)
        {
            if (_clockHand != null)
            {
                _clockHand.localEulerAngles = new Vector3(0f, 0f, angle);
            }
        }

        #endregion

        #region Private Methods

        private void SubscribeButtons()
        {
            if (_unitButton != null)
            {
                _unitButton.onClick.AddListener(HandleUnitClick);
            }

            if (_settingsButton != null)
            {
                _settingsButton.onClick.AddListener(HandleSettingsClick);
            }

            if (_locateButton != null)
            {
                _locateButton.onClick.AddListener(HandleLocateClick);
            }

            if (_endTurnButton != null)
            {
                _endTurnButton.onClick.AddListener(HandleEndTurnClick);
            }
        }

        private void UnsubscribeButtons()
        {
            if (_unitButton != null)
            {
                _unitButton.onClick.RemoveListener(HandleUnitClick);
            }

            if (_settingsButton != null)
            {
                _settingsButton.onClick.RemoveListener(HandleSettingsClick);
            }

            if (_locateButton != null)
            {
                _locateButton.onClick.RemoveListener(HandleLocateClick);
            }

            if (_endTurnButton != null)
            {
                _endTurnButton.onClick.RemoveListener(HandleEndTurnClick);
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
            foreach (SynergyItemView item in _synergyItems)
            {
                SubscribeSynergyItem(item);
            }
        }

        private void UnsubscribeSynergyItems()
        {
            foreach (SynergyItemView item in _synergyItems)
            {
                UnsubscribeSynergyItem(item);
            }
        }

        private void SubscribeArtifactItems()
        {
            foreach (ArtifactInfoItemView item in _artifactItems)
            {
                SubscribeArtifactItem(item);
            }
        }

        private void UnsubscribeArtifactItems()
        {
            foreach (ArtifactInfoItemView item in _artifactItems)
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

        private void HandleLocateClick()
        {
            LocateClicked?.Invoke(this);
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
