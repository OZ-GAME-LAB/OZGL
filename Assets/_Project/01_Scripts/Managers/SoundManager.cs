using System;
using System.Collections.Generic;
using OzGameLab01.Interfaces;
using UnityEngine;

namespace OzGameLab01.Managers
{
    public enum SoundId
    {
        None = 0, // 사운드 없음

        BgmTitle, // 타이틀 씬 BGM
        BgmBoard, // 보드 씬 BGM
        BgmCombat, // 일반 전투 씬 BGM
        BgmVictory, // 승리 화면 BGM
        BgmDefeat, // 패배 화면 BGM

        UiMouseClick, // 마우스 클릭
        UiButtonClick, // UI 버튼 클릭
        UiButtonHover, // UI 버튼 마우스 오버
        UiConfirm, // 확인 선택
        UiCancel, // 취소 선택
        UiError, // 잘못된 선택 또는 오류 알림
        UiSettingsOpen, // 설정 창 열기
        UiSettingsClose, // 설정 창 닫기

        DiceRoll, // 주사위 굴리기
        PlayerMove, // 플레이어 이동
        MapNodeSelect, // 맵 노드 선택
        UnitPlaced, // 유닛 배치
        UnitRemoved, // 배치한 유닛 회수
        UnitAttack, // 아군 유닛 공격
        UnitHit, // 아군 유닛 피격
        UnitDeath, // 아군 유닛 사망
        EnemyAttack, // 적 유닛 공격
        EnemyHit, // 적 유닛 피격
        EnemyDeath, // 적 유닛 사망
        RoundStart, // 전투 라운드 시작
        RoundEnd, // 전투 라운드 종료
        CombatVictory, // 전투 승리 효과음
        CombatDefeat, // 전투 패배 효과음
        RelicGain, // 유물 획득
        ItemGain, // 아이템 획득
        SceneTransition // 씬 전환
    }

    public enum SoundChannel
    {
        Bgm,
        Sfx
    }

    [Serializable]
    public sealed class SoundEntry
    {
        [SerializeField] private SoundId id;
        [SerializeField] private SoundChannel channel = SoundChannel.Sfx;
        [SerializeField] private AudioClip clip;
        [SerializeField, Range(0f, 1f)] private float volume = 1f;

        public SoundId Id => id;
        public SoundChannel Channel => channel;
        public AudioClip Clip => clip;
        public float Volume => volume;
    }

    /// <summary>
    /// 게임 전체에서 BGM과 효과음을 재생하고 볼륨 설정을 보관합니다.
    /// GlobalManagers 프리팹에 포함되며 GameBootstrapper와 함께 씬 전환 후에도 유지됩니다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SoundManager : MonoBehaviour, IGameManager
    {
        private const string MasterVolumeKey = "Audio.MasterVolume";
        private const string BgmVolumeKey = "Audio.BgmVolume";
        private const string SfxVolumeKey = "Audio.SfxVolume";
        private const string MuteAllKey = "Audio.MuteAll";

        private const float DefaultVolume = 1f;
        private const float SaveDelaySeconds = 0.5f;
        private bool volumeSettingsDirty;
        private float saveAt;
        private float currentBgmVolume = 1f;

        private static SoundManager instance;

        [Header("Sound Library")]
        [Tooltip("SoundId별 AudioClip과 채널을 등록합니다. 음원이 없는 항목은 비워둘 수 있습니다.")]
        [SerializeField] private List<SoundEntry> sounds = new();

        [Header("Runtime Sources")]
        [SerializeField] private AudioSource bgmSource;
        [SerializeField] private AudioSource sfxSource;

        private readonly Dictionary<SoundId, SoundEntry> soundLookup = new();
        private readonly HashSet<SoundId> missingSoundWarnings = new();

        public static SoundManager Instance => instance != null
            ? instance
            : FindFirstObjectByType<SoundManager>(FindObjectsInactive.Include);

        public bool IsInitialized { get; private set; }
        public float MasterVolume { get; private set; } = DefaultVolume;
        public float BgmVolume { get; private set; } = DefaultVolume;
        public float SfxVolume { get; private set; } = DefaultVolume;
        public bool IsMuted { get; private set; }

        public event Action VolumeSettingsChanged;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Debug.LogWarning("[SoundManager] 이미 유지 중인 인스턴스가 있어 중복 컴포넌트를 비활성화합니다.", this);
                enabled = false;
                return;
            }

