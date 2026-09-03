using UnityEngine;
using OzGameLab01.Data;
using System.Threading.Tasks;

namespace OzGameLab01.Managers
{
    public static class DataManager
    {
        public static readonly GameDB<MonsterData, MonsterDataList> Monsters = new();
        public static readonly GameDB<UnitData, UnitDataList> Units = new();
        public static readonly GameDB<RelicData, RelicDataList> Relics = new();

        // 어드레서블 주소 기반 모든 데이터 비동기 캐싱
        // 주소 나중에 추가하기!!!!!
        public static async Task LoadAllDatabase()
        {
            await Task.WhenAll(
                Monsters.LoadAsync(""),
                Units.LoadAsync(""),
                Relics.LoadAsync("")
            );
        }
    }
}

