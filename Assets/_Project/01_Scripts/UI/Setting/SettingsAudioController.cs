using OzGameLab01.Managers;
using UnityEngine;

namespace OzGameLab01.UI.Settings
{
    /// <summary>
    /// 기존 SettingsView의 오디오 항목과 전역 SoundManager를 연결합니다.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SettingsView))]
    public sealed class SettingsAudioController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private SettingsView settingsView;
        [SerializeField] private SliderItemView masterVolumeItem;
        [SerializeField] private SliderItemView bgmVolumeItem;
        [SerializeField] private SliderItemView sfxVolumeItem;
        [SerializeField] private ToggleItemView muteAllItem;

        private SoundManager soundManager;
        private bool isSubscribed;

        private void OnEnable()
        {
            soundManager = SoundManager.Instance;
            if (soundManager == null)
            {
                Debug.LogWarning("[SettingsAudioController] SoundManager를 찾을 수 없습니다.", this);
                return;
            }

            if (!soundManager.IsInitialized)
            {
                soundManager.Initialize();
            }

            Subscribe();
            ConfigureItems();
            RefreshView();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void Subscribe()
        {
            if (isSubscribed || settingsView == null)
            {
                return;
            }

            settingsView.SliderValueChanged += HandleSliderValueChanged;
            settingsView.ToggleValueChanged += HandleToggleValueChanged;
            soundManager.VolumeSettingsChanged += RefreshView;
            isSubscribed = true;
        }

        private void Unsubscribe()
        {
            if (!isSubscribed)
            {
                return;
            }

            if (settingsView != null)
            {
                settingsView.SliderValueChanged -= HandleSliderValueChanged;
                settingsView.ToggleValueChanged -= HandleToggleValueChanged;
            }

            if (soundManager != null)
            {
                soundManager.VolumeSettingsChanged -= RefreshView;
            }

            isSubscribed = false;
        }

        private void ConfigureItems()
        {
            masterVolumeItem?.SetRange(0f, 1f);
            bgmVolumeItem?.SetRange(0f, 1f);
            sfxVolumeItem?.SetRange(0f, 1f);
        }

        private void RefreshView()
        {
            if (soundManager == null)
            {
                return;
            }

            masterVolumeItem?.SetValue(soundManager.MasterVolume);
            bgmVolumeItem?.SetValue(soundManager.BgmVolume);
            sfxVolumeItem?.SetValue(soundManager.SfxVolume);
            muteAllItem?.SetIsOn(soundManager.IsMuted);
        }

        private void HandleSliderValueChanged(SliderItemView item, float value)
        {
            if (item == masterVolumeItem)
            {
                soundManager.SetMasterVolume(value);
            }
            else if (item == bgmVolumeItem)
            {
                soundManager.SetBgmVolume(value);
            }
            else if (item == sfxVolumeItem)
            {
                soundManager.SetSfxVolume(value);
            }
        }

        private void HandleToggleValueChanged(ToggleItemView item, bool isOn)
        {
            if (item == muteAllItem)
            {
                soundManager.SetMuteAll(isOn);
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (settingsView == null)
            {
                settingsView = GetComponent<SettingsView>();
            }
        }
#endif
    }
}
