using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OzGameLab01.UI
{
    public enum SettingsTab
    {
        Game,
        Video,
        Audio
    }

    public sealed class SettingsView : MonoBehaviour
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
        [SerializeField] private Image dimmer; //팝업시 배경을 어둡게 처리하는 영역
        [SerializeField] private Image panel; //버튼을 담는 팝업 패널
        [SerializeField] private Button backButton;

        [Header("Category Buttons")]
        [SerializeField] private Button gameButton;
        [SerializeField] private Button videoButton;
        [SerializeField] private Button audioButton;

        [Header("Category Content")]
        [SerializeField] private GameObject gameContent; //게임 설정 관련 UI 영역
        [SerializeField] private GameObject videoContent; //영상 설정 관련 UI 영역
        [SerializeField] private GameObject audioContent; //오디오 설정 관련 UI 영역

        [Header("Game")]
        [SerializeField] private DropdownRefs languageDropdown;
        [SerializeField] private ToggleRefs simplifyEffectsToggle;
        [SerializeField] private ToggleRefs synergySummaryToggle;

        [SerializeField] private Button replayTutorialButton;
        [SerializeField] private Button replayCutsceneButton;
        [SerializeField] private Button resetGameDataButton;

        [Header("Video")]
        [SerializeField] private DropdownRefs resolutionDropdown;
        [SerializeField] private DropdownRefs screenModeDropdown;

        [Header("Audio")]
        [SerializeField] private SliderRefs masterSlider;
        [SerializeField] private SliderRefs bgmSlider;
        [SerializeField] private SliderRefs sfxSlider;
        [SerializeField] private ToggleRefs muteAllToggle;

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
            get => languageDropdown.dropdown.value;
            set => SetDropdownValue(languageDropdown.dropdown, value); //외부 값 반영 시 이벤트를 다시 발생시키지 않음
        }

        public int ResolutionIndex
        {
            get => resolutionDropdown.dropdown.value;
            set => SetDropdownValue(resolutionDropdown.dropdown, value);
        }

        public int ScreenModeIndex
        {
            get => screenModeDropdown.dropdown.value;
            set => SetDropdownValue(screenModeDropdown.dropdown, value);
        }

        public float MasterVolume
        {
            get => masterSlider.slider.value;
            set => SetSliderValue(masterSlider, value);
        }

        public float BgmVolume
        {
            get => bgmSlider.slider.value;
            set => SetSliderValue(bgmSlider, value);
        }

        public float SfxVolume
        {
            get => sfxSlider.slider.value;
            set => SetSliderValue(sfxSlider, value);
        }

        public bool IsMuted
        {
            get => muteAllToggle.toggle.isOn;
            set => muteAllToggle.toggle.SetIsOnWithoutNotify(value);
        }

        public bool IsEffectsSimplified
        {
            get => simplifyEffectsToggle.toggle.isOn;
            set => simplifyEffectsToggle.toggle.SetIsOnWithoutNotify(value);
        }

        public bool IsSynergySummaryEnabled
        {
            get => synergySummaryToggle.toggle.isOn;
            set => synergySummaryToggle.toggle.SetIsOnWithoutNotify(value);
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
            gameButton.onClick.AddListener(OnGameTabClicked);
            videoButton.onClick.AddListener(OnVideoTabClicked);
            audioButton.onClick.AddListener(OnAudioTabClicked);
            backButton.onClick.AddListener(OnBackClicked);

            languageDropdown.dropdown.onValueChanged.AddListener(OnLanguageChanged);
            resolutionDropdown.dropdown.onValueChanged.AddListener(OnResolutionChanged);
            screenModeDropdown.dropdown.onValueChanged.AddListener(OnScreenModeChanged);

            masterSlider.slider.onValueChanged.AddListener(OnMasterVolumeChanged);
            bgmSlider.slider.onValueChanged.AddListener(OnBgmVolumeChanged);
            sfxSlider.slider.onValueChanged.AddListener(OnSfxVolumeChanged);

            muteAllToggle.toggle.onValueChanged.AddListener(OnMuteAllChanged);
            simplifyEffectsToggle.toggle.onValueChanged.AddListener(OnEffectsSimplifiedChanged);
            synergySummaryToggle.toggle.onValueChanged.AddListener(OnSynergySummaryChanged);

            replayTutorialButton.onClick.AddListener(OnReplayTutorialClicked);
            replayCutsceneButton.onClick.AddListener(OnReplayCutsceneClicked);
            resetGameDataButton.onClick.AddListener(OnResetGameDataClicked);

            SelectTab(CurrentTab, false);
        }

        private void OnDestroy()
        {
            gameButton.onClick.RemoveListener(OnGameTabClicked);
            videoButton.onClick.RemoveListener(OnVideoTabClicked);
            audioButton.onClick.RemoveListener(OnAudioTabClicked);
            backButton.onClick.RemoveListener(OnBackClicked);

            languageDropdown.dropdown.onValueChanged.RemoveListener(OnLanguageChanged);
            resolutionDropdown.dropdown.onValueChanged.RemoveListener(OnResolutionChanged);
            screenModeDropdown.dropdown.onValueChanged.RemoveListener(OnScreenModeChanged);

            masterSlider.slider.onValueChanged.RemoveListener(OnMasterVolumeChanged);
            bgmSlider.slider.onValueChanged.RemoveListener(OnBgmVolumeChanged);
            sfxSlider.slider.onValueChanged.RemoveListener(OnSfxVolumeChanged);

            muteAllToggle.toggle.onValueChanged.RemoveListener(OnMuteAllChanged);
            simplifyEffectsToggle.toggle.onValueChanged.RemoveListener(OnEffectsSimplifiedChanged);
            synergySummaryToggle.toggle.onValueChanged.RemoveListener(OnSynergySummaryChanged);

            replayTutorialButton.onClick.RemoveListener(OnReplayTutorialClicked);
            replayCutsceneButton.onClick.RemoveListener(OnReplayCutsceneClicked);
            resetGameDataButton.onClick.RemoveListener(OnResetGameDataClicked);
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
        /// 지정된 설정 탭만 활성화 됨
        /// 이때 notify가 트루면 TabSelected 이벤트가 발생함
        /// </summary>
        /// <param name="tab"></param>
        /// <param name="notify"></param>
        public void SelectTab(SettingsTab tab, bool notify)
        {
            CurrentTab = tab;

            gameContent.SetActive(tab == SettingsTab.Game);
            videoContent.SetActive(tab == SettingsTab.Video);
            audioContent.SetActive(tab == SettingsTab.Audio);

            if (notify)
                TabSelected?.Invoke(tab);
        }

        #endregion

        #region Internal API
        
        /// <summary>
        /// 외부에서 리소스 데이터를 받아 UI에 적용
        /// </summary>
        /// <param name="resources"></param>
        internal void ApplyResources(TitleSettingsResources resources)
        {
            if (resources == null)
                return;

            TitleUiStyleApplier.Apply(dimmer, resources.dimmer);
            TitleUiStyleApplier.Apply(panel, resources.panel);

            TitleUiStyleApplier.Apply(gameButton, resources.categoryButton);
            TitleUiStyleApplier.Apply(videoButton, resources.categoryButton);
            TitleUiStyleApplier.Apply(audioButton, resources.categoryButton);
            TitleUiStyleApplier.Apply(backButton, resources.backButton);

            TitleUiStyleApplier.Apply(replayTutorialButton, resources.actionButton);
            TitleUiStyleApplier.Apply(replayCutsceneButton, resources.actionButton);
            TitleUiStyleApplier.Apply(resetGameDataButton, resources.actionButton);

            ApplyDropdown(languageDropdown, resources.dropdown);
            ApplyDropdown(resolutionDropdown, resources.dropdown);
            ApplyDropdown(screenModeDropdown, resources.dropdown);

            ApplySlider(masterSlider, resources.slider);
            ApplySlider(bgmSlider, resources.slider);
            ApplySlider(sfxSlider, resources.slider);

            ApplyToggle(muteAllToggle, resources.toggle);
            ApplyToggle(simplifyEffectsToggle, resources.toggle);
            ApplyToggle(synergySummaryToggle, resources.toggle);
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

        private static void ApplyDropdown(DropdownRefs refs, DropdownStyle style)
        {
            TitleUiStyleApplier.Apply(refs.background, style.background);
            TitleUiStyleApplier.Apply(refs.arrow, style.arrow);
            TitleUiStyleApplier.Apply(refs.itemBackground, style.itemBackground);
            TitleUiStyleApplier.Apply(refs.itemCheckmark, style.itemCheckmark);
        }

        private static void ApplySlider(SliderRefs refs, SliderStyle style)
        {
            TitleUiStyleApplier.Apply(refs.track, style.track);
            TitleUiStyleApplier.Apply(refs.fill, style.fill);
            TitleUiStyleApplier.Apply(refs.handle, style.handle);
        }

        private static void ApplyToggle(ToggleRefs refs, ToggleStyle style)
        {
            TitleUiStyleApplier.Apply(
                refs.toggle,
                style,
                refs.background,
                refs.checkmark);
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
