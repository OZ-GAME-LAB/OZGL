using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class MonsterData
{
    public int id;
    public string name;
    public string spriteAddress;

    public MonsterType type;
}

public enum MonsterType
{
    normal,
    boss
}