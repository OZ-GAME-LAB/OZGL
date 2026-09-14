using System.Collections.Generic;
using UnityEngine;
using OzGameLab01.Data;
using OzGameLab01.Combat;

namespace OzGameLab01.Managers
{
    /// <summary>
    /// CombatManager/BoardSceneController/PlayerInventoryManager 등 게임 로직이 유닛·적·시너지
    /// 데이터를 가져가는 단일 진입점입니다. UnitRosterData/MonsterRosterData를 Resources.Load로
    /// 직접 불러와 등록하므로, CombatManager나 UnitFormationController처럼 씬의 다른 컴포넌트가
    /// 먼저 활성화되어 RegisterActive를 호출해줄 때까지 기다릴 필요가 없습니다 — 비활성 UI
    /// 패널(예: 유닛 편성 화면)에 붙어 있어 Start()가 안 도는 컴포넌트에 데이터 등록을
    /// 의존했다가 유닛 획득이 막히는 문제가 있었습니다. Synergy는 아직 대응하는 라이브
    /// 로스터가 없어 GameDataLoader로 직접 캐싱합니다.
    /// </summary>
    public sealed class RuntimeDataManager : Singleton<RuntimeDataManager>
    {
        private static readonly List<UnitData> EmptyUnits = new List<UnitData>();
        private static readonly List<MonsterData> EmptyEnemies = new List<MonsterData>();

        private UnitRosterData _unitRosterData;
        private MonsterRosterData _monsterRosterData;
        private readonly Dictionary<int, SynergyData> _synergiesById = new Dictionary<int, SynergyData>();
        private readonly Dictionary<int, RelicData> _relicsById = new Dictionary<int, RelicData>();

        public IReadOnlyList<UnitData> Units => _unitRosterData != null ? _unitRosterData.UnitStats : EmptyUnits;
        public IReadOnlyList<MonsterData> Enemies => _monsterRosterData != null ? _monsterRosterData.MonsterStats : EmptyEnemies;
        public IReadOnlyDictionary<int, SynergyData> Synergies => _synergiesById;
        public IReadOnlyDictionary<int, RelicData> Relics => _relicsById;

        protected override void Awake()
        {
            base.Awake();
            LoadRosters();
            LoadSynergies();
            LoadRelics();
        }

        /// <summary>
        /// Resources/UnitRosterData, Resources/MonsterRosterData를 로드합니다. Resources.Load
        /// 자체가 각 asset의 OnEnable()을 트리거해 JSON(TempUnitData/EnemyData) 역직렬화까지
        /// 끝마칩니다.
        /// </summary>
        public void LoadRosters()
        {
            _unitRosterData = Resources.Load<UnitRosterData>("UnitRosterData");
            if (_unitRosterData != null)
            {
                UnitRosterData.RegisterActive(_unitRosterData, this);
            }
            else
            {
                Debug.LogWarning("[RuntimeDataManager] Resources/UnitRosterData.asset을 찾을 수 없습니다.", this);
            }

            _monsterRosterData = Resources.Load<MonsterRosterData>("MonsterRosterData");
            if (_monsterRosterData == null)
            {
                Debug.LogWarning("[RuntimeDataManager] Resources/MonsterRosterData.asset을 찾을 수 없습니다.", this);
            }
        }

        /// <summary>
        /// Synergy 캐시를 다시 읽습니다.
        /// </summary>
        public void LoadSynergies()
        {
            _synergiesById.Clear();
            List<SynergyData> loaded = GameDataLoader.LoadSynergies();
            if (loaded == null)
            {
                return;
            }

            foreach (SynergyData synergy in loaded)
            {
                if (synergy != null)
                {
                    _synergiesById[synergy.id] = synergy;
                }
            }
        }

        public UnitData GetUnit(int id)
        {
            IReadOnlyList<UnitData> units = Units;
            for (int i = 0; i < units.Count; i++)
            {
                if (units[i] != null && units[i].id == id)
                {
                    return units[i];
                }
            }

            return null;
        }

        public MonsterData GetEnemy(int id) => _monsterRosterData?.GetById(id);

        /// <summary>
        /// 몬스터 스킬(200번대)을 먼저 찾고 없으면 유닛 스킬(900번대, 훔친 액티브 포함)을
        /// 찾습니다. Unit.ConfigureEnemy()가 쓰던 두 로스터 순차 조회와 동일한 순서입니다.
        /// </summary>
        public SkillData GetSkill(int id)
        {
            SkillData monsterSkill = _monsterRosterData?.GetSkill(id);
            if (monsterSkill != null)
            {
                return monsterSkill;
            }

            return _unitRosterData?.GetSkill(id);
        }

        public SynergyData GetSynergy(int id)
        {
            _synergiesById.TryGetValue(id, out SynergyData data);
            return data;
        }

        /// <summary>
        /// 유물 캐시를 다시 읽습니다. RelicData.xlsx를 옮긴 Resources/RelicData.json을
        /// 직접 로드합니다(DataManager.Relics는 Addressables 주소가 비어 있고
        /// LoadAllDatabase()도 호출되지 않아 항상 비어 있으므로 쓰지 않습니다).
        /// </summary>
        public void LoadRelics()
        {
            _relicsById.Clear();
            List<RelicData> loaded = GameDataLoader.LoadRelics();
            if (loaded == null)
            {
                return;
            }

            foreach (RelicData relic in loaded)
            {
                if (relic != null)
                {
                    _relicsById[relic.id] = relic;
                }
            }
        }

        public RelicData GetRelic(int id)
        {
            _relicsById.TryGetValue(id, out RelicData data);
            return data;
        }

        /// <summary>
        /// 특정 트리거에 연결된 패시브(유닛)/유물 효과 목록입니다. 실제 캐시 구성은
        /// RuntimeEffectManager가 담당하고(플레이어 보유 상태를 읽어야 해서), 여기서는
        /// 전투 로직(CombatEffectExecutor)이 RuntimeDataManager 하나만 참조해도 되도록
        /// 위임만 합니다.
        /// </summary>
        public IReadOnlyList<RuntimeEffectManager.EffectSource> GetEffects(TriggerType trigger)
        {
            return RuntimeEffectManager.Instance.GetEffects(trigger);
        }
    }
}
