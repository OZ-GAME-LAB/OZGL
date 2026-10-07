using System.Collections.Generic;
using OzGameLab01.Data;
using OzGameLab01.Managers;

namespace OzGameLab01.Combat
{
    /// <summary>
    /// 이번 전투에 참여하는 유닛(출전 + 보조칸)의 패시브 효과를 효과 캐시 소스로 만듭니다.
    /// 패시브 정의는 UnitData.passiveSkillId가 가리키는 SkillData.passiveEffects에 있습니다.
    /// 같은 유닛이 여러 칸에 있어도 패시브는 유닛 종류당 한 번만 적용합니다.
    /// </summary>
    public static class UnitPassiveSources
    {
        public static List<RuntimeEffectManager.EffectSource> Build(IEnumerable<UnitData> units, ContentCatalog content)
        {
            var sources = new List<RuntimeEffectManager.EffectSource>();
            if (units == null || content == null) return sources;

            var added = new HashSet<int>();
            foreach (UnitData unit in units)
            {
                if (unit == null || unit.passiveSkillId <= 0 || !added.Add(unit.id)) continue;

                IReadOnlyList<EffectInstance> effects = content.GetSkill(unit.passiveSkillId)?.passiveEffects;
                if (effects == null) continue;
                for (int i = 0; i < effects.Count; i++)
                {
                    sources.Add(new RuntimeEffectManager.EffectSource(
                        RuntimeEffectManager.EffectSourceKind.UnitPassive, unit.id, effects[i], i));
                }
            }

            return sources;
        }
    }
}
