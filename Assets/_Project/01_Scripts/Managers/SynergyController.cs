using System.Collections.Generic;
using UnityEngine;
using OzGameLab01.UI;
using OzGameLab01.Data;
using OzGameLab01.Managers;

namespace OzGameLab01.Combat
{
    /// <summary>
    /// CombatManager에서 분리된 시너지 집계/발동/표시 책임을 담당합니다.
    /// Inspector 참조는 CombatManager가 그대로 들고 있고, 이 클래스는 그 값을
    /// 생성자로 전달받아 사용하는 순수 C# 클래스입니다(씬/프리팹 재배선 불필요).
    /// </summary>
    public class SynergyController
    {
        private readonly UnitRosterData rosterData;
        private readonly Transform synergyPanelRoot;
        private readonly SynergyItemView synergyItemTemplate;
        private readonly Color synergyActiveColor;
        private readonly Color synergyInactiveColor;
        private readonly Object logContext;

        private Dictionary<int, List<SynergyDefinition>> _unitTraitsById;
        private Dictionary<SynergyDefinition, int> _traitCounts;

        public SynergyController(
            UnitRosterData rosterData,
            Transform synergyPanelRoot,
            SynergyItemView synergyItemTemplate,
            Color synergyActiveColor,
            Color synergyInactiveColor,
            Object logContext)
        {
            this.rosterData = rosterData;
            this.synergyPanelRoot = synergyPanelRoot;
            this.synergyItemTemplate = synergyItemTemplate;
            this.synergyActiveColor = synergyActiveColor;
            this.synergyInactiveColor = synergyInactiveColor;
            this.logContext = logContext;
        }

        /// <summary>
        /// 유닛 id별 시너지 트레이트를 읽어 둡니다. UnitRosterData.UnitTraits(수동 목록,
        /// 로스터 준비 화면 전용)가 아니라 실제로 전투에 등장하는 유닛들의 jobType/tribeType으로
        /// 직접 계산합니다 — 유닛이 UnitRosterData의 placeholder든 실제 DB(JSON)든 동일하게 동작합니다.
        /// </summary>
        public void BuildUnitTraitLookup(Dictionary<int, UnitData> unitDataById)
        {
            _unitTraitsById = new Dictionary<int, List<SynergyDefinition>>();
            if (rosterData == null || unitDataById == null)
            {
                return;
            }

            UnitRosterData.RegisterActive(rosterData, logContext);

            foreach (KeyValuePair<int, UnitData> kvp in unitDataById)
            {
                List<SynergyDefinition> traits = new List<SynergyDefinition>();

                SynergyDefinition jobTrait = rosterData.GetJobTrait(kvp.Value.jobType);
                if (jobTrait != null)
                {
                    traits.Add(jobTrait);
                }

                SynergyDefinition tribeTrait = rosterData.GetTribeTrait(kvp.Value.tribeType);
                if (tribeTrait != null)
                {
                    traits.Add(tribeTrait);
                }

                _unitTraitsById[kvp.Key] = traits;
            }
        }

        public void ApplySynergies(Dictionary<CombatManager.SlotKey, int> spawnedFormation, Unit[,] slotUnits)
        {
            // 팀 전체에서 각 트레이트를 보유한 유닛 수를 센다 (시너지 발동 여부 판정용).
            // 인스펙터 폴백 편성이 아니라 실제로 스폰된 편성(spawnedFormation)을 기준으로 삼아야
            // 배치 화면에서 넘어온 편성에도 시너지가 정상 반영된다.
            _traitCounts = SynergyPanelUtility.CountTraits(spawnedFormation.Values, _unitTraitsById);

            // SelfSynergy 대상 효과: 해당 트레이트를 실제로 보유한 유닛에게만 적용한다.
            foreach (KeyValuePair<CombatManager.SlotKey, int> kvp in spawnedFormation)
            {
                Unit unit = slotUnits[kvp.Key.column, (int)kvp.Key.row];
                if (unit == null || !_unitTraitsById.TryGetValue(kvp.Value, out List<SynergyDefinition> traits) || traits == null)
                {
                    continue;
                }

                foreach (SynergyDefinition definition in traits)
                {
                    if (definition == null)
                    {
                        continue;
                    }

                    _traitCounts.TryGetValue(definition, out int count);
                    ApplyTierEffects(definition, count, SynergyTargetType.SelfSynergy, unit, spawnedFormation, slotUnits);
                }
            }

            // AllAllies 대상 효과: 트레이트 보유 여부와 무관하게 스폰된 유닛 전원에게 적용한다.
            // 같은 트레이트를 여러 유닛이 들고 있어도 한 번만 처리한다.
            HashSet<SynergyDefinition> processed = new HashSet<SynergyDefinition>();
            foreach (List<SynergyDefinition> traits in _unitTraitsById.Values)
            {
                if (traits == null)
                {
                    continue;
                }

                foreach (SynergyDefinition definition in traits)
                {
                    if (definition == null || !processed.Add(definition))
                    {
                        continue;
                    }

                    _traitCounts.TryGetValue(definition, out int count);
                    ApplyTierEffects(definition, count, SynergyTargetType.AllAllies, null, spawnedFormation, slotUnits);
                }
            }
        }

