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
        public string spriteAddress;
        // Resources 경로(UnitPrefabProvider.GetEnemyPrefab 인자). normal/night는 이 필드를
        // 쓰지 않고 6종 몹 풀에서 랜덤으로 고른다(AllySpawner) — semiboss/boss(중간·최종보스)만
        // 이 값을 그대로 스폰한다. 상세: Docs/ENEMY_SCALING_DESIGN.md 4-1절.
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

        public int Id => id;        // GameDB 식별자
    }

    public enum MonsterType
    {
        normal,
        night,
        semiboss,
        boss
    }
}
