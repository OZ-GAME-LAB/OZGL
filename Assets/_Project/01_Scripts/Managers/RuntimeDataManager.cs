using System.Collections.Generic;
using OzGameLab01.Data;
using OzGameLab01.Combat;

namespace OzGameLab01.Managers
{
    /// <summary>
    /// CombatManager/MapManager/PlayerInventoryManager 등이 유닛·적·시너지 데이터를 가져가는
    /// 단일 진입점입니다. Unit/Enemy는 다시 파싱하지 않고 이미 로드되어 있는
    /// UnitRosterData.Active/MonsterRosterData.Active를 그대로 참조합니다 — 두 asset이
    /// 각자 OnEnable()에서 TempUnitData.json/EnemyData.json을 읽어 캐싱해두는 값을 그대로 쓰는
    /// 것이라, 씬에 그 asset들이 로드된 이후에만 값이 채워집니다. Synergy는 아직 대응하는
    /// 라이브 로스터가 없어 GameDataLoader로 직접 캐싱합니다.
    /// </summary>
    public sealed class RuntimeDataManager : Singleton<RuntimeDataManager>
    {
        private static readonly List<UnitData> EmptyUnits = new List<UnitData>();
        private static readonly List<MonsterData> EmptyEnemies = new List<MonsterData>();

        private readonly Dictionary<int, SynergyData> _synergiesById = new Dictionary<int, SynergyData>();

        public IReadOnlyList<UnitData> Units => UnitRosterData.Active?.UnitStats ?? EmptyUnits;
        public IReadOnlyList<MonsterData> Enemies => MonsterRosterData.Active?.MonsterStats ?? EmptyEnemies;
        public IReadOnlyDictionary<int, SynergyData> Synergies => _synergiesById;

        protected override void Awake()
        {
            base.Awake();
            LoadSynergies();
        }

        /// <summary>
        /// Synergy 캐시를 다시 읽습니다. Unit/Enemy는 UnitRosterData/MonsterRosterData가 이미
        /// 자체적으로 갱신을 관리하므로 여기서 다시 로드할 필요가 없습니다.
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

        public MonsterData GetEnemy(int id) => MonsterRosterData.Active?.GetById(id);

        public SynergyData GetSynergy(int id)
        {
            _synergiesById.TryGetValue(id, out SynergyData data);
            return data;
        }
    }
}
