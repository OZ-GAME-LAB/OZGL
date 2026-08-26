using UnityEngine;
using System.Collections.Generic;

//public class MonsterDataList : IDataList<MonsterData>
//{
//    public List<MonsterData> monsterList;
//    public List<MonsterData> GetList() => monsterList;
//}

[System.Serializable]
public class MonsterData
{
    public int id;
    public string name;
    public string spriteAddress;

    public int healthPoint;
    public int attackPoint;
    public int attackSpeed;
    public int skillCooldown;

    public MonsterType type;
}

public enum MonsterType
{
    normal,
    boss
}