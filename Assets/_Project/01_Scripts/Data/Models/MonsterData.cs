using UnityEngine;
using System.Collections.Generic;

namespace OzGameLab01.Data
{
    public class MonsterDataList : IDataList<MonsterData>
    {
        public List<MonsterData> monsterList;
        public List<MonsterData> GetList() => monsterList;
    }

    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, sourceNamespace: "", sourceAssembly: null, sourceClassName: null)]
    [System.Serializable]
    public class MonsterData : IIdentifiable
    {
        public int id;
        public string name;
        // Resources 경로(UnitPrefabProvider.GetEnemyPrefab 인자). 스폰은 species 목록을 우선 쓰고,
        // species가 비어 있을 때만 semiboss/boss(중간·최종보스)가 이 값으로 폴백한다(AllySpawner).
        // 상세: Docs/ENEMY_SCALING_DESIGN.md 4-2절.
        public string prefabAddress;

        public int healthPoint;
        public int attackPoint;
        public float defensePoint;
        public int criticalRate;
        public float criticalMult = 150f;
        public int dodgeRate;
        public float attackSpeed;
        public int skillCooldown;

        // 기본공격을 포함한 스킬 목록. SkillData.id 참조, 0번째 항목이 기본공격입니다.
        public List<int> skillIds = new List<int>();

        public MonsterType type;

        // 이 계층 전투에 나올 적 종(프리팹·표시 이름). 스폰 시 무작위로 하나 고른다(AllySpawner).
        // 원본: Docs/Database/EnemyData (1).xlsx normalEnemy 시트 하단 "적 목록".
        public List<MonsterSpecies> species = new List<MonsterSpecies>();

        public int Id => id;        // GameDB 식별자
    }

    [System.Serializable]
    public class MonsterSpecies
    {
        public string name;             // 전투 화면 표시 이름
        public string prefabAddress;    // Resources 경로(UnitPrefabProvider.GetEnemyPrefab 인자)
    }

    public enum MonsterType
    {
        normal,
        night,
        semiboss,
        boss
    }
}
