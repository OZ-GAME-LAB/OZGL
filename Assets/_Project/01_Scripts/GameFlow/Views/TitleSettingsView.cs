using OzGameLab01.UI.Common;
using System;
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
        [SerializeField] private Image _dimmer; //팝업시 배경을 어둡게 처리하는 영역
        [UnityEngine.Serialization.FormerlySerializedAs("panel")]
        [SerializeField] private Image _panel; //버튼을 담는 팝업 패널
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
        [SerializeField] private GameObject _gameContent; //게임 설정 관련 UI 영역
        [UnityEngine.Serialization.FormerlySerializedAs("videoContent")]
        [SerializeField] private GameObject _videoContent; //영상 설정 관련 UI 영역
        [UnityEngine.Serialization.FormerlySerializedAs("audioContent")]
        [SerializeField] private GameObject _audioContent; //오디오 설정 관련 UI 영역

        [Header("Category Feedback")]
        [SerializeField] private UITabButtonFeedback gameButtonFeedback;
        [SerializeField] private UITabButtonFeedback videoButtonFeedback;
        [SerializeField] private UITabButtonFeedback audioButtonFeedback;

        [Header("Game")]
        [UnityEngine.Serialization.FormerlySerializedAs("languageDropdown")]
        [SerializeField] private DropdownRefs _languageDropdown;
        [UnityEngine.Serialization.FormerlySerializedAs("simplifyEffectsToggle")]
        [SerializeField] private ToggleRefs _simplifyEffectsToggle;
        [UnityEngine.Serialization.FormerlySerializedAs("synergySummaryToggle")]
        [SerializeField] private ToggleRefs _synergySummaryToggle;

        [UnityEngine.Serialization.FormerlySerializedAs("replayTutorialButton")]

        [SerializeField] private Button _replayTutorialButton;
        [UnityEngine.Serialization.FormerlySerializedAs("replayCutsceneButton")]
        [SerializeField] private Button _replayCutsceneButton;
        [UnityEngine.Serialization.FormerlySerializedAs("resetGameDataButton")]
        [SerializeField] private Button _resetGameDataButton;

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

        #region Properties
        
        /// <summary>
        /// 설정 팝업의 현재 표시 상태
        /// </summary>
        public bool IsVisible => gameObject.activeSelf;
        
        /// <summary>
        /// 현재 선택된 설정 탭 (Show, SelectTab 호출 시 변경됨)
        /// </summary>
        public SettingsTab CurrentTab { get; private set; } = SettingsTab.Game;
        
        /// <summary>
        /// 현재 선택된 언어 옵션의 인덱스
        /// 외부 설정 데이터를 ui에 반영할 때 설정하며, 사용자 변경은 LanguageSelected 이벤트를 통해 전달됨
        /// 아래도 동일
        /// </summary>
        public int LanguageIndex
        {
            get => _languageDropdown.dropdown.value;
            set => SetDropdownValue(_languageDropdown.dropdown, value); //외부 값 반영 시 이벤트를 다시 발생시키지 않음
        }

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

        public bool IsEffectsSimplified
        {
            get => _simplifyEffectsToggle.toggle.isOn;
            set => _simplifyEffectsToggle.toggle.SetIsOnWithoutNotify(value);
        }

        public bool IsSynergySummaryEnabled
        {
            get => _synergySummaryToggle.toggle.isOn;
            set => _synergySummaryToggle.toggle.SetIsOnWithoutNotify(value);
        }

        #endregion

        #region Events

        public event Action CloseRequested; //설정 팝업의 닫기 버튼을 누르면 발생하는 이벤트
        public event Action<SettingsTab> TabSelected; //설정 팝업의 탭 버튼을 누르면 발생하는 이벤트 (Game, Video, Audio)

        public event Action<int> LanguageSelected; //사용자가 언어 옵션을 변경하면 발생하는 이벤트 (옵션 인덱스 전달)
        public event Action<int> ResolutionSelected; //사용자가 해상도 옵션을 변경하면 발생하는 이벤트 (옵션 인덱스 전달)
        public event Action<int> ScreenModeSelected; //사용자가 화면 모드 옵션을 변경하면 발생하는 이벤트 (옵션 인덱스 전달)

        public event Action<float> MasterVolumeChanged; //사용자가 마스터 볼륨 슬라이더를 변경하면 발생하는 이벤트 (볼륨 값 전달)
        public event Action<float> BgmVolumeChanged; //사용자가 BGM 볼륨 슬라이더를 변경하면 발생하는 이벤트 (볼륨 값 전달)
        public event Action<float> SfxVolumeChanged; //사용자가 SFX 볼륨 슬라이더를 변경하면 발생하는 이벤트 (볼륨 값 전달)

        public event Action<bool> MuteAllChanged; //사용자가 전체 음소거 토글을 변경하면 발생하는 이벤트 (토글 상태 전달)
        public event Action<bool> EffectsSimplifiedChanged; //사용자가 이펙트 단순화 토글을 변경하면 발생하는 이벤트 (토글 상태 전달)
        public event Action<bool> SynergySummaryChanged; //사용자가 시너지 요약 토글을 변경하면 발생하는 이벤트 (토글 상태 전달)

        public event Action ReplayTutorialRequested; //사용자가 튜토리얼 재시청 버튼을 누르면 발생하는 이벤트
        public event Action ReplayCutsceneRequested; //사용자가 컷씬 재시청 버튼을 누르면 발생하는 이벤트
        public event Action ResetGameDataRequested; //사용자가 게임 데이터 초기화 버튼을 누르면 발생하는 이벤트

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _gameButton.onClick.AddListener(OnGameTabClicked);
            _videoButton.onClick.AddListener(OnVideoTabClicked);
            _audioButton.onClick.AddListener(OnAudioTabClicked);
            _backButton.onClick.AddListener(OnBackClicked);

            _languageDropdown.dropdown.onValueChanged.AddListener(OnLanguageChanged);
            _resolutionDropdown.dropdown.onValueChanged.AddListener(OnResolutionChanged);
            _screenModeDropdown.dropdown.onValueChanged.AddListener(OnScreenModeChanged);

            _masterSlider.slider.onValueChanged.AddListener(OnMasterVolumeChanged);
            _bgmSlider.slider.onValueChanged.AddListener(OnBgmVolumeChanged);
            _sfxSlider.slider.onValueChanged.AddListener(OnSfxVolumeChanged);

            _muteAllToggle.toggle.onValueChanged.AddListener(OnMuteAllChanged);
            _simplifyEffectsToggle.toggle.onValueChanged.AddListener(OnEffectsSimplifiedChanged);
            _synergySummaryToggle.toggle.onValueChanged.AddListener(OnSynergySummaryChanged);

            _replayTutorialButton.onClick.AddListener(OnReplayTutorialClicked);
            _replayCutsceneButton.onClick.AddListener(OnReplayCutsceneClicked);
            _resetGameDataButton.onClick.AddListener(OnResetGameDataClicked);

            SelectTab(CurrentTab, false);
        }

        private void OnDestroy()
        {
            _gameButton.onClick.RemoveListener(OnGameTabClicked);
            _videoButton.onClick.RemoveListener(OnVideoTabClicked);
            _audioButton.onClick.RemoveListener(OnAudioTabClicked);
            _backButton.onClick.RemoveListener(OnBackClicked);

            _languageDropdown.dropdown.onValueChanged.RemoveListener(OnLanguageChanged);
            _resolutionDropdown.dropdown.onValueChanged.RemoveListener(OnResolutionChanged);
            _screenModeDropdown.dropdown.onValueChanged.RemoveListener(OnScreenModeChanged);

            _masterSlider.slider.onValueChanged.RemoveListener(OnMasterVolumeChanged);
            _bgmSlider.slider.onValueChanged.RemoveListener(OnBgmVolumeChanged);
            _sfxSlider.slider.onValueChanged.RemoveListener(OnSfxVolumeChanged);

            _muteAllToggle.toggle.onValueChanged.RemoveListener(OnMuteAllChanged);
            _simplifyEffectsToggle.toggle.onValueChanged.RemoveListener(OnEffectsSimplifiedChanged);
            _synergySummaryToggle.toggle.onValueChanged.RemoveListener(OnSynergySummaryChanged);

            _replayTutorialButton.onClick.RemoveListener(OnReplayTutorialClicked);
            _replayCutsceneButton.onClick.RemoveListener(OnReplayCutsceneClicked);
            _resetGameDataButton.onClick.RemoveListener(OnResetGameDataClicked);
        }

        #endregion

        #region Public API
        
        /// <summary>
        /// 설정 팝업을 열고 지정된 탭을 표시
        /// </summary>
        /// <param name="tab"></param>
        public void Show(SettingsTab tab = SettingsTab.Game)
        {
            gameObject.SetActive(true);
            SelectTab(tab, false);
        }
        
        public void Hide()
        {
            gameObject.SetActive(false);
        }

        /// <summary>
        /// 컷씬 재시청 버튼의 표시 여부를 설정합니다.
        /// 컷씬 시스템이 아직 없어 기본적으로 숨겨둡니다.
        /// </summary>
        public void SetReplayCutsceneButtonVisible(bool visible)
        {
            if (_replayCutsceneButton != null)
            {
                _replayCutsceneButton.gameObject.SetActive(visible);
            }
        }

        /// <summary>
        /// 지정된 설정 탭만 활성화 됨
        /// 이때 notify가 트루면 TabSelected 이벤트가 발생함
        /// </summary>
        /// <param name="tab"></param>
        /// <param name="notify"></param>
        public void SelectTab(SettingsTab tab, bool notify)
        {
            CurrentTab = tab;

            _gameContent.SetActive(tab == SettingsTab.Game);
            _videoContent.SetActive(tab == SettingsTab.Video);
            _audioContent.SetActive(tab == SettingsTab.Audio);

            // 처음 열 때는 즉시 반영하고, 클릭 전환에는 애니메이션 적용.
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

        #endregion

        #region Private Methods

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

        private void OnLanguageChanged(int value)
        {
            LanguageSelected?.Invoke(value);
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

        private void OnEffectsSimplifiedChanged(bool value)
        {
            EffectsSimplifiedChanged?.Invoke(value);
        }

        private void OnSynergySummaryChanged(bool value)
        {
            SynergySummaryChanged?.Invoke(value);
        }

        private void OnReplayTutorialClicked()
        {
            ReplayTutorialRequested?.Invoke();
        }

        private void OnReplayCutsceneClicked()
        {
            ReplayCutsceneRequested?.Invoke();
        }

        private void OnResetGameDataClicked()
        {
            ResetGameDataRequested?.Invoke();
        }

        private static void SetDropdownValue(TMP_Dropdown dropdown, int value)
        {
            if (dropdown == null || dropdown.options.Count == 0)
                return;

            int clampedValue = Mathf.Clamp(value, 0, dropdown.options.Count - 1);
            dropdown.SetValueWithoutNotify(clampedValue);
            dropdown.RefreshShownValue();
        }

        private static void SetSliderValue(SliderRefs refs, float value)
        {
            if (refs.slider == null)
                return;

            float clampedValue = Mathf.Clamp(value, refs.slider.minValue, refs.slider.maxValue);
            refs.slider.SetValueWithoutNotify(clampedValue);
        }

        #endregion
    }
}
