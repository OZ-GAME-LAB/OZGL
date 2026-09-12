using System.Collections.Generic;
using UnityEngine;
using OzGameLab01.Managers;

namespace OzGameLab01.Combat
{
    /// <summary>
    /// 유닛 패시브/유물의 EffectInstance를 실제로 실행하는 트리거→타겟→이펙트 실행기입니다.
    /// 데이터는 RuntimeDataManager.GetEffects(trigger) 하나만 참조하고, 전투 상태(행 배치·
    /// 생존 여부)는 CombatManager/BattleUnitRegistry에서 읽습니다 — PlayerInventoryManager나
    /// RelicManager를 직접 참조하지 않습니다(그 둘의 보유 목록 취합은 RuntimeEffectManager가
    /// 이미 담당).
    ///
    /// 지금 실제로 실행되는 EffectType은 StatModifier(Unit.ApplyStatEffect)와
    /// DealDamage(Unit.TakeDamage, 고정 데미지)뿐입니다. Heal/GrantShield/Revive/
    /// CleanseDebuffs/DebuffImmunity/DebuffDurationModifier/CooldownModifier/
    /// NullifyNextSkill/SynergyModifier는 대응하는 메커니즘이 Unit에 아직 없어 조용히
    /// 건너뜁니다 — Docs/PASSIVE_TRIGGER_EFFECT_SCHEMA.md의 권고(메커니즘이 생길 때 그
    /// 효과의 실행 로직도 같이 만들기)를 따릅니다.
    /// </summary>
    public sealed class CombatEffectExecutor
    {
        private readonly CombatManager _combatManager;
        private readonly HashSet<(RuntimeEffectManager.EffectSourceKind, int, int)> _firedOnce = new();
        private bool _firstAllyDeathFired;

        public CombatEffectExecutor(CombatManager combatManager)
        {
            _combatManager = combatManager;

            PassiveEventBus.OnBattleStart += HandleBattleStart;
            PassiveEventBus.OnSelfDeath += HandleSelfDeath;
            PassiveEventBus.OnAllyDeath += HandleAllyDeath;
            PassiveEventBus.OnAllySkillUsed += HandleAllySkillUsed;
            PassiveEventBus.OnEnemySkillUsed += HandleEnemySkillUsed;
        }

        private void HandleBattleStart()
        {
            // Always는 별도 이벤트가 아니라 "전투 시작 시 1회 적용 후 유지"로 정의되어 있어
            // OnBattleStart와 같은 시점에 함께 처리합니다.
            Execute(TriggerType.Always, null);
            Execute(TriggerType.OnBattleStart, null);
        }

        private void HandleSelfDeath(Unit unit)
        {
            Execute(TriggerType.OnSelfDeath, unit);
        }

        private void HandleAllyDeath(Unit unit)
        {
            Execute(TriggerType.OnAllyDeath, unit);

            if (!_firstAllyDeathFired)
            {
                _firstAllyDeathFired = true;
                Execute(TriggerType.OnFirstAllyDeath, unit);
            }
        }

        private void HandleAllySkillUsed(Unit caster, SkillData skill)
        {
            Execute(TriggerType.OnAllySkillUsed, caster);
        }

        private void HandleEnemySkillUsed(Unit caster, SkillData skill)
        {
            Execute(TriggerType.OnEnemySkillUsed, caster);
        }

        private void Execute(TriggerType trigger, Unit triggeringUnit)
        {
            IReadOnlyList<RuntimeEffectManager.EffectSource> sources =
                RuntimeDataManager.Instance.GetEffects(trigger);

            for (int i = 0; i < sources.Count; i++)
            {
                RuntimeEffectManager.EffectSource source = sources[i];
                EffectInstance effect = source.Definition;

                var onceKey = (source.Kind, source.SourceId, source.DeclarationIndex);
                if (effect.once && _firedOnce.Contains(onceKey))
                {
                    continue;
                }

                // chance는 0~100 퍼센트. 0 이하는 "확률 미지정 = 항상 발동"으로 취급합니다.
                if (effect.chance > 0f && Random.value * 100f >= effect.chance)
                {
                    continue;
                }

                foreach (Unit target in ResolveTargets(source, effect, triggeringUnit))
                {
                    if (ApplyEffect(effect, target))
                    {
                        bool relic = source.Kind == RuntimeEffectManager.EffectSourceKind.Relic;
                        string sourceName = relic
                            ? RuntimeDataManager.Instance.GetRelic(source.SourceId)?.name
                            : RuntimeDataManager.Instance.GetUnit(source.SourceId)?.name;
                        string detail = effect.effect == EffectType.StatModifier
                            ? CombatFeedback.StatText(effect.statType, effect.effectParam)
                            : $"피해 {effect.effectParam:0.##}";
                        _combatManager.ReportFeedback(new CombatFeedback(
                            relic ? CombatFeedbackKind.Relic : CombatFeedbackKind.Passive,
                            sourceName ?? $"#{source.SourceId}", detail, target));
                    }
                }

                if (effect.once)
                {
                    _firedOnce.Add(onceKey);
                }
            }
        }

