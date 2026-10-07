using OzGameLab01.Managers;
using OzGameLab01.UI.Title;
using UnityEngine;

namespace OzGameLab01.Controllers
{
    /// <summary>
    /// 타이틀 설정창의 오디오 옵션과 전역 SoundManager를 연결합니다.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(TitleSettingsView))]
    public sealed class TitleSettingsAudioController : MonoBehaviour
    {
        [UnityEngine.Serialization.FormerlySerializedAs("settingsView")]
        [SerializeField] private TitleSettingsView _settingsView;

        private SoundManager _soundManager;
        private bool _isSubscribed;
        private TitleSettingsView _subscribedView;

        private void OnEnable()
        {
            _soundManager = SoundManager.Instance;
            if (_soundManager == null)
            {
                Debug.LogWarning("[TitleSettingsAudioController] SoundManager를 찾을 수 없습니다.", this);
                return;
            }

            if (!_soundManager.IsInitialized)
            {
                _soundManager.Initialize();
            }

            Subscribe();
            RefreshView();
        }

        private void OnDisable()
        {
            if (_soundManager != null) _soundManager.SaveVolumeSettings();
            Unsubscribe();
        }

        private void Subscribe()
        {
            if (_isSubscribed || _settingsView == null)
            {
                return;
            }

            _subscribedView = _settingsView;
            _settingsView.CloseRequested += SaveSettings;
            _settingsView.TabSelected += HandleTabSelected;
            _settingsView.MasterVolumeChanged += _soundManager.SetMasterVolume;
            _settingsView.BgmVolumeChanged += _soundManager.SetBgmVolume;
            _settingsView.SfxVolumeChanged += _soundManager.SetSfxVolume;
            _settingsView.MuteAllChanged += _soundManager.SetMuteAll;
            _soundManager.VolumeSettingsChanged += RefreshView;
            _isSubscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_isSubscribed)
            {
                return;
            }

            if (_subscribedView != null)
            {
                _subscribedView.CloseRequested -= SaveSettings;
                _subscribedView.TabSelected -= HandleTabSelected;
                if (!ReferenceEquals(_soundManager, null))
                {
                    _subscribedView.MasterVolumeChanged -= _soundManager.SetMasterVolume;
                    _subscribedView.BgmVolumeChanged -= _soundManager.SetBgmVolume;
                    _subscribedView.SfxVolumeChanged -= _soundManager.SetSfxVolume;
                    _subscribedView.MuteAllChanged -= _soundManager.SetMuteAll;
                }
            }

            if (!ReferenceEquals(_soundManager, null))
            {
                _soundManager.VolumeSettingsChanged -= RefreshView;
            }

            _isSubscribed = false;
            _subscribedView = null;
        }

        private void SaveSettings()
        {
            if (_soundManager != null) _soundManager.SaveVolumeSettings();
        }

        private void HandleTabSelected(SettingsTab tab) => SaveSettings();

        private void RefreshView()
        {
            if (_soundManager == null || _settingsView == null)
            {
                return;
            }

            _settingsView.MasterVolume = _soundManager.MasterVolume;
            _settingsView.BgmVolume = _soundManager.BgmVolume;
            _settingsView.SfxVolume = _soundManager.SfxVolume;
            _settingsView.IsMuted = _soundManager.IsMuted;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_settingsView == null)
            {
                _settingsView = GetComponent<TitleSettingsView>();
            }
        }
#endif
    }
}
