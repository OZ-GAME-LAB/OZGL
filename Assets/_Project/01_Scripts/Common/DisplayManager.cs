using OzGameLab01.Interfaces;
using UnityEngine;

namespace OzGameLab01.Managers
{
    /// <summary>
    /// 해상도와 화면 모드 설정을 관리합니다. PlayerPrefs에 저장하며,
    /// GameBootstrapper가 부팅 시점에 초기화해 타이틀 화면 진입 전에 적용합니다.
    /// </summary>
    public sealed class DisplayManager : Singleton<DisplayManager>, IGameManager
    {
        private const string ResolutionIndexKey = "Display.ResolutionIndex";
        private const string ScreenModeIndexKey = "Display.ScreenModeIndex";

        private const int DefaultResolutionIndex = 2; // 1920 x 1080
        private const int DefaultScreenModeIndex = 0; // 전체 화면

        // TitleSettingsView의 해상도/화면 모드 드롭다운 옵션 순서와 반드시 일치해야 합니다.
        private static readonly (int Width, int Height)[] Resolutions =
        {
            (1280, 720),
            (1600, 900),
            (1920, 1080)
        };

        private static readonly FullScreenMode[] ScreenModes =
        {
            FullScreenMode.ExclusiveFullScreen,
            FullScreenMode.Windowed
        };

        public bool IsInitialized { get; private set; }
        public int ResolutionIndex { get; private set; } = DefaultResolutionIndex;
        public int ScreenModeIndex { get; private set; } = DefaultScreenModeIndex;

        public void Initialize()
        {
            if (IsInitialized)
            {
                return;
            }

            ResolutionIndex = Mathf.Clamp(
                PlayerPrefs.GetInt(ResolutionIndexKey, DefaultResolutionIndex), 0, Resolutions.Length - 1);
            ScreenModeIndex = Mathf.Clamp(
                PlayerPrefs.GetInt(ScreenModeIndexKey, DefaultScreenModeIndex), 0, ScreenModes.Length - 1);

            ApplyDisplaySettings();

            IsInitialized = true;
            Debug.Log($"[DisplayManager] 초기화 완료 | 해상도 인덱스: {ResolutionIndex}, 화면 모드 인덱스: {ScreenModeIndex}", this);
        }

        public void Shutdown()
        {
            IsInitialized = false;
        }

        public void SetResolutionIndex(int index)
        {
            if (!IsInitialized) Initialize();

            index = Mathf.Clamp(index, 0, Resolutions.Length - 1);
            if (ResolutionIndex == index) return;

            ResolutionIndex = index;
            PlayerPrefs.SetInt(ResolutionIndexKey, ResolutionIndex);
            PlayerPrefs.Save();
            ApplyDisplaySettings();
        }

        public void SetScreenModeIndex(int index)
        {
            if (!IsInitialized) Initialize();

            index = Mathf.Clamp(index, 0, ScreenModes.Length - 1);
            if (ScreenModeIndex == index) return;

            ScreenModeIndex = index;
            PlayerPrefs.SetInt(ScreenModeIndexKey, ScreenModeIndex);
            PlayerPrefs.Save();
            ApplyDisplaySettings();
        }

        private void ApplyDisplaySettings()
        {
            (int width, int height) = Resolutions[ResolutionIndex];
            Screen.SetResolution(width, height, ScreenModes[ScreenModeIndex]);
        }
    }
}