        private IEnumerable<Unit> ResolveTargets(
            RuntimeEffectManager.EffectSource source, EffectInstance effect, Unit triggeringUnit)
        {
            switch (effect.target)
            {
                case EffectTarget.Self:
                    // 유물은 특정 유닛 소유가 아니라 Self가 성립하지 않습니다.
                    if (source.Kind == RuntimeEffectManager.EffectSourceKind.UnitPassive)
                    {
                        Unit owner = _combatManager.GetAllyUnitById(source.SourceId);
                        if (owner != null && !owner.IsDead)
                        {
                            yield return owner;
                        }
                    }

                    break;

                case EffectTarget.TriggeringUnit:
                    if (triggeringUnit != null && !triggeringUnit.IsDead)
                    {
                        yield return triggeringUnit;
                    }

                    break;

                case EffectTarget.AllAllies:
                    foreach (Unit unit in _combatManager.GetParticipatingAllyUnits())
                    {
                        if (!unit.IsDead)
                        {
                            yield return unit;
                        }
                    }

                    break;

                case EffectTarget.FrontRow:
                    foreach (Unit unit in _combatManager.GetAliveAlliesInRow(CombatManager.SlotRow.Front))
                    {
                        yield return unit;
                    }

                    break;

                case EffectTarget.MidRow:
                    foreach (Unit unit in _combatManager.GetAliveAlliesInRow(CombatManager.SlotRow.Mid))
                    {
                        yield return unit;
                    }

                    break;

                case EffectTarget.BackRow:
                    foreach (Unit unit in _combatManager.GetAliveAlliesInRow(CombatManager.SlotRow.Back))
                    {
                        yield return unit;
                    }

                    break;

                case EffectTarget.SupportRow:
                    // 서포트 칸 유닛은 전투 그리드에 스폰되지 않아 Unit 인스턴스가 없습니다.
                    // 서포트 칸 전용 메커니즘이 생기기 전까지는 대상이 없습니다.
                    break;

                case EffectTarget.RandomAlly:
                {
                    List<Unit> alive = GetAliveAllies();
                    if (alive.Count > 0)
                    {
                        yield return alive[Random.Range(0, alive.Count)];
                    }

                    break;
                }

                case EffectTarget.WorstHpAlly:
                {
                    Unit worst = null;
                    float worstRatio = float.MaxValue;
                    foreach (Unit unit in GetAliveAllies())
                    {
                        float ratio = unit.MaxHp > 0f ? unit.CurrentHp / unit.MaxHp : 0f;
                        if (ratio < worstRatio)
                        {
                            worstRatio = ratio;
                            worst = unit;
                        }
                    }

                    if (worst != null)
                    {
                        yield return worst;
                    }

                    break;
                }

                case EffectTarget.Enemy:
                    if (_combatManager.EnemyUnit != null && !_combatManager.EnemyUnit.IsDead)
                    {
                        yield return _combatManager.EnemyUnit;
                    }

                    break;
            }
        }

        private List<Unit> GetAliveAllies()
        {
            List<Unit> alive = new List<Unit>();
            foreach (Unit unit in _combatManager.GetParticipatingAllyUnits())
            {
                if (!unit.IsDead)
                {
                    alive.Add(unit);
                }
            }

            return alive;
        }

        private static bool ApplyEffect(EffectInstance effect, Unit target)
        {
            switch (effect.effect)
            {
                case EffectType.StatModifier:
                    return target.ApplyStatEffect(effect.statType, effect.effectParam);

                case EffectType.DealDamage:
                    target.TakeDamage(effect.effectParam);
                    return true;

                // Heal/GrantShield/Revive/CleanseDebuffs/DebuffImmunity/DebuffDurationModifier/
                // CooldownModifier/NullifyNextSkill/SynergyModifier: 대응 메커니즘이 아직 없어
                // 의도적으로 건너뜁니다(Docs/PASSIVE_TRIGGER_EFFECT_SCHEMA.md 참고).
            }
            return false;
        }
    }
}