            instance = this;
        }

        private void Update()
        {
            if (volumeSettingsDirty && Time.unscaledTime >= saveAt)
                SaveVolumeSettings();
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) SaveVolumeSettings();
        }

        private void OnApplicationQuit() => SaveVolumeSettings();

        /// <summary>
        /// 보류된 변경이 있을 때만 디스크에 저장합니다.
        /// </summary>
        public void SaveVolumeSettings()
        {
            if (!volumeSettingsDirty) return;
            PlayerPrefs.Save();
            volumeSettingsDirty = false;
        }

        private void OnDestroy()
        {
            SaveVolumeSettings();
            if (instance == this)
            {
                instance = null;
            }
        }

        public void Initialize()
        {
            if (IsInitialized)
            {
                return;
            }

            BuildSoundLookup();
            EnsureAudioSources();
            LoadVolumeSettings();
            ApplySourceVolumes();

            IsInitialized = true;
            Debug.Log($"[SoundManager] 초기화 완료 | 등록 사운드: {soundLookup.Count}", this);
        }

        public void Shutdown()
        {
            SaveVolumeSettings();
            if (!IsInitialized)
            {
                return;
            }

            if (bgmSource != null)
            {
                bgmSource.Stop();
            }

            if (sfxSource != null)
            {
                sfxSource.Stop();
            }

            soundLookup.Clear();
            missingSoundWarnings.Clear();
            IsInitialized = false;
        }

        public void PlayBgm(SoundId id, bool restart = false)
        {
            if (!TryGetSound(id, SoundChannel.Bgm, out SoundEntry entry))
            {
                return;
            }

            currentBgmVolume = entry.Volume;
            ApplySourceVolumes();
            if (!restart && bgmSource.isPlaying && bgmSource.clip == entry.Clip)
            {
                return;
            }

            bgmSource.clip = entry.Clip;
            bgmSource.loop = true;
            bgmSource.volume = GetChannelVolume(SoundChannel.Bgm) * entry.Volume;
            bgmSource.Play();
        }

        public void StopBgm()
        {
            if (bgmSource == null)
            {
                return;
            }

            bgmSource.Stop();
            bgmSource.clip = null;
        }

        public void PlaySfx(SoundId id)
        {
            if (!TryGetSound(id, SoundChannel.Sfx, out SoundEntry entry))
            {
                return;
            }

            sfxSource.PlayOneShot(entry.Clip, entry.Volume);
        }

        public void SetMasterVolume(float value)
        {
            if (!IsInitialized) Initialize();
            if (MasterVolume == Mathf.Clamp01(value)) return;
            MasterVolume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(MasterVolumeKey, MasterVolume);
            CompleteVolumeChange();
        }

        public void SetBgmVolume(float value)
        {
            if (!IsInitialized) Initialize();
            if (BgmVolume == Mathf.Clamp01(value)) return;
            BgmVolume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(BgmVolumeKey, BgmVolume);
            CompleteVolumeChange();
        }

        public void SetSfxVolume(float value)
        {
            if (!IsInitialized) Initialize();
            if (SfxVolume == Mathf.Clamp01(value)) return;
            SfxVolume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(SfxVolumeKey, SfxVolume);
            CompleteVolumeChange();
        }

        public void SetMuteAll(bool isMuted)
        {
            if (!IsInitialized) Initialize();
            if (IsMuted == isMuted) return;
            IsMuted = isMuted;
            PlayerPrefs.SetInt(MuteAllKey, IsMuted ? 1 : 0);
            CompleteVolumeChange();
        }

        private void BuildSoundLookup()
        {
            soundLookup.Clear();
            missingSoundWarnings.Clear();

            foreach (SoundEntry entry in sounds)
            {
                if (entry == null || entry.Id == SoundId.None)
                {
                    continue;
                }

                if (!soundLookup.TryAdd(entry.Id, entry))
                {
                    Debug.LogWarning($"[SoundManager] 중복 SoundId를 건너뜁니다: {entry.Id}", this);
                }
            }
        }

        private void EnsureAudioSources()
        {
            if (bgmSource == null)
            {
                bgmSource = gameObject.AddComponent<AudioSource>();
            }

            if (sfxSource == null)
            {
                sfxSource = gameObject.AddComponent<AudioSource>();
            }

            ConfigureSource(bgmSource, true);
            ConfigureSource(sfxSource, false);
        }

        private static void ConfigureSource(AudioSource source, bool loop)
        {
            source.playOnAwake = false;
            source.loop = loop;
            source.spatialBlend = 0f;
        }

        private void LoadVolumeSettings()
        {
            MasterVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(MasterVolumeKey, DefaultVolume));
            BgmVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(BgmVolumeKey, DefaultVolume));
            SfxVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(SfxVolumeKey, DefaultVolume));
            IsMuted = PlayerPrefs.GetInt(MuteAllKey, 0) == 1;
        }

        private void CompleteVolumeChange()
        {
            ApplySourceVolumes();
            volumeSettingsDirty = true;
            saveAt = Time.unscaledTime + SaveDelaySeconds;
            VolumeSettingsChanged?.Invoke();
        }

        private void ApplySourceVolumes()
        {
            if (bgmSource != null)
            {
                bgmSource.volume = GetChannelVolume(SoundChannel.Bgm) * currentBgmVolume;
            }

            if (sfxSource != null)
            {
                sfxSource.volume = GetChannelVolume(SoundChannel.Sfx);
            }
        }

        private float GetChannelVolume(SoundChannel channel)
        {
            if (IsMuted)
            {
                return 0f;
            }

            float channelVolume = channel == SoundChannel.Bgm ? BgmVolume : SfxVolume;
            return MasterVolume * channelVolume;
        }

        private bool TryGetSound(SoundId id, SoundChannel expectedChannel, out SoundEntry entry)
        {
            if (!IsInitialized)
            {
                Initialize();
            }

            if (soundLookup.TryGetValue(id, out entry) && entry.Channel == expectedChannel)
            {
                // 음원은 나중에 연결할 수 있습니다. 빈 항목은 건너뜁니다.
                return entry.Clip != null;
            }

            if (id == SoundId.None) return false;

            if (missingSoundWarnings.Add(id))
            {
                Debug.LogWarning($"[SoundManager] {expectedChannel} 사운드가 등록되지 않았습니다: {id}", this);
            }

            entry = null;
            return false;
        }

    }
}
