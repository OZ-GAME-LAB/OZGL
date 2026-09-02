using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OzGameLab01.UI.Settings
{
    [DisallowMultipleComponent]
    public sealed class SettingsView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private Transform contentRoot;
        [SerializeField] private Button returnToMainButton;
        [SerializeField] private Button backButton;

        [Header("Setting Items")]
        [SerializeField] private List<SliderItemView> sliderItems = new List<SliderItemView>();
        [SerializeField] private List<ToggleItemView> toggleItems = new List<ToggleItemView>();
        [SerializeField] private List<SettingItemView> settingItems = new List<SettingItemView>();

        private bool isListening;

        #region Properties

        public TMP_Text TitleText => titleText;
        public ScrollRect ScrollRect => scrollRect;
        public Transform ContentRoot => contentRoot;
        public Button ReturnToMainButton => returnToMainButton;
        public Button BackButton => backButton;

        public IReadOnlyList<SliderItemView> SliderItems => sliderItems;
        public IReadOnlyList<ToggleItemView> ToggleItems => toggleItems;
        public IReadOnlyList<SettingItemView> SettingItems => settingItems;

        public string Title
        {
            get => titleText != null ? titleText.text : string.Empty;
            set
            {
                if (titleText != null)
                {
                    titleText.text = value ?? string.Empty;
                }
            }
        }

        public event Action<SettingsView> ReturnToMainClicked; //메인으로 돌아가기 버튼 클릭 이벤트
        public event Action<SettingsView> BackClicked; //뒤로가기 버튼 클릭 이벤트
        public event Action<SliderItemView, float> SliderValueChanged; //슬라이더 값 변경 이벤트
        public event Action<ToggleItemView, bool> ToggleValueChanged; //토글 값 변경 이벤트
        public event Action<SettingItemView> SettingItemClicked; //설정 항목 클릭 이벤트

        #endregion

        #region Lifecycle

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

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (scrollRect == null)
            {
                scrollRect = GetComponentInChildren<ScrollRect>(true);
            }

            if (contentRoot == null && scrollRect != null)
            {
                contentRoot = scrollRect.content;
            }

            if (titleText == null)
            {
                titleText = GetComponentInChildren<TMP_Text>(true);
            }
        }
#endif

        #endregion

        #region API

        public void SetTitle(string value)
        {
            Title = value;
        }

        public void Show()
        {
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        public void ScrollToTop()
        {
            SetVerticalNormalizedPosition(1f);
        }

        public void ScrollToBottom()
        {
            SetVerticalNormalizedPosition(0f);
        }

        public void SetVerticalNormalizedPosition(float value)
        {
            if (scrollRect == null)
            {
                return;
            }

            scrollRect.verticalNormalizedPosition = Mathf.Clamp01(value);
        }

        public void RegisterSliderItem(SliderItemView item)
        {
            if (item == null || sliderItems.Contains(item))
            {
                return;
            }

            sliderItems.Add(item);

            if (isListening)
            {
                item.ValueChanged += HandleSliderValueChanged;
            }
        }

        public void UnregisterSliderItem(SliderItemView item)
        {
            if (item == null || !sliderItems.Remove(item))
            {
                return;
            }

            item.ValueChanged -= HandleSliderValueChanged;
        }

        public void RegisterToggleItem(ToggleItemView item)
        {
            if (item == null || toggleItems.Contains(item))
            {
                return;
            }

            toggleItems.Add(item);

            if (isListening)
            {
                item.ValueChanged += HandleToggleValueChanged;
            }
        }

        public void UnregisterToggleItem(ToggleItemView item)
        {
            if (item == null || !toggleItems.Remove(item))
            {
                return;
            }

            item.ValueChanged -= HandleToggleValueChanged;
        }

        public void RegisterSettingItem(SettingItemView item)
        {
            if (item == null || settingItems.Contains(item))
            {
                return;
            }

            settingItems.Add(item);

            if (isListening)
            {
                item.Clicked += HandleSettingItemClicked;
            }
        }

        public void UnregisterSettingItem(SettingItemView item)
        {
            if (item == null || !settingItems.Remove(item))
            {
                return;
            }

            item.Clicked -= HandleSettingItemClicked;
        }

        #endregion

        #region Private Methods

        private void SubscribeButtons()
        {
            if (returnToMainButton != null)
            {
                returnToMainButton.onClick.AddListener(HandleReturnToMainClick);
            }

            if (backButton != null)
            {
                backButton.onClick.AddListener(HandleBackClick);
            }
        }

        private void UnsubscribeButtons()
        {
            if (returnToMainButton != null)
            {
                returnToMainButton.onClick.RemoveListener(HandleReturnToMainClick);
            }

            if (backButton != null)
            {
                backButton.onClick.RemoveListener(HandleBackClick);
            }
        }

        private void SubscribeItems()
        {
            foreach (SliderItemView item in sliderItems)
            {
                if (item != null)
                {
                    item.ValueChanged += HandleSliderValueChanged;
                }
            }

            foreach (ToggleItemView item in toggleItems)
            {
                if (item != null)
                {
                    item.ValueChanged += HandleToggleValueChanged;
                }
            }

            foreach (SettingItemView item in settingItems)
            {
                if (item != null)
                {
                    item.Clicked += HandleSettingItemClicked;
                }
            }
        }

        private void UnsubscribeItems()
        {
            foreach (SliderItemView item in sliderItems)
            {
                if (item != null)
                {
                    item.ValueChanged -= HandleSliderValueChanged;
                }
            }

            foreach (ToggleItemView item in toggleItems)
            {
                if (item != null)
                {
                    item.ValueChanged -= HandleToggleValueChanged;
                }
            }

            foreach (SettingItemView item in settingItems)
            {
                if (item != null)
                {
                    item.Clicked -= HandleSettingItemClicked;
                }
            }
        }

        private void HandleReturnToMainClick()
        {
            ReturnToMainClicked?.Invoke(this);
        }

        private void HandleBackClick()
        {
            BackClicked?.Invoke(this);
        }

        private void HandleSliderValueChanged(SliderItemView item, float value)
        {
            SliderValueChanged?.Invoke(item, value);
        }

        private void HandleToggleValueChanged(ToggleItemView item, bool isOn)
        {
            ToggleValueChanged?.Invoke(item, isOn);
        }

        private void HandleSettingItemClicked(SettingItemView item)
        {
            SettingItemClicked?.Invoke(item);
        }

        #endregion
    }
}
