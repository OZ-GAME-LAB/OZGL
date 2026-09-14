using System.Collections.Generic;
using OzGameLab01.Combat;

public class SkillDataList : IDataList<SkillData>
{
    public List<SkillData> skillList;
    public List<SkillData> GetList() => skillList;
}

[System.Serializable]
public class SkillData
{
    public int id;
    public string name;

    public float damage;
    public float cooldown;

    public DebuffProfile debuff;
}
