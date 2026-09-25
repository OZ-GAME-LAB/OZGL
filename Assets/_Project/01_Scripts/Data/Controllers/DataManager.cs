using OzGameLab01.Data;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace OzGameLab01.Managers
{
    /// <summary>
    /// 데이터베이스 구성 및 보관 코어 엔진
    /// - 부트스트래퍼에서 DataManager.InitializeAsync() 실행
    /// - 데이터 조회 메서드(API) 하단에 정리되어 있음
    /// </summary>
    public static class DataManager
    {
        public static GameDB<UnitData, UnitDataList> Units { get; } = new();
        public static GameDB<MonsterData, MonsterDataList> Monsters { get; } = new();
        public static GameDB<EnemySpeciesData, EnemySpeciesDataList> EnemySpecies { get; } = new();
        public static GameDB<RelicData, RelicDataList> Relics { get; } = new();
        public static GameDB<SynergyData, SynergyDataList> Synergies { get; } = new();
        public static GameDB<SkillData, SkillDataList> Skills { get; } = new();

        public static bool IsInitialized { get; private set; }

        // 어드레서블 주소 기반 모든 데이터 비동기 캐싱
        public static async Task InitializeAsync()
        {
            if (IsInitialized) return;

            // 어드레서블 주소 매핑 일원화
            var tasks = new List<Task<bool>>()
            {
                Units.LoadAsync("JSON/UnitJSON"),
                Monsters.LoadAsync("JSON/MonsterJSON"),
                EnemySpecies.LoadAsync("JSON/EnemySpeciesJSON"),
                Relics.LoadAsync("JSON/RelicJSON"),
                Synergies.LoadAsync("JSON/SynergyJSON"),
                Skills.LoadAsync("JSON/SkillDesignJSON")
            };

            bool[] loaded = await Task.WhenAll(tasks);
            if (System.Array.Exists(loaded, success => !success))
                throw new System.InvalidOperationException("One or more gameplay databases failed to load.");
            IsInitialized = true;
            Debug.Log($"[DataManager] Gameplay databases ready: units={Units.Count}, monsters={Monsters.Count}, enemySpecies={EnemySpecies.Count}, " +
                      $"skills={Skills.Count}, synergies={Synergies.Count}, relics={Relics.Count}.");
        }

        // 각 도메인 조회 편의 API
        public static UnitData GetUnit(int id) => Units.Get(id);
        public static MonsterData GetMonster(int id) => Monsters.Get(id);
        public static RelicData GetRelic(int id) => Relics.Get(id);
        public static SynergyData GetSynergy(int id) => Synergies.Get(id);
        public static SkillData GetSkill(int id) => Skills.Get(id);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSession()
        {
            IsInitialized = false;
            Units.Clear();
            Monsters.Clear();
            EnemySpecies.Clear();
            Relics.Clear();
            Synergies.Clear();
            Skills.Clear();
        }
    }
}

