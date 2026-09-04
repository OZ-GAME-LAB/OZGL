using UnityEngine;
using System.Collections.Generic;
using OzGameLab01.Combat;

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
    public float basicAttackCooldown = 1f;
    public int skillCooldown;

    public string attackKey;
    public string skillKey;

    // 공용 아군 프리팹(Unit.Configure)이 생성 시 참조하는 시각/전투 클래스 정보
    public Color color = Color.white;
    public Unit.SkillType skillType;

    // public UnitType type;
}
