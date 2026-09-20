using System.Collections.Generic;
using OzGameLab01.Combat;

namespace OzGameLab01.Data
{
    public class SkillDataList : IDataList<SkillData>
    {
        public List<SkillData> skillList;
        public List<SkillData> GetList() => skillList;
    }

    [System.Serializable]
    public class SkillData : IIdentifiable
    {
        public int id;                  // 고유 ID
        public int unitId;              // 연결된 유닛 ID

        public string name;             // 스킬 명칭
        public string description;      // 스킬 설명
        public float cooldown;          // 스킬 쿨타임

        // 액티브 효과 정의 (패시브일 경우 Null)
        public List<ActiveSkillEffectNode> activeEffects = new();
        // 패시브 효과 정의 (액티브일 경우 Null)
        public List<EffectInstance> passiveEffects = new();

        public int Id => id;            // GameDB 식별자
    }
}
