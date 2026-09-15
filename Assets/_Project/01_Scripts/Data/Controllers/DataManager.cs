using OzGameLab01.Data;
using System.Threading.Tasks;

namespace OzGameLab01.Managers
{
    public static class DataManager
    {
        public static GameDB<MonsterData, MonsterDataList> Monsters { get; } = new();
        public static GameDB<UnitData, UnitDataList> Units { get; } = new();
        public static GameDB<RelicData, RelicDataList> Relics { get; } = new();
        public static GameDB<SynergyData, SynergyDataList> Synergies { get; } = new();
        public static GameDB<SkillData, SkillDataList> Skills { get; } = new();

        // 어드레서블 주소 기반 모든 데이터 비동기 캐싱
        // 주소 나중에 추가하기!!!!!
        public static async Task LoadAllDatabase()
        {
            await Task.WhenAll(
                Monsters.LoadAsync(""),
                Units.LoadAsync("JSON/UnitJSON"),
                Relics.LoadAsync(""),
                Synergies.LoadAsync("JSON/SynergyJSON"),
                Skills.LoadAsync("")
            );
        }
    }
}

