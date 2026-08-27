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
    public int criticalRate;
    public int dodgeRate;
    public int attackSpeed;
    public int skillCooldown;

    public string skillKey_one;
    public string skillKey_two;
    public string skillKey_three;

    public MonsterType type;
}

public enum MonsterType
{
    normal,
    boss
}