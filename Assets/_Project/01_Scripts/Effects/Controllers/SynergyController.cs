using System.Collections.Generic;
using UnityEngine;
using OzGameLab01.UI;
using OzGameLab01.Combat;
using OzGameLab01.Data;
using OzGameLab01.Effects.Models;
using OzGameLab01.Managers;

namespace OzGameLab01.Controllers
{
    /// <summary>
    /// CombatManager에서 분리된 시너지 집계/발동/표시 책임을 담당합니다.
    /// Inspector 참조는 CombatManager가 그대로 들고 있고, 이 클래스는 그 값을
    /// 생성자로 전달받아 사용하는 순수 C# 클래스입니다(씬/프리팹 재배선 불필요).
    /// </summary>
    public class SynergyController : OzGameLab01.Effects.Contracts.IEffectsNotificationSource
    {
        private readonly OzGameLab01.Effects.Controllers.EffectsNotificationPublisher _notifications = new OzGameLab01.Effects.Controllers.EffectsNotificationPublisher();
        public event System.Action<OzGameLab01.Effects.Models.EffectsNotification> Notification
        {
            add => _notifications.Notification += value;
            remove => _notifications.Notification -= value;
        }

        public System.Action<CombatFeedback> OnEffectApplied { get; set; }
        private readonly UnitRosterData _rosterData;
        private readonly Object _logContext;
        private readonly OzGameLab01.Effects.Views.SynergyPanelView _panelView;

        private Dictionary<int, List<SynergyDefinition>> _unitTraitsById;
        private Dictionary<SynergyDefinition, int> _traitCounts;
        private readonly List<RuntimeEffectManager.EffectSource> _activeSharedEffects = new List<RuntimeEffectManager.EffectSource>();
        public IReadOnlyList<RuntimeEffectManager.EffectSource> ActiveSharedEffects => _activeSharedEffects;

        public SynergyController(
            UnitRosterData rosterData,
            Transform synergyPanelRoot,
            SynergyItemView synergyItemTemplate,
            Color synergyActiveColor,
            Color synergyInactiveColor,
            Object logContext)
        {
            _rosterData = rosterData;
            _logContext = logContext;
            _panelView = new OzGameLab01.Effects.Views.SynergyPanelView(synergyPanelRoot, synergyItemTemplate, synergyActiveColor, synergyInactiveColor);
        }

        /// <summary>
        /// 유닛 id별 시너지 트레이트를 읽어 둡니다. UnitRosterData.UnitTraits(수동 목록,
        /// 로스터 준비 화면 전용)가 아니라 실제로 전투에 등장하는 유닛들의 jobType/tribeType으로
        /// 직접 계산합니다 — 유닛이 UnitRosterData의 placeholder든 실제 DB(JSON)든 동일하게 동작합니다.
        /// </summary>
        public void BuildUnitTraitLookup(Dictionary<int, UnitData> unitDataById)
        {
            _unitTraitsById = new Dictionary<int, List<SynergyDefinition>>();
            if (_rosterData == null || unitDataById == null)
            {
                return;
            }

            UnitRosterData.RegisterActive(_rosterData, _logContext);

            _unitTraitsById = OzGameLab01.Effects.Models.SynergyModel.BuildTraitLookup(unitDataById, _rosterData.GetJobTrait, _rosterData.GetTribeTrait);
        }

