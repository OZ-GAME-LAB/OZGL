using System.Collections.Generic;
using OzGameLab01.Combat;

namespace OzGameLab01.Data
{
    public class SkillDataList : IDataList<SkillData>
    {
        public List<SkillData> skillList;
        public List<SkillData> GetList() => skillList;
    }

    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, sourceNamespace: "", sourceAssembly: null, sourceClassName: null)]
    [System.Serializable]
    public class SkillData
    {
        public int id;
        public string name;

        public float damage;
        public float cooldown;

        public DebuffProfile debuff;
        // Effects are executed in declaration order when present. Legacy skills use damage/debuff above.
        public List<EffectInstance> effects = new List<EffectInstance>();
    }
}
