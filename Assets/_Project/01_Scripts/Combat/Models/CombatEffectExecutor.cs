using OzGameLab01.Data;
using System.Collections.Generic;
using UnityEngine;
using OzGameLab01.Managers;

namespace OzGameLab01.Combat
{
    /// <summary>
    /// 유닛 패시브/유물의 EffectInstance를 실제로 실행하는 트리거→타겟→이펙트 실행기입니다.
    /// 효과 목록은 CombatEffectCatalog에서 참조하고, 전투 상태(행 배치·
    /// 생존 여부)는 CombatFacade에서 읽습니다 — PlayerInventoryManager나
    /// RelicManager를 직접 참조하지 않습니다(그 둘의 보유 목록 취합은 RuntimeEffectManager가
    /// 이미 담당).
    ///
    /// 스탯, 고정 피해, 회복, 보호막, 디버프 해제를 지원합니다. Revive/
    /// DebuffImmunity/DebuffDurationModifier/CooldownModifier/
    /// NullifyNextSkill/SynergyModifier는 대응하는 메커니즘이 Unit에 아직 없어 조용히
    /// 건너뜁니다 — Docs/PASSIVE_TRIGGER_EFFECT_SCHEMA.md의 권고(메커니즘이 생길 때 그
    /// 효과의 실행 로직도 같이 만들기)를 따릅니다.
    /// </summary>
    public sealed class CombatEffectExecutor : System.IDisposable
    {
        private readonly CombatFacade _facade;
        private readonly IRandomProvider _random;
        private readonly HashSet<(RuntimeEffectManager.EffectSourceKind, int, int)> _firedOnce = new();
        private bool _firstAllyDeathFired;
        private int _allyAttackCount;
        private bool _processingAllyHealed;
        private bool _processingAllyBuffed;
        private bool _processingAllyShielded;

        public void Dispose()
        {
            PassiveEventBus.OnBattleStart -= HandleBattleStart;
            PassiveEventBus.OnBattleEnd -= HandleBattleEnd;
            PassiveEventBus.OnSelfDeath -= HandleSelfDeath;
            PassiveEventBus.OnAllyDeath -= HandleAllyDeath;
            PassiveEventBus.OnAllySkillUsed -= HandleAllySkillUsed;
            PassiveEventBus.OnEnemySkillUsed -= HandleEnemySkillUsed;
            PassiveEventBus.OnAttackLanded -= HandleAttackLanded;
            PassiveEventBus.OnAllyHealed -= HandleAllyHealed;
            PassiveEventBus.OnAllyBuffed -= HandleAllyBuffed;
            PassiveEventBus.OnAllyShielded -= HandleAllyShielded;
            PassiveEventBus.OnAllyHpChanged -= HandleAllyHpChanged;
            _firedOnce.Clear();
        }

        public CombatEffectExecutor(CombatFacade facade, IRandomProvider random = null)
        {
            _facade = facade;
            _random = random ?? new CombatRandom();

            PassiveEventBus.OnBattleStart += HandleBattleStart;
            PassiveEventBus.OnBattleEnd += HandleBattleEnd;
            PassiveEventBus.OnSelfDeath += HandleSelfDeath;
            PassiveEventBus.OnAllyDeath += HandleAllyDeath;
            PassiveEventBus.OnAllySkillUsed += HandleAllySkillUsed;
            PassiveEventBus.OnEnemySkillUsed += HandleEnemySkillUsed;
            PassiveEventBus.OnAttackLanded += HandleAttackLanded;
            PassiveEventBus.OnAllyHealed += HandleAllyHealed;
            PassiveEventBus.OnAllyBuffed += HandleAllyBuffed;
            PassiveEventBus.OnAllyShielded += HandleAllyShielded;
            PassiveEventBus.OnAllyHpChanged += HandleAllyHpChanged;
        }

        private void HandleBattleStart()
        {
            _allyAttackCount = 0;
            // Always는 별도 이벤트가 아니라 "전투 시작 시 1회 적용 후 유지"로 정의되어 있어
            // OnBattleStart와 같은 시점에 함께 처리합니다.
            Execute(TriggerType.Always, null);
            Execute(TriggerType.OnBattleStart, null);
        }

        private void HandleBattleEnd(bool victory) => Execute(TriggerType.OnBattleEnd, null);

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

        private void HandleAttackLanded(Unit attacker, Unit target)
        {
            if (attacker == null || attacker.TeamValue != Unit.Team.Ally) return;
            _allyAttackCount++;
            Execute(TriggerType.OnAttackCount, attacker, _allyAttackCount);
        }

        private void HandleAllyHealed(Unit unit)
        {
            if (_processingAllyHealed) return;
            _processingAllyHealed = true;
            try { Execute(TriggerType.OnAllyHealed, unit); }
            finally { _processingAllyHealed = false; }
        }

        private void HandleAllyBuffed(Unit unit)
        {
            if (_processingAllyBuffed) return;
            _processingAllyBuffed = true;
            try { Execute(TriggerType.OnAllyBuffed, unit); }
            finally { _processingAllyBuffed = false; }
        }

        private void HandleAllyShielded(Unit unit)
        {
            if (_processingAllyShielded) return;
            _processingAllyShielded = true;
            try { Execute(TriggerType.OnAllyShielded, unit); }
            finally { _processingAllyShielded = false; }
        }

        private void HandleAllyHpChanged(Unit unit, float previousUnitRatio)
        {
            float previousAverage = GetAverageHpRatio(previousUnitRatio, unit);
            float currentAverage = GetAverageHpRatio(-1f, null);
            if (previousAverage > currentAverage)
                Execute(TriggerType.OnHpBelowThreshold, unit, -1, currentAverage, previousAverage);
        }