        /// <summary>
        /// 엑셀 설계 기반 SynergyData(RuntimeDataManager)에서 이름이 같은 시너지를 찾아, 현재
        /// 보유 수에 해당하는 단계의 효과 중 targetType이 일치하는 것만 적용합니다.
        /// 구버전 SynergyDefinition(job/tribe 트레이트)은 트레이트 보유 판정과 패널 표시에만 쓰고,
        /// 실제 스탯 적용은 이 SynergyData가 담당합니다 — 두 소스를 동시에 적용하면 중복 버프가 됩니다.
        /// </summary>
        private void ApplyTierEffects(
            SynergyDefinition definition,
            int count,
            SynergyTargetType targetType,
            Unit selfUnit,
            Dictionary<CombatManager.SlotKey, int> spawnedFormation,
            Unit[,] slotUnits)
        {
            SynergyData richData = FindSynergyData(definition.DisplayName);
            SynergyTier tier = FindActiveTier(richData, count);
            if (tier == null)
            {
                return;
            }

            foreach (SynergyEffectNode effect in tier.effects)
            {
                if (effect.targetType != targetType || !TryResolveStatType(effect, out EffectStatType statType))
                {
                    continue;
                }

                if (targetType == SynergyTargetType.SelfSynergy)
                {
                    selfUnit.ApplyStatEffect(statType, (float)effect.value);
                    continue;
                }

                foreach (KeyValuePair<CombatManager.SlotKey, int> kvp in spawnedFormation)
                {
                    Unit unit = slotUnits[kvp.Key.column, (int)kvp.Key.row];
                    unit?.ApplyStatEffect(statType, (float)effect.value);
                }
            }
        }

        private static SynergyData FindSynergyData(string displayName)
        {
            if (RuntimeDataManager.Instance == null)
            {
                return null;
            }

            foreach (SynergyData data in RuntimeDataManager.Instance.Synergies.Values)
            {
                if (data != null && data.name == displayName)
                {
                    return data;
                }
            }

            return null;
        }

        private static SynergyTier FindActiveTier(SynergyData data, int count)
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

        /// <summary>
        /// 1단계 적용 범위: 퍼센트 스탯 배율로 표현 가능한 효과만 지원합니다(StatBuff/IncreaseDamage/
        /// CooldownDecrease). ShieldOnStart/FixedDamage/StatusEffect/ExtraDamageOnStatus/
        /// UseSkillTwoTimes/NoSkillStatBuff/RecoveryIncrease/DecreaseDmgDefense는 보호막·고정
        /// 데미지·상태이상 부여 같은 새 전투 메커니즘이 있어야 해서 조용히 건너뜁니다(후속 작업).
        /// </summary>
        private static bool TryResolveStatType(SynergyEffectNode effect, out EffectStatType statType)
        {
            statType = EffectStatType.Unknown;

            if (effect.effectType == "CooldownDecrease")
            {
                statType = EffectStatType.AttackInterval;
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

        /// <summary>
        /// 보유 중인(카운트 1 이상) 시너지를 패널에 표시합니다.
        /// 발동 중인 시너지와 아직 발동하지 않은 시너지를 색상으로 구분합니다.
        /// </summary>
        public void PopulateSynergyPanel()
        {
            if (rosterData == null || synergyPanelRoot == null || synergyItemTemplate == null)
            {
                return;
            }

            for (int i = synergyPanelRoot.childCount - 1; i >= 0; i--)
            {
                Object.Destroy(synergyPanelRoot.GetChild(i).gameObject);
            }

            List<SynergyPanelUtility.DisplayItem> displayItems =
                SynergyPanelUtility.BuildDisplayItems(rosterData.SynergyDefinitions, _traitCounts);

            foreach (SynergyPanelUtility.DisplayItem displayItem in displayItems)
            {
                SynergyItemView item = Object.Instantiate(synergyItemTemplate, synergyPanelRoot);
                item.gameObject.SetActive(true);
                item.SetTitle(displayItem.Definition.DisplayName);
                item.SetStackText(displayItem.StackText);
                item.SetBackgroundColor(displayItem.IsActive ? synergyActiveColor : synergyInactiveColor);
            }
        }
    }
}
