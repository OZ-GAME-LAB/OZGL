using System.Collections.Generic;
using OzGameLab01.Managers;
using OzGameLab01.UI.Title;
using UnityEngine;

namespace OzGameLab01.Controllers
{
    /// <summary>
    /// 설정창의 Video 탭 옵션과 전역 DisplayManager를 연결합니다.
    /// 타이틀/보드/전투 등 TitleSettingsView를 사용하는 모든 씬에서 재사용됩니다.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(TitleSettingsView))]
    public sealed class SettingsVideoController : MonoBehaviour
    {
        [SerializeField] private TitleSettingsView _settingsView;

        private DisplayManager _displayManager;
        private bool _isSubscribed;
        private TitleSettingsView _subscribedView;

        private void OnEnable()
        {
            _displayManager = DisplayManager.Instance;
            if (_displayManager == null)
            {
                Debug.LogWarning("[SettingsVideoController] DisplayManager를 찾을 수 없습니다.", this);
                return;
            }

            if (!_displayManager.IsInitialized)
            {
                _displayManager.Initialize();
            }

            Subscribe();
            ApplyResolutionOptions();
            RefreshView();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void Subscribe()
        {
            if (_isSubscribed || _settingsView == null)
            {
                return;
            }

            _subscribedView = _settingsView;
            _settingsView.ResolutionSelected += HandleResolutionSelected;
            _settingsView.ScreenModeSelected += HandleScreenModeSelected;
            _displayManager.VideoSettingsChanged += RefreshView;
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
                _subscribedView.ResolutionSelected -= HandleResolutionSelected;
                _subscribedView.ScreenModeSelected -= HandleScreenModeSelected;
            }

            if (_displayManager != null)
            {
                _displayManager.VideoSettingsChanged -= RefreshView;
            }

            _isSubscribed = false;
            _subscribedView = null;
        }

        private void HandleResolutionSelected(int index)
        {
            _displayManager.SetResolutionIndex(index);
        }

        private void HandleScreenModeSelected(int index)
        {
            _displayManager.SetScreenModeIndex(index);
        }

        private void ApplyResolutionOptions()
        {
            if (_displayManager == null || _settingsView == null)
            {
                return;
            }

            var labels = new List<string>(_displayManager.AvailableResolutions.Count);
            foreach ((int Width, int Height) resolution in _displayManager.AvailableResolutions)
            {
                labels.Add(DisplayManager.FormatResolutionLabel(resolution));
            }

            _settingsView.SetResolutionOptions(labels);
        }

        private void RefreshView()
        {
            if (_displayManager == null || _settingsView == null)
            {
                return;
            }

            // 전체 화면은 모니터 해상도로 고정되므로 실제 저장값이 아닌
            // DisplayedResolutionIndex(전체 화면이면 모니터 해상도)를 보여줍니다.
            _settingsView.ResolutionIndex = _displayManager.DisplayedResolutionIndex;
            _settingsView.ScreenModeIndex = _displayManager.ScreenModeIndex;
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
