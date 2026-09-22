using OzGameLab01.UI.Common;
using OzGameLab01.UI;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OzGameLab01.UI.Title
{
    public enum SettingsTab
    {
        Game,
        Video,
        Audio
    }

    public sealed class TitleSettingsView : MonoBehaviour
    {
        [Serializable]
        private struct DropdownRefs
        {
            public TMP_Dropdown dropdown;
            public Image background;
            public Image arrow;
            public Image itemBackground;
            public Image itemCheckmark;
        }

        [Serializable]
        private struct SliderRefs
        {
            public Slider slider;
            public Image track;
            public Image fill;
            public Image handle;
        }

        [Serializable]
        private struct ToggleRefs
        {
            public Toggle toggle;
            public Image background;
            public Image checkmark;
        }

        [Header("Base")]
        [UnityEngine.Serialization.FormerlySerializedAs("dimmer")]
        [SerializeField] private Image _dimmer;

        [UnityEngine.Serialization.FormerlySerializedAs("panel")]
        [SerializeField] private Image _panel;

        [UnityEngine.Serialization.FormerlySerializedAs("backButton")]
        [SerializeField] private Button _backButton;

        [Header("Category Buttons")]
        [UnityEngine.Serialization.FormerlySerializedAs("gameButton")]
        [SerializeField] private Button _gameButton;

        [UnityEngine.Serialization.FormerlySerializedAs("videoButton")]
        [SerializeField] private Button _videoButton;

        [UnityEngine.Serialization.FormerlySerializedAs("audioButton")]
        [SerializeField] private Button _audioButton;

        [Header("Category Content")]
        [UnityEngine.Serialization.FormerlySerializedAs("gameContent")]
        [SerializeField] private GameObject _gameContent;

        [UnityEngine.Serialization.FormerlySerializedAs("videoContent")]
        [SerializeField] private GameObject _videoContent;

        [UnityEngine.Serialization.FormerlySerializedAs("audioContent")]
        [SerializeField] private GameObject _audioContent;

        [Header("Category Feedback")]
        [SerializeField] private UITabButtonFeedback gameButtonFeedback;
        [SerializeField] private UITabButtonFeedback videoButtonFeedback;
        [SerializeField] private UITabButtonFeedback audioButtonFeedback;

        [Header("Game Buttons")]
        [SerializeField] private SettingsActionButtonView _gameButtonPrefab;
        [SerializeField] private Transform _gameButtonContainer;
        [SerializeField] private ConfirmPopupView resetConfirmPrefab;

        [Header("Video")]
        [UnityEngine.Serialization.FormerlySerializedAs("resolutionDropdown")]
        [SerializeField] private DropdownRefs _resolutionDropdown;

        [UnityEngine.Serialization.FormerlySerializedAs("screenModeDropdown")]
        [SerializeField] private DropdownRefs _screenModeDropdown;

        [Header("Audio")]
        [UnityEngine.Serialization.FormerlySerializedAs("masterSlider")]
        [SerializeField] private SliderRefs _masterSlider;

        [UnityEngine.Serialization.FormerlySerializedAs("bgmSlider")]
        [SerializeField] private SliderRefs _bgmSlider;

        [UnityEngine.Serialization.FormerlySerializedAs("sfxSlider")]
        [SerializeField] private SliderRefs _sfxSlider;

        [UnityEngine.Serialization.FormerlySerializedAs("muteAllToggle")]
        [SerializeField] private ToggleRefs _muteAllToggle;

        private readonly List<SettingsActionButtonView> _gameButtons =
            new List<SettingsActionButtonView>();
        private ConfirmPopupView _resetConfirm;
        private Action _resetConfirmed;
        private int _initialResolutionIndex;
        private int _initialScreenModeIndex;

        #region Properties

        public bool IsVisible => gameObject.activeSelf;

        public SettingsTab CurrentTab { get; private set; } = SettingsTab.Game;

        public int ResolutionIndex
        {
            get => _resolutionDropdown.dropdown.value;
            set => SetDropdownValue(_resolutionDropdown.dropdown, value);
        }

        public int ScreenModeIndex
        {
            get => _screenModeDropdown.dropdown.value;
            set => SetDropdownValue(_screenModeDropdown.dropdown, value);
        }

        public float MasterVolume
        {
            get => _masterSlider.slider.value;
            set => SetSliderValue(_masterSlider, value);
        }

        public float BgmVolume
        {
            get => _bgmSlider.slider.value;
            set => SetSliderValue(_bgmSlider, value);
        }

        public float SfxVolume
        {
            get => _sfxSlider.slider.value;
            set => SetSliderValue(_sfxSlider, value);
        }

        public bool IsMuted
        {
            get => _muteAllToggle.toggle.isOn;
            set => _muteAllToggle.toggle.SetIsOnWithoutNotify(value);
        }

        #endregion

        #region Events

        public event Action CloseRequested;
        public event Action<SettingsTab> TabSelected;

        public event Action<int> ResolutionSelected;
        public event Action<int> ScreenModeSelected;

        public event Action<float> MasterVolumeChanged;
        public event Action<float> BgmVolumeChanged;
        public event Action<float> SfxVolumeChanged;

        public event Action<bool> MuteAllChanged;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _initialResolutionIndex = _resolutionDropdown.dropdown.value;
            _initialScreenModeIndex = _screenModeDropdown.dropdown.value;

            _gameButton.onClick.AddListener(OnGameTabClicked);
            _videoButton.onClick.AddListener(OnVideoTabClicked);
            _audioButton.onClick.AddListener(OnAudioTabClicked);
            _backButton.onClick.AddListener(OnBackClicked);

            _resolutionDropdown.dropdown.onValueChanged.AddListener(OnResolutionChanged);
            _screenModeDropdown.dropdown.onValueChanged.AddListener(OnScreenModeChanged);

            _masterSlider.slider.onValueChanged.AddListener(OnMasterVolumeChanged);
            _bgmSlider.slider.onValueChanged.AddListener(OnBgmVolumeChanged);
            _sfxSlider.slider.onValueChanged.AddListener(OnSfxVolumeChanged);

            _muteAllToggle.toggle.onValueChanged.AddListener(OnMuteAllChanged);

            SelectTab(CurrentTab, false);
        }

        private void OnDestroy()
        {
            _gameButton.onClick.RemoveListener(OnGameTabClicked);
            _videoButton.onClick.RemoveListener(OnVideoTabClicked);
            _audioButton.onClick.RemoveListener(OnAudioTabClicked);
            _backButton.onClick.RemoveListener(OnBackClicked);

            _resolutionDropdown.dropdown.onValueChanged.RemoveListener(OnResolutionChanged);
            _screenModeDropdown.dropdown.onValueChanged.RemoveListener(OnScreenModeChanged);

            _masterSlider.slider.onValueChanged.RemoveListener(OnMasterVolumeChanged);
            _bgmSlider.slider.onValueChanged.RemoveListener(OnBgmVolumeChanged);
            _sfxSlider.slider.onValueChanged.RemoveListener(OnSfxVolumeChanged);

            _muteAllToggle.toggle.onValueChanged.RemoveListener(OnMuteAllChanged);

            ClearGameButtons();
            if (_resetConfirm != null)
            {
                _resetConfirm.ConfirmClicked -= OnResetConfirmed;
                _resetConfirm.CancelClicked -= OnResetCancelled;
            }
        }

        #endregion

        #region Public API

        public void Show(SettingsTab tab = SettingsTab.Game)
        {
            gameObject.SetActive(true);
            SelectTab(tab, false);
        }

        public void Hide()
        {
            HideResetConfirmation();
            gameObject.SetActive(false);
        }

        public void SelectTab(SettingsTab tab, bool notify)
        {
            CurrentTab = tab;

            _gameContent.SetActive(tab == SettingsTab.Game);
            _videoContent.SetActive(tab == SettingsTab.Video);
            _audioContent.SetActive(tab == SettingsTab.Audio);

            bool immediate = !notify;

            gameButtonFeedback?.SetSelected(
                tab == SettingsTab.Game, immediate);

            videoButtonFeedback?.SetSelected(
                tab == SettingsTab.Video, immediate);

            audioButtonFeedback?.SetSelected(
                tab == SettingsTab.Audio, immediate);

            if (notify)
                TabSelected?.Invoke(tab);
        }

        /// <summary>
        /// 연결된 버튼 프리팹을 생성하고 표시 이름과 동작을 설정합니다.
        /// </summary>
        public SettingsActionButtonView AddGameButton(
            string label,
            Action onClicked)
        {
            if (_gameButtonPrefab == null || _gameButtonContainer == null)
            {
                Debug.LogError(
                    "게임 버튼 프리팹과 생성 위치를 연결해야 합니다.",
                    this);

                return null;
            }

            SettingsActionButtonView buttonView = Instantiate(
                _gameButtonPrefab,
                _gameButtonContainer,
                false);

            buttonView.Initialize(label, onClicked);
            buttonView.gameObject.SetActive(true);

            _gameButtons.Add(buttonView);
            return buttonView;
        }

        /// <summary>
        /// 이 View에서 동적으로 생성한 버튼을 모두 제거합니다.
        /// </summary>
        public void ClearGameButtons()
        {
            foreach (SettingsActionButtonView buttonView in _gameButtons)
            {
                if (buttonView == null)
                    continue;

                buttonView.gameObject.SetActive(false);
                Destroy(buttonView.gameObject);
            }

            _gameButtons.Clear();
        }

        /// <summary>
        /// 해상도 드롭다운의 선택지를 채웁니다. 모니터마다 실제 지원 해상도가 달라
        /// 런타임에 DisplayManager가 구성한 목록으로 갈아끼웁니다. 값 설정은
        /// ResolutionIndex 세터(SetValueWithoutNotify)로 분리 유지 — 옵션 교체가
        /// ResolutionSelected 이벤트를 발생시키면 안 됩니다.
        /// </summary>
        public void SetResolutionOptions(IReadOnlyList<string> labels)
        {
            _resolutionDropdown.dropdown.ClearOptions();
            _resolutionDropdown.dropdown.AddOptions(new List<string>(labels));
            _resolutionDropdown.dropdown.RefreshShownValue();
        }

        public void RemoveGameButton(SettingsActionButtonView buttonView)
        {
            if (buttonView == null || _gameButtons.Remove(buttonView) == false)
            {
                return;
            }

            buttonView.gameObject.SetActive(false);
            Destroy(buttonView.gameObject);
        }

        /// <summary>
        /// 타이틀 설정 화면의 데이터 초기화 확인 팝업입니다.
        /// </summary>
        public void ShowResetConfirmation(Action onConfirmed)
        {
            if (resetConfirmPrefab == null)
            {
                Debug.LogError("데이터 초기화 확인 팝업 프리팹을 연결해야 합니다.", this);
                return;
            }

            if (_resetConfirm == null)
            {
                _resetConfirm = Instantiate(resetConfirmPrefab, transform, false);
                _resetConfirm.HideImmediate();
                _resetConfirm.ConfirmClicked += OnResetConfirmed;
                _resetConfirm.CancelClicked += OnResetCancelled;
            }

            _resetConfirmed = onConfirmed;
            _resetConfirm.SetConfirmButtonText("초기화");
            _resetConfirm.SetCancelButtonText("취소");
            _resetConfirm.Show("모든 게임 데이터와 설정을 초기화하시겠습니까?", "삭제한 데이터는 복구할 수 없습니다.");
        }

        public void HideResetConfirmation()
        {
            _resetConfirmed = null;
            if (_resetConfirm != null)
            {
                _resetConfirm.HideImmediate();
            }
        }

        public void ResetTransientOptions()
        {
            ResolutionIndex = _initialResolutionIndex;
            ScreenModeIndex = _initialScreenModeIndex;
        }

        #endregion

        #region Private Methods

        private void OnResetConfirmed(ConfirmPopupView view)
        {
            Action onConfirmed = _resetConfirmed;
            HideResetConfirmation();
            onConfirmed?.Invoke();
        }

        private void OnResetCancelled(ConfirmPopupView view)
        {
            HideResetConfirmation();
        }

        private void OnGameTabClicked()
        {
            SelectTab(SettingsTab.Game, true);
        }

        private void OnVideoTabClicked()
        {
            SelectTab(SettingsTab.Video, true);
        }

        private void OnAudioTabClicked()
        {
            SelectTab(SettingsTab.Audio, true);
        }

        private void OnBackClicked()
        {
            CloseRequested?.Invoke();
        }

        private void OnResolutionChanged(int value)
        {
            ResolutionSelected?.Invoke(value);
        }

        private void OnScreenModeChanged(int value)
        {
            ScreenModeSelected?.Invoke(value);
        }

        private void OnMasterVolumeChanged(float value)
        {
            MasterVolumeChanged?.Invoke(value);
        }

        private void OnBgmVolumeChanged(float value)
        {
            BgmVolumeChanged?.Invoke(value);
        }

        private void OnSfxVolumeChanged(float value)
        {
            SfxVolumeChanged?.Invoke(value);
        }

        private void OnMuteAllChanged(bool value)
        {
            MuteAllChanged?.Invoke(value);
        }

        private static void SetDropdownValue(TMP_Dropdown dropdown, int value)
        {
            if (dropdown == null || dropdown.options.Count == 0)
                return;

            int clampedValue = Mathf.Clamp(
                value, 0, dropdown.options.Count - 1);

            dropdown.SetValueWithoutNotify(clampedValue);
            dropdown.RefreshShownValue();
        }

        private static void SetSliderValue(SliderRefs refs, float value)
        {
            if (refs.slider == null)
                return;

            float clampedValue = Mathf.Clamp(
                value,
                refs.slider.minValue,
                refs.slider.maxValue);

            refs.slider.SetValueWithoutNotify(clampedValue);
        }

        #endregion
    }
}
