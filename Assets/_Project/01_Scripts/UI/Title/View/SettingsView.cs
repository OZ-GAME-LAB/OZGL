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
        [SerializeField] private Image dimmer;
        [SerializeField] private Image panel;

        [SerializeField] private Button backButton;

        [Header("Category Buttons")]
        [SerializeField] private Button gameButton;
        [SerializeField] private Button videoButton;
        [SerializeField] private Button audioButton;

        [Header("Category Content")]
        [SerializeField] private GameObject gameContent;
        [SerializeField] private GameObject videoContent;
        [SerializeField] private GameObject audioContent;

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

        public bool IsVisible => gameObject.activeSelf;

        public SettingsTab CurrentTab { get; private set; } = SettingsTab.Game;

        public int LanguageIndex
        {
            get => languageDropdown.dropdown.value;
            set => SetDropdownValue(languageDropdown.dropdown, value);
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

        public event Action CloseRequested;
        public event Action<SettingsTab> TabSelected;

        public event Action<int> LanguageSelected;
        public event Action<int> ResolutionSelected;
        public event Action<int> ScreenModeSelected;

        public event Action<float> MasterVolumeChanged;
        public event Action<float> BgmVolumeChanged;
        public event Action<float> SfxVolumeChanged;

        public event Action<bool> MuteAllChanged;
        public event Action<bool> EffectsSimplifiedChanged;
        public event Action<bool> SynergySummaryChanged;

        public event Action ReplayTutorialRequested;
        public event Action ReplayCutsceneRequested;
        public event Action ResetGameDataRequested;

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

        public void Show(SettingsTab tab = SettingsTab.Game)
        {
            gameObject.SetActive(true);
            SelectTab(tab, false);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

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
