using OzGameLab01.Interfaces;
using System;
using System.Collections.Generic;
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

        // ScreenModes 배열의 인덱스와 일치해야 합니다.
        private const int FullscreenModeIndex = 0; // 전체 화면 - 테두리 없는 창, 모니터 해상도로 고정
        private const int WindowedModeIndex = 1;    // 창 모드 - 해상도 드롭다운으로 크기 지정

        private const int DefaultScreenModeIndex = FullscreenModeIndex;
        private const int MinResolutionWidth = 1280;
        private const int MinResolutionHeight = 720;

        // Screen.resolutions가 비어있는 극단적 환경(헤드리스 등)을 위한 폴백.
        private static readonly (int Width, int Height)[] FallbackResolutions =
        {
            (1280, 720),
            (1600, 900),
            (1920, 1080)
        };

        // FullscreenModeIndex/WindowedModeIndex와 순서가 일치해야 합니다.
        // ExclusiveFullScreen은 일부 환경(멀티모니터/원격 데스크톱 등)에서 DXGI 전환 자체가
        // 실패해 조용히 FullScreenWindow로 강등되는 사례가 실측 빌드에서 확인되어 사용하지 않습니다.
        private static readonly FullScreenMode[] ScreenModes =
        {
            FullScreenMode.FullScreenWindow,
            FullScreenMode.Windowed
        };

        private readonly List<(int Width, int Height)> _resolutions = new();

        public bool IsInitialized { get; private set; }
        public int ResolutionIndex { get; private set; }
        public int ScreenModeIndex { get; private set; } = DefaultScreenModeIndex;

        /// <summary>모니터의 현재(네이티브) 해상도와 일치하는 <see cref="AvailableResolutions"/> 인덱스.</summary>
        public int NativeResolutionIndex { get; private set; }

        /// <summary>실행 중인 모니터에서 실제로 선택 가능한 해상도 목록(오름차순, 중복 제거).</summary>
        public IReadOnlyList<(int Width, int Height)> AvailableResolutions => _resolutions;

        /// <summary>
        /// 해상도 드롭다운에 표시할 인덱스. 전체 화면은 모니터 해상도로 고정되므로,
        /// 전체 화면 상태에서는 실제 저장된 ResolutionIndex 대신 이 값을 보여줍니다.
        /// </summary>
        public int DisplayedResolutionIndex =>
            ScreenModeIndex == FullscreenModeIndex ? NativeResolutionIndex : ResolutionIndex;

        /// <summary>
        /// 해상도 또는 화면 모드가 바뀌면 발생합니다. 전체 화면 상태에서 다른 해상도를
        /// 선택하면 창 모드로 자동 전환되는 것처럼 두 값이 한 번에 바뀔 수 있어,
        /// View가 두 드롭다운을 함께 갱신할 수 있도록 별도 이벤트로 둡니다.
        /// </summary>
        public event Action VideoSettingsChanged;

        public void Initialize()
        {
            if (IsInitialized)
            {
                return;
            }

            BuildResolutionList();

            ResolutionIndex = Mathf.Clamp(
                PlayerPrefs.GetInt(ResolutionIndexKey, NativeResolutionIndex), 0, _resolutions.Count - 1);
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

        /// <summary>
        /// 해상도 드롭다운 선택을 반영합니다. 전체 화면은 모니터 해상도로 고정이므로,
        /// 전체 화면 상태에서 모니터 해상도가 아닌 값을 고르면 창 모드로 자동 전환됩니다.
        /// </summary>
        public void SetResolutionIndex(int index)
        {
            if (!IsInitialized) Initialize();

            index = Mathf.Clamp(index, 0, _resolutions.Count - 1);

            bool resolutionChanged = ResolutionIndex != index;
            bool isFullscreen = ScreenModeIndex == FullscreenModeIndex;
            bool forceWindowed = isFullscreen && index != NativeResolutionIndex;

            if (!resolutionChanged && !forceWindowed) return;

            ResolutionIndex = index;
            if (forceWindowed)
            {
                ScreenModeIndex = WindowedModeIndex;
            }

            SaveDisplaySettings();
            ApplyDisplaySettings();
            VideoSettingsChanged?.Invoke();
        }

        public void SetScreenModeIndex(int index)
        {
            if (!IsInitialized) Initialize();

            index = Mathf.Clamp(index, 0, ScreenModes.Length - 1);
            if (ScreenModeIndex == index) return;

            ScreenModeIndex = index;
            SaveDisplaySettings();
            ApplyDisplaySettings();
            VideoSettingsChanged?.Invoke();
        }

        public static string FormatResolutionLabel((int Width, int Height) resolution) =>
            $"{resolution.Width} x {resolution.Height}";

        private void BuildResolutionList()
        {
            _resolutions.Clear();

            var seen = new HashSet<(int, int)>();
            foreach (Resolution r in Screen.resolutions)
            {
                if (r.width < MinResolutionWidth || r.height < MinResolutionHeight) continue;
                if (!seen.Add((r.width, r.height))) continue;
                _resolutions.Add((r.width, r.height));
            }

            _resolutions.Sort((a, b) =>
                a.Width != b.Width ? a.Width.CompareTo(b.Width) : a.Height.CompareTo(b.Height));

            if (_resolutions.Count == 0)
            {
                _resolutions.AddRange(FallbackResolutions);
            }

            Resolution current = Screen.currentResolution;
            int nativeIndex = _resolutions.FindIndex(
                r => r.Width == current.width && r.Height == current.height);
            NativeResolutionIndex = nativeIndex >= 0 ? nativeIndex : _resolutions.Count - 1;
        }

        private void SaveDisplaySettings()
        {
            PlayerPrefs.SetInt(ResolutionIndexKey, ResolutionIndex);
            PlayerPrefs.SetInt(ScreenModeIndexKey, ScreenModeIndex);
            PlayerPrefs.Save();
        }

        private void ApplyDisplaySettings()
        {
            if (ScreenModeIndex == FullscreenModeIndex)
            {
                Resolution native = Screen.currentResolution;
                Screen.SetResolution(native.width, native.height, FullScreenMode.FullScreenWindow);
                return;
            }

            (int width, int height) = _resolutions[ResolutionIndex];
            Screen.SetResolution(width, height, FullScreenMode.Windowed);
        }
    }
}
