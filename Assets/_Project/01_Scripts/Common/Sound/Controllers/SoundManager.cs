using System;
using System.Collections.Generic;
using OzGameLab01.Interfaces;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace OzGameLab01.Managers
{
    public enum SoundId
    {
        None = 0, // 사운드 없음

        BgmTitle = 1, // 타이틀 씬 BGM
        BgmBoardDay = 2, // 보드 씬 낮 BGM
        BgmBoardNight = 3, // 보드 씬 밤 BGM
        BgmCombat = 4, // 일반 전투 씬 BGM
        BgmVictory = 5, // 승리 화면 BGM
        BgmDefeat = 6, // 패배 화면 BGM

        UiMouseClick = 7, // 마우스 클릭
        UiButtonClick = 8, // UI 버튼 클릭
        UiButtonHover = 9, // UI 버튼 마우스 오버
        UiConfirm = 10, // 확인 선택
        UiCancel = 11, // 취소 선택
        UiError = 12, // 잘못된 선택 또는 오류 알림
        UiSettingsOpen = 13, // 설정 창 열기
        UiSettingsClose = 14, // 설정 창 닫기

        DiceRoll = 15, // 주사위 굴리기
        PlayerMove = 16, // 플레이어 이동
        MapNodeSelect = 17, // 맵 노드 선택
        UnitPlaced = 18, // 유닛 배치
        UnitRemoved = 19, // 배치한 유닛 회수
        UnitAttack_01 = 20, // 아군 유닛 공격
        UnitAttack_02 = 21, // 아군 유닛 공격
        UnitAttack_03 = 22, // 아군 유닛 공격
        UnitAttack_04 = 23, // 아군 유닛 공격
        UnitAttack_05 = 24, // 아군 유닛 공격
        UnitUseSkill = 25,  // 아군 유닛 스킬 사용
        UnitHit = 26, // 아군 유닛 피격
        UnitDeath = 27, // 아군 유닛 사망
        UnitHeal = 28, // 아군 유닛 회복
        UnitStatModify = 29, // 아군 스탯 버프
        UnitDebuff = 30, // 아군 디버프 스킬

        EnemyAttack = 31, // 적 유닛 공격
        EnemyHit_01 = 32, // 적 유닛 피격
        EnemyHit_02 = 33, // 적 유닛 피격
        EnemyHit_03 = 34, // 적 유닛 피격
        EnemyDeath = 35, // 적 유닛 사망

        RoundStart = 36, // 전투 라운드 시작
        RoundEnd = 37, // 전투 라운드 종료
        CombatVictory = 38, // 전투 승리 효과음
        CombatDefeat = 39, // 전투 패배 효과음
        RelicGain = 40, // 유물 획득
        ItemGain = 41, // 아이템 획득
        SceneTransition = 42, // 씬 전환

        BoardTurnEnd = 43, // 보드 턴 종료
        BoardPlayerMove_0 = 44, // 보드 플레이어 한 칸 이동 0
        BoardPlayerMove_1 = 45, // 보드 플레이어 한 칸 이동 1
        BoardPlayerMove_2 = 46, // 보드 플레이어 한 칸 이동 2
        BoardPlayerMove_3 = 47, // 보드 플레이어 한 칸 이동 3
        BoardDayChange = 48, // 보드 낮/밤 전환
        BoardDiceRoll_0 = 49, // 보드 주사위 굴리기 0
        BoardDiceRoll_1 = 50, // 보드 주사위 굴리기 1
        BoardDiceRoll_2 = 51, // 보드 주사위 굴리기 2
        BoardDiceRoll_3 = 52, // 보드 주사위 굴리기 3
        BoardGetUnit = 53, // 보드 유닛 획득

        TutorialDialog = 54, // 튜토리얼 가이드 닫기
        TutorialEmphasize = 55, // 튜토리얼 강조 시작
        UnitPositionMoveSucceeded = 56, // 유닛 배치/배치 해제 성공
        UnitPositionMoveFailed = 57, // 유닛 배치/배치 해제 실패
        CombatBattleBegin = 58, // 전투 정보 UI가 닫히고 실제 전투 시작
        CombatStatIncrease = 59, // 스킬로 인한 스탯 증가
        CombatStatDecrease = 60, // 스킬로 인한 스탯 감소
        CombatStatusEffect = 61, // 비능력치 상태이상 스킬 사용
        BoardDiceButton = 62, // 보드 주사위 버튼 클릭
        EventOpen = 63, // 이벤트 UI 열기
        EventChoice = 64, // 이벤트 선택지 Burn 연출 시작
        UnitPublicHit_0 = 65, // 피터팬 기본공격 명중
        UnitPublicHit_1 = 66, // 기본 유닛 기본공격 명중
        UnitPublicHit_2 = 67, // 앨리스/지니/사자/허수아비 기본공격 명중
        UnitSkillAlice = 68, // 앨리스 액티브 스킬 적중
        UnitSkillDorothy = 69, // 도로시 액티브 스킬 적중
        UnitSkillLionBarrier = 70, // 사자 보호막 스킬 발동
        UnitSkillLionTaunt = 71, // 사자 도발 스킬 발동
        UnitSkillScarecrow = 72, // 허수아비 액티브 스킬 적중
        UnitStatusDot = 73, // 지속 피해 상태이상 적용
        UnitStatusSilence = 74, // 침묵 상태이상 적용
        UnitStatusStun = 75, // 기절 상태이상 적용
        UiButtonClickAlt = 76, // 공용 UI 버튼 클릭 대체음
        UiGameClear = 77 // 최종 게임 클리어 결과 UI
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
    public sealed class SoundManager : Singleton<SoundManager>, IGameManager
    {
        private const string MASTER_VOLUME_KEY = "Audio.MasterVolume";
        private const string BGM_VOLUME_KEY = "Audio.BgmVolume";
        private const string SFX_VOLUME_KEY = "Audio.SfxVolume";
        private const string MUTE_ALL_KEY = "Audio.MuteAll";
        private const string TITLE_SCENE_NAME = "01_Title";
        private const string COMBAT_SCENE_NAME = "03_Combat";

        private const float DEFAULT_VOLUME = 1f;
        private const float SAVE_DELAY_SECONDS = 0.5f;
        private bool _volumeSettingsDirty;
        private float _saveAt;
        private float _currentBgmVolume = 1f;

        [Header("Sound Library")]
        [Tooltip("SoundId별 AudioClip과 채널을 등록합니다. 음원이 없는 항목은 비워둘 수 있습니다.")]
        [SerializeField] private List<SoundEntry> sounds = new();

        [Header("Runtime Sources")]
        [SerializeField] private AudioSource bgmSource;
        [SerializeField] private AudioSource sfxSource;

        private readonly Dictionary<SoundId, SoundEntry> _soundLookup = new();
        private readonly HashSet<SoundId> _missingSoundWarnings = new();

        public bool IsInitialized { get; private set; }
        public float MasterVolume { get; private set; } = DEFAULT_VOLUME;
        public float BgmVolume { get; private set; } = DEFAULT_VOLUME;
        public float SfxVolume { get; private set; } = DEFAULT_VOLUME;
        public bool IsMuted { get; private set; }

        public event Action VolumeSettingsChanged;

        protected override void Awake()
        {
            base.Awake();
        }

        private void Update()
        {
            if (_volumeSettingsDirty && Time.unscaledTime >= _saveAt)
                SaveVolumeSettings();
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) SaveVolumeSettings();
        }

        protected override void OnApplicationQuit()
        {
            base.OnApplicationQuit();
            SaveVolumeSettings();
        }

        /// <summary>
        /// 보류된 변경이 있을 때만 디스크에 저장합니다.
        /// </summary>
        public void SaveVolumeSettings()
        {
            if (!_volumeSettingsDirty) return;
            PlayerPrefs.Save();
            _volumeSettingsDirty = false;
        }

        /// <summary>
        /// 저장된 오디오 설정을 삭제하고 기본값으로 되돌립니다.
        /// </summary>
        public void ResetVolumeSettings()
        {
            PlayerPrefs.DeleteKey(MASTER_VOLUME_KEY);
            PlayerPrefs.DeleteKey(BGM_VOLUME_KEY);
            PlayerPrefs.DeleteKey(SFX_VOLUME_KEY);
            PlayerPrefs.DeleteKey(MUTE_ALL_KEY);

            MasterVolume = DEFAULT_VOLUME;
            BgmVolume = DEFAULT_VOLUME;
            SfxVolume = DEFAULT_VOLUME;
            IsMuted = false;

            _volumeSettingsDirty = false;

            ApplySourceVolumes();
            PlayerPrefs.Save();

            VolumeSettingsChanged?.Invoke();

            Debug.Log("[SoundManager] 오디오 설정 초기화 완료", this);
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            SaveVolumeSettings();
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

            SceneManager.sceneLoaded += HandleSceneLoaded; // 기본 BGM 확인 

            PlaySceneBgm(SceneManager.GetActiveScene()); // 씬이 이미 로드된 경우에도 BGM을 재생

            Debug.Log($"[SoundManager] 초기화 완료 | 등록 사운드: {_soundLookup.Count}", this);
        }

        public void Shutdown()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;

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

            _soundLookup.Clear();
            _missingSoundWarnings.Clear();
            IsInitialized = false;
        }

        /// <summary>
        /// 씬이 로드되면 해당 씬의 기본 BGM을 재생합니다.
        /// 보드 씬 BGM은 낮/밤 상태에 따라 별도로 처리합니다.
        /// </summary>
        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            PlaySceneBgm(scene);
        }

        /// <summary>
        /// 타이틀과 전투 씬의 기본 BGM을 재생합니다.
        /// </summary>
        private void PlaySceneBgm(Scene scene)
        {
            switch (scene.name)
            {
                case TITLE_SCENE_NAME:
                    PlayBgm(SoundId.BgmTitle);
                    break;

                case COMBAT_SCENE_NAME:
                    PlayBgm(SoundId.BgmCombat);
                    break;
            }
        }

        public void PlayBgm(SoundId id, bool restart = false)
        {
            if (!TryGetSound(id, SoundChannel.Bgm, out SoundEntry entry))
            {
                return;
            }

            _currentBgmVolume = entry.Volume;
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

        /// <summary>
        /// 외부 컴포넌트에서 지정한 효과음 클립을 재생합니다.
        /// </summary>
        public void PlaySfx(AudioClip clip, float volume = 1f)
        {
            if (clip == null)
            {
                return;
            }

            if (!IsInitialized)
            {
                Initialize();
            }

            sfxSource.PlayOneShot(clip, Mathf.Clamp01(volume));
        }

        public void SetMasterVolume(float value)
        {
            if (!IsInitialized) Initialize();
            if (MasterVolume == Mathf.Clamp01(value)) return;
            MasterVolume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(MASTER_VOLUME_KEY, MasterVolume);
            CompleteVolumeChange();
        }

        public void SetBgmVolume(float value)
        {
            if (!IsInitialized) Initialize();
            if (BgmVolume == Mathf.Clamp01(value)) return;
            BgmVolume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(BGM_VOLUME_KEY, BgmVolume);
            CompleteVolumeChange();
        }

        public void SetSfxVolume(float value)
        {
            if (!IsInitialized) Initialize();
            if (SfxVolume == Mathf.Clamp01(value)) return;
            SfxVolume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(SFX_VOLUME_KEY, SfxVolume);
            CompleteVolumeChange();
        }

        public void SetMuteAll(bool isMuted)
        {
            if (!IsInitialized) Initialize();
            if (IsMuted == isMuted) return;
            IsMuted = isMuted;
            PlayerPrefs.SetInt(MUTE_ALL_KEY, IsMuted ? 1 : 0);
            CompleteVolumeChange();
        }

        private void BuildSoundLookup()
        {
            _soundLookup.Clear();
            _missingSoundWarnings.Clear();

            foreach (SoundEntry entry in sounds)
            {
                if (entry == null || entry.Id == SoundId.None)
                {
                    continue;
                }

                if (!_soundLookup.TryAdd(entry.Id, entry))
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
            MasterVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(MASTER_VOLUME_KEY, DEFAULT_VOLUME));
            BgmVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(BGM_VOLUME_KEY, DEFAULT_VOLUME));
            SfxVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(SFX_VOLUME_KEY, DEFAULT_VOLUME));
            IsMuted = PlayerPrefs.GetInt(MUTE_ALL_KEY, 0) == 1;
        }

        private void CompleteVolumeChange()
        {
            ApplySourceVolumes();
            _volumeSettingsDirty = true;
            _saveAt = Time.unscaledTime + SAVE_DELAY_SECONDS;
            VolumeSettingsChanged?.Invoke();
        }

        private void ApplySourceVolumes()
        {
            if (bgmSource != null)
            {
                bgmSource.volume = GetChannelVolume(SoundChannel.Bgm) * _currentBgmVolume;
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

            if (_soundLookup.TryGetValue(id, out entry) && entry.Channel == expectedChannel)
            {
                // 음원은 나중에 연결할 수 있습니다. 빈 항목은 건너뜁니다.
                return entry.Clip != null;
            }

            if (id == SoundId.None) return false;

            if (_missingSoundWarnings.Add(id))
            {
                Debug.LogWarning($"[SoundManager] {expectedChannel} 사운드가 등록되지 않았습니다: {id}", this);
            }

            entry = null;
            return false;
        }

    }
}