        private float GetAverageHpRatio(float previousUnitRatio, Unit changedUnit)
        {
            float currentHp = 0f;
            float maxHp = 0f;
            foreach (Unit unit in _facade.GetParticipatingAllyUnits())
            {
                if (unit == null) continue;
                float hp = unit.CurrentHp;
                if (unit == changedUnit && previousUnitRatio >= 0f) hp = previousUnitRatio * unit.MaxHp;
                currentHp += hp;
                maxHp += unit.MaxHp;
            }
            return maxHp > 0f ? currentHp / maxHp : 0f;
        }

        private void Execute(TriggerType trigger, Unit triggeringUnit, int eventCount = -1,
            float thresholdRatio = -1f, float previousThresholdRatio = -1f)
        {
            IReadOnlyList<RuntimeEffectManager.EffectSource> sources =
                OzGameLab01.Common.SystemBus.Get<CombatEffectCatalog>()?.GetEffects(trigger)
                ?? System.Array.Empty<RuntimeEffectManager.EffectSource>();

            for (int i = 0; i < sources.Count; i++)
            {
                RuntimeEffectManager.EffectSource source = sources[i];
                EffectInstance effect = source.Definition;

                if (trigger == TriggerType.OnAttackCount && effect.triggerParam > 0f &&
                    eventCount % Mathf.Max(1, Mathf.RoundToInt(effect.triggerParam)) != 0)
                    continue;
                if (trigger == TriggerType.OnHpBelowThreshold)
                {
                    float threshold = effect.triggerParam > 0f ? effect.triggerParam / 100f : 0.5f;
                    if (previousThresholdRatio < 0f || previousThresholdRatio < threshold || thresholdRatio >= threshold)
                        continue;
                }

                var onceKey = (source.Kind, source.SourceId, source.DeclarationIndex);
                if (effect.once && _firedOnce.Contains(onceKey))
                {
                    continue;
                }

                // chance는 0~100 퍼센트. 0 이하는 "확률 미지정 = 항상 발동"으로 취급합니다.
                if (effect.chance > 0f && _random.NextDouble() * 100f >= effect.chance)
                {
                    continue;
                }

                foreach (Unit target in ResolveTargets(source, effect, triggeringUnit))
                {
                    if (ApplyEffect(effect, target))
                    {
                        string sourceName = source.Kind == RuntimeEffectManager.EffectSourceKind.Relic
                            ? OzGameLab01.Data.RuntimeContent.Catalog.GetRelic(source.SourceId)?.name
                            : source.Kind == RuntimeEffectManager.EffectSourceKind.Synergy
                                ? OzGameLab01.Data.RuntimeContent.Catalog.GetSynergy(source.SourceId)?.name
                                : OzGameLab01.Data.RuntimeContent.Catalog.GetUnit(source.SourceId)?.name;
                        string detail = effect.effect == EffectType.StatModifier
                            ? CombatFeedback.StatText(effect.statType, effect.effectParam)
                            : $"피해 {effect.effectParam:0.##}";
                        _facade.ReportFeedback(new CombatFeedback(
                            source.Kind == RuntimeEffectManager.EffectSourceKind.Relic ? CombatFeedbackKind.Relic : CombatFeedbackKind.Passive,
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
                        Unit owner = _facade.GetAllyUnitById(source.SourceId);
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
                    foreach (Unit unit in _facade.GetParticipatingAllyUnits())
                    {
                        if (!unit.IsDead)
                        {
                            yield return unit;
                        }
                    }

                    break;

                case EffectTarget.FrontRow:
                    foreach (Unit unit in _facade.GetAliveAlliesInRow(CombatManager.SlotRow.Front))
                    {
                        yield return unit;
                    }

                    break;

                case EffectTarget.MidRow:
                    foreach (Unit unit in _facade.GetAliveAlliesInRow(CombatManager.SlotRow.Mid))
                    {
                        yield return unit;
                    }

                    break;

                case EffectTarget.BackRow:
                    foreach (Unit unit in _facade.GetAliveAlliesInRow(CombatManager.SlotRow.Back))
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
                        yield return alive[_random.Next(alive.Count)];
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
                    if (_facade.EnemyUnit != null && !_facade.EnemyUnit.IsDead)
                    {
                        yield return _facade.EnemyUnit;
                    }

                    break;
            }
        }

        private List<Unit> GetAliveAllies()
        {
            List<Unit> alive = new List<Unit>();
            foreach (Unit unit in _facade.GetParticipatingAllyUnits())
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
            if (target == null) return false;
            switch (effect.effect)
            {
                case EffectType.StatModifier:
                    return target.ApplyStatEffect(effect.statType, effect.effectParam, effect.operation,
                        effect.durationSeconds, effect.untilBattleEnd || effect.durationSeconds <= 0);

                case EffectType.DealDamage:
                    target.TakeDamage(effect.effectParam);
                    return true;

                case EffectType.Heal:
                    return target.Heal(effect.effectParam);
                case EffectType.GrantShield:
                    return target.GrantShield(effect.effectParam, effect.durationSeconds, effect.untilBattleEnd);
                case EffectType.CleanseDebuffs:
                    target.CleanseDebuffs();
                    return true;

                // Revive/DebuffImmunity/DebuffDurationModifier/
                // CooldownModifier/NullifyNextSkill/SynergyModifier: 대응 메커니즘이 아직 없어
                // 의도적으로 건너뜁니다(Docs/PASSIVE_TRIGGER_EFFECT_SCHEMA.md 참고).
            }
            return false;
        }
    }
}
