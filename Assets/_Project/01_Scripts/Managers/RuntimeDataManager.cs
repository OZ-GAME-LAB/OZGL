using System.Collections.Generic;
using UnityEngine;
using OzGameLab01.Data;

namespace OzGameLab01.Managers
{
    /// <summary>
    /// GameDataLoader가 역직렬화한 Unit/Enemy/Synergy 데이터를 id 기준으로 캐싱해
    /// CombatManager/MapManager/PlayerInventoryManager 등 게임 로직에 노출합니다.
    /// ContentCatalog/Repository 계층(Docs/BACKEND_DATA_ARCHITECTURE.md)이 들어오기 전까지의
    /// 임시 경로이며, GameDB/UnitRosterData 등 기존 경로는 건드리지 않습니다.
    /// </summary>
    public sealed class RuntimeDataManager : Singleton<RuntimeDataManager>
    {
        private readonly Dictionary<int, UnitData> _unitsById = new Dictionary<int, UnitData>();
        private readonly Dictionary<int, MonsterData> _enemiesById = new Dictionary<int, MonsterData>();
        private readonly Dictionary<int, SynergyData> _synergiesById = new Dictionary<int, SynergyData>();

        public IReadOnlyDictionary<int, UnitData> Units => _unitsById;
        public IReadOnlyDictionary<int, MonsterData> Enemies => _enemiesById;
        public IReadOnlyDictionary<int, SynergyData> Synergies => _synergiesById;

        protected override void Awake()
        {
            base.Awake();
            LoadAll();
        }

        /// <summary>
        /// Resources JSON을 다시 읽어 캐시를 갱신합니다. 보통 Awake()에서 한 번이면 충분하지만,
        /// 에디터에서 데이터 리소스를 갱신한 뒤 재로드가 필요할 때를 위해 공개해둡니다.
        /// </summary>
        public void LoadAll()
        {
            Fill(_unitsById, GameDataLoader.LoadUnits(), unit => unit.id);
            Fill(_enemiesById, GameDataLoader.LoadEnemies(), enemy => enemy.id);
            Fill(_synergiesById, GameDataLoader.LoadSynergies(), synergy => synergy.id);
        }

        public UnitData GetUnit(int id)
        {
            _unitsById.TryGetValue(id, out UnitData data);
            return data;
        }

        public MonsterData GetEnemy(int id)
        {
            _enemiesById.TryGetValue(id, out MonsterData data);
            return data;
        }

        public SynergyData GetSynergy(int id)
        {
            _synergiesById.TryGetValue(id, out SynergyData data);
            return data;
        }

        private static void Fill<T>(Dictionary<int, T> target, List<T> source, System.Func<T, int> idSelector)
        {
            target.Clear();
            if (source == null)
            {
                return;
            }

            foreach (T item in source)
            {
                if (item != null)
                {
                    target[idSelector(item)] = item;
                }
            }
        }
    }
}
