using System.Collections.Generic;

namespace OzGameLab01.Data
{
    public class EnemySpeciesDataList : IDataList<EnemySpeciesData>
    {
        public List<EnemySpeciesData> speciesList;
        public List<EnemySpeciesData> GetList() => speciesList;
    }

    /// <summary>
    /// 적 종류(비주얼) 데이터. 스탯은 MonsterData의 계층(normal/night/semiboss/boss) 행이 정하고,
    /// 이 데이터는 그 계층 전투에 어떤 종이 나올지와 표시 이름만 정합니다.
    /// 원본: Docs/Database/EnemyData (1).xlsx normalEnemy 시트 하단 "적 목록".
    /// </summary>
    [System.Serializable]
    public class EnemySpeciesData : IIdentifiable
    {
        public int id;
        public string name;             // 전투 화면 표시 이름
        public string prefabAddress;    // Resources 경로(UnitPrefabProvider.GetEnemyPrefab 인자)
        public List<MonsterType> tiers = new List<MonsterType>();   // 등장 계층(해당 계층 전투에서 무작위 선택)

        public int Id => id;
    }
}