        public void ApplySynergies(Dictionary<CombatManager.SlotKey, int> spawnedFormation, Unit[,] slotUnits)
        {
            _activeSharedEffects.Clear();
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
                    CollectSharedTierEffects(definition, count);
                    ApplyTierEffects(definition, count, SynergyTargetType.HighestHPUnit, null, spawnedFormation, slotUnits);
                }
            }
            _notifications.Publish(OzGameLab01.Effects.Models.EffectsNotificationKind.SynergiesEvaluated, 0, _traitCounts.Count);
        }

        private void CollectSharedTierEffects(SynergyDefinition definition, int count)
        {
            SynergyData richData = FindSynergyData(definition.DisplayName);
            SynergyTier tier = FindActiveTier(richData, count);
            if (tier == null || tier.effects == null || richData == null) return;

            int declarationIndex = 0;
            foreach (SynergyEffectNode node in tier.effects)
            {
                if (node.targetType != SynergyTargetType.AllAllies || !TryResolveSynergyEffect(node, out EffectInstance effect))
                    continue;
                _activeSharedEffects.Add(new RuntimeEffectManager.EffectSource(
                    RuntimeEffectManager.EffectSourceKind.Synergy,
                    richData.id,
                    effect,
                    declarationIndex++));
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
                if (effect.targetType != targetType)
                {
                    continue;
                }

                if (targetType == SynergyTargetType.SelfSynergy && effect.effectType == "ShieldOnStart")
                {
                    selfUnit?.GrantShield(selfUnit.MaxHp * effect.value, 0f, true);
                    continue;
                }

                if (targetType == SynergyTargetType.SelfSynergy && effect.effectType == "FixedDamage")
                {
                    selfUnit?.SetFixedDamage(effect.value);
                    continue;
                }

                if (targetType == SynergyTargetType.SelfSynergy && effect.effectType == "ExtraDamageOnStatus")
                {
                    selfUnit?.SetExtraDamageOnStatus(effect.value);
                    continue;
                }

                if (targetType == SynergyTargetType.HighestHPUnit)
                {
                    Unit highest = null;
                    foreach (KeyValuePair<CombatManager.SlotKey, int> kvp in spawnedFormation)
                    {
                        Unit candidate = slotUnits[kvp.Key.column, (int)kvp.Key.row];
                        if (candidate != null && (highest == null || candidate.CurrentHp > highest.CurrentHp)) highest = candidate;
                    }
                    if (highest == null) continue;
                    if (effect.effectType == "ShieldOnStart")
                    {
                        highest.GrantShield(highest.MaxHp * effect.value, 0f, true);
                    }
                    else if (TryResolveStatType(effect, out EffectStatType highestStat))
                    {
                        ApplyAndReport(highest, richData.name, highestStat, effect.value);
                    }
                    continue;
                }

                if (!TryResolveStatType(effect, out EffectStatType statType)) continue;

                if (targetType == SynergyTargetType.SelfSynergy)
                {
                    ApplyAndReport(selfUnit, richData.name, statType, (float)effect.value);
                    continue;
                }

                // Shared synergy effects are collected into CombatEffectCatalog and run by
                // CombatEffectExecutor at battle start. This prevents a second direct path.
            }
        }

        private static bool TryResolveSynergyEffect(SynergyEffectNode node, out EffectInstance effect)
        {
            effect = default;
            if (node.effectType == "ShieldOnStart")
            {
                effect = new EffectInstance
                {
                    trigger = TriggerType.Always,
                    target = EffectTarget.AllAllies,
                    effect = EffectType.GrantShield,
                    effectParam = node.value * 100f,
                    effectParamIsPercent = true,
                    untilBattleEnd = true
                };
                return true;
            }

            if (node.effectType == "FixedDamage")
            {
                effect = new EffectInstance
                {
                    trigger = TriggerType.Always,
                    target = EffectTarget.AllAllies,
                    effect = EffectType.FixedDamage,
                    effectParam = node.value,
                    untilBattleEnd = true
                };
                return true;
            }

            if (node.effectType == "ExtraDamageOnStatus")
            {
                effect = new EffectInstance
                {
                    trigger = TriggerType.Always,
                    target = EffectTarget.AllAllies,
                    effect = EffectType.ExtraDamageOnStatus,
                    effectParam = node.value,
                    untilBattleEnd = true
                };
                return true;
            }

            if (!TryResolveStatType(node, out EffectStatType statType)) return false;
            effect = new EffectInstance
            {
                trigger = TriggerType.Always,
                target = EffectTarget.AllAllies,
                effect = EffectType.StatModifier,
                statType = statType,
                operation = EffectOperation.Add,
                effectParam = node.value,
                untilBattleEnd = true
            };
            return true;
        }

        private void ApplyAndReport(Unit unit, string source, EffectStatType stat, float value)
        {
            if (unit != null && unit.ApplyStatEffect(stat, value))
                OnEffectApplied?.Invoke(new CombatFeedback(CombatFeedbackKind.Synergy,
                    source, CombatFeedback.StatText(stat, value), unit));
        }

        private static SynergyData FindSynergyData(string displayName)
        {
            foreach (SynergyData data in OzGameLab01.Data.RuntimeContent.Catalog.Synergies.Values)
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
            return OzGameLab01.Effects.Models.SynergyModel.FindActiveTier(data, count);
        }

        /// <summary>
        /// 1단계 적용 범위: 퍼센트 스탯 배율로 표현 가능한 효과만 지원합니다(StatBuff/IncreaseDamage/
        /// CooldownDecrease). ShieldOnStart/FixedDamage/StatusEffect/ExtraDamageOnStatus/
        /// UseSkillTwoTimes/NoSkillStatBuff/RecoveryIncrease/DecreaseDmgDefense는 보호막·고정
        /// 데미지·상태이상 부여 같은 새 전투 메커니즘이 있어야 해서 조용히 건너뜁니다(후속 작업).
        /// </summary>
        private static bool TryResolveStatType(SynergyEffectNode effect, out EffectStatType statType)
        {
            return OzGameLab01.Effects.Models.SynergyModel.TryResolveStatType(effect, out statType);
        }

        /// <summary>
        /// 보유 중인(카운트 1 이상) 시너지를 패널에 표시합니다.
        /// 발동 중인 시너지와 아직 발동하지 않은 시너지를 색상으로 구분합니다.
        /// </summary>
        public void PopulateSynergyPanel()
        {
            if (_rosterData == null || !_panelView.IsAvailable)
            {
                return;
            }
            _panelView.Render(SynergyPanelUtility.BuildDisplayItems(_rosterData.SynergyDefinitions, _traitCounts));
        }
    }
}
