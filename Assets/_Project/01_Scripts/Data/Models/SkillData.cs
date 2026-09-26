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
        public int unitId;              // 연결된 유닛 ID
        public string iconAddress;      // 아이콘 스프라이트 주소
        public string castVfxAddress;   // 액티브 스킬 발동 시 시전자 위치 VFX 어드레서블 주소 (없으면 재생 안 함)
        public float castVfxScale;      // 발동 VFX 월드 스케일 (0이면 프리팹 원본 크기)
        public float projectileScale;   // 0보다 크면 시전자의 기본공격 투사체를 이 배율로 발사하고 도착 시 activeEffects 적용 (0이면 즉발)

        public string name;             // 스킬 명칭
        public string description;      // 스킬 설명
        public float cooldown;          // 스킬 쿨타임

        // Legacy combat fields remain available while activeEffects is integrated into execution.
        public float damage;
        public DebuffProfile debuff;
        // Effects are executed in declaration order when present. Older skills use damage/debuff above.
        public List<EffectInstance> effects = new List<EffectInstance>();

        // 액티브 효과 정의 (패시브일 경우 Null)
        public List<ActiveSkillEffectNode> activeEffects = new();
        // 패시브 효과 정의 (액티브일 경우 Null)
        public List<EffectInstance> passiveEffects = new();

        public int Id => id;
    }
}
