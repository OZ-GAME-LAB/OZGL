using OzGameLab01.Managers;
using UnityEngine;

namespace OzGameLab01.UI.Title
{
    /// <summary>
    /// 타이틀 설정창의 오디오 옵션과 전역 SoundManager를 연결합니다.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(TitleSettingsView))]
    public sealed class TitleSettingsAudioController : MonoBehaviour
    {
        [SerializeField] private TitleSettingsView settingsView;

        private SoundManager soundManager;
        private bool isSubscribed;

        private void OnEnable()
        {
            soundManager = SoundManager.Instance;
            if (soundManager == null)
            {
                Debug.LogWarning("[TitleSettingsAudioController] SoundManager를 찾을 수 없습니다.", this);
                return;
            }

            if (!soundManager.IsInitialized)
            {
                soundManager.Initialize();
            }

            Subscribe();
            RefreshView();
        }

        private void OnDisable()
        {
            if (soundManager != null) soundManager.SaveVolumeSettings();
            Unsubscribe();
        }

        private void Subscribe()
        {
            if (isSubscribed || settingsView == null)
            {
                return;
            }

            settingsView.CloseRequested += SaveSettings;
            settingsView.TabSelected += HandleTabSelected;
            settingsView.MasterVolumeChanged += soundManager.SetMasterVolume;
            settingsView.BgmVolumeChanged += soundManager.SetBgmVolume;
            settingsView.SfxVolumeChanged += soundManager.SetSfxVolume;
            settingsView.MuteAllChanged += soundManager.SetMuteAll;
            soundManager.VolumeSettingsChanged += RefreshView;
            isSubscribed = true;
        }

        private void Unsubscribe()
        {
            if (!isSubscribed)
            {
                return;
            }

            if (settingsView != null && soundManager != null)
            {
                settingsView.CloseRequested -= SaveSettings;
                settingsView.TabSelected -= HandleTabSelected;
                settingsView.MasterVolumeChanged -= soundManager.SetMasterVolume;
                settingsView.BgmVolumeChanged -= soundManager.SetBgmVolume;
                settingsView.SfxVolumeChanged -= soundManager.SetSfxVolume;
                settingsView.MuteAllChanged -= soundManager.SetMuteAll;
            }

            if (soundManager != null)
            {
                soundManager.VolumeSettingsChanged -= RefreshView;
            }

            isSubscribed = false;
        }

        private void SaveSettings()
        {
            if (soundManager != null) soundManager.SaveVolumeSettings();
        }

        private void HandleTabSelected(SettingsTab tab) => SaveSettings();

        private void RefreshView()
        {
            if (soundManager == null || settingsView == null)
            {
                return;
            }

            settingsView.MasterVolume = soundManager.MasterVolume;
            settingsView.BgmVolume = soundManager.BgmVolume;
            settingsView.SfxVolume = soundManager.SfxVolume;
            settingsView.IsMuted = soundManager.IsMuted;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (settingsView == null)
            {
                settingsView = GetComponent<TitleSettingsView>();
            }
        }
#endif
    }
}
