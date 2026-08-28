using UnityEngine;
using System.Collections.Generic;

public class UnitDataList : IDataList<UnitData>
{
    public List<UnitData> unitList;
    public List<UnitData> GetList() => unitList;
}

[System.Serializable]
public class UnitData
{
    public int id;
    public string name;
    public string spriteAddress;

    public int healthPoint;
    public int attackPoint;
    public int criticalRate;
    public int dodgeRate;
    public int bloodDrain;
    public int attackSpeed;
    public int skillCooldown;

    public string attackKey;
    public string skillKey;

    // public UnitType type;
}