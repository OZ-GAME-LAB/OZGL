using UnityEngine;
using System.Collections.Generic;

public class MonsterDataList : IDataList<MonsterData>
{
    public List<MonsterData> monsterList;
    public List<MonsterData> GetList() => monsterList;
}

[System.Serializable]
public class MonsterData
{
    public int id;
    public string name;
    public string spriteAddress;

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
}

public enum MonsterType
{
    normal,
    night,
    semiboss,
    boss
}