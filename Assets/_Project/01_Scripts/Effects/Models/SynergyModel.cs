using System.Collections.Generic;
using OzGameLab01.Data;

namespace OzGameLab01.Effects.Models
{
    /// <summary>
    /// 시너지 집계와 활성 단계 및 지원 스탯 판정을 수행합니다.
    /// </summary>
    public static class SynergyModel
    {
        /// <summary>직업·종족 조회 결과 기반 유닛별 트레이트 구성</summary>
        public static Dictionary<int, List<SynergyDefinition>> BuildTraitLookup(
            Dictionary<int, OzGameLab01.Data.UnitData> units,
            System.Func<OzGameLab01.Data.UnitTypeJob, SynergyDefinition> getJob,
            System.Func<OzGameLab01.Data.UnitTypeTribe, SynergyDefinition> getTribe)
        {
            var result = new Dictionary<int, List<SynergyDefinition>>();
            foreach (var pair in units)
            {
                var traits = new List<SynergyDefinition>();
                SynergyDefinition job = getJob(pair.Value.jobType);
                SynergyDefinition tribe = getTribe(pair.Value.tribeType);
                if (job != null) traits.Add(job);
                if (tribe != null) traits.Add(tribe);
                result[pair.Key] = traits;
            }
            return result;
        }

        public static Dictionary<SynergyDefinition, int> CountTraits(
            IEnumerable<int> unitIds,
            Dictionary<int, List<SynergyDefinition>> traitsById)
        {
            Dictionary<SynergyDefinition, int> traitCounts = new Dictionary<SynergyDefinition, int>();

            foreach (int unitId in unitIds)
            {
                if (!traitsById.TryGetValue(unitId, out List<SynergyDefinition> traits) || traits == null)
                {
                    continue;
                }

                foreach (SynergyDefinition trait in traits)
                {
                    if (trait == null)
                    {
                        continue;
                    }

                    traitCounts.TryGetValue(trait, out int count);
                    traitCounts[trait] = count + 1;
                }
            }

            return traitCounts;
        }

        public static SynergyTier FindActiveTier(SynergyData data, int count)
        {
            if (data == null || data.tiers == null)
            {
                return null;
            }

            SynergyTier active = null;
            foreach (SynergyTier tier in data.tiers)
            {
                if (tier.requiredCount <= count && (active == null || tier.requiredCount > active.requiredCount))
                {
                    active = tier;
                }
            }

            return active;
        }

        public static bool TryResolveStatType(SynergyEffectNode effect, out EffectStatType statType)
        {
            statType = EffectStatType.Unknown;

            if (effect.effectType == "CooldownDecrease")
            {
                statType = EffectStatType.AttackInterval;
                return true;
            }

            if (effect.effectType == "RecoveryIncrease")
            {
                statType = EffectStatType.RecoveryAmount;
                return true;
            }

            if (effect.effectType != "StatBuff" && effect.effectType != "IncreaseDamage")
            {
                return false;
            }

            switch (effect.statType)
            {
                case "MaxHp": statType = EffectStatType.MaxHealth; return true;
                case "Attack": statType = EffectStatType.Attack; return true;
                // 이 코드베이스엔 스킬 전용 데미지 배율이 없어 SkillDamage도 Attack과 동일하게 취급한다.
                case "SkillDamage": statType = EffectStatType.Attack; return true;
                case "Defense": statType = EffectStatType.Defense; return true;
                case "AttackSpeed": statType = EffectStatType.AttackInterval; return true;
                case "CritcalRate": statType = EffectStatType.CriticalChance; return true;
                case "CritcalMult": statType = EffectStatType.CriticalMultiplier; return true;
                case "DodgeRate": statType = EffectStatType.DodgeChance; return true;
                default: return false; // AllStats 등 1단계 미지원
            }
        }
    }
}
