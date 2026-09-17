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
    public class SkillData : IIdentifiable
    {
        public int id;                  // 고유 ID
        public int unitId;              // 스킬 보유 유닛 ID

        public string name;             // 스킬 명칭
        public string description;      // 스킬 설명

        public float baseValue;         // 기본 수치
        public float subValue;          // 보조 수치(필요 값이 복수일 때)
        public float cooldown;          // 스킬 쿨타임

        public TriggerType triggerType; // 스킬 발동 트리거
        public EffectTarget target;     // 효과 대상
        public DebuffProfile debuff;

        public int Id => id;
    }
}
