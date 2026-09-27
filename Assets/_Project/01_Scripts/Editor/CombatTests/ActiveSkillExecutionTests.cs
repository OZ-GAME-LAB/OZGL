using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using OzGameLab01.Combat;
using OzGameLab01.Data;

namespace OzGameLab01.Tests.EditMode
{
    /// <summary>
    /// SkillData.activeEffects 실행기(Unit.ActiveSkills.cs) 검증. 기획서(UnitData.xlsx 스킬 시트) 공식 기준.
    /// 전투 세션이 없는 EditMode에서는 아군 대상이 시전자 자신으로 해석됩니다.
    /// </summary>
    public class ActiveSkillExecutionTests
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private readonly List<UnityEngine.GameObject> _created = new List<UnityEngine.GameObject>();

        /// <summary>회피·치명타가 일어나지 않도록 항상 큰 값을 돌려주는 난수(Next는 고정 인덱스).</summary>
        private sealed class FixedRandom : IRandomProvider
        {
            private readonly double _value;
            private readonly int _index;
            public FixedRandom(double value, int index = 0) { _value = value; _index = index; }
            public double NextDouble() => _value;
            public int Next(int exclusiveMax) => _index % exclusiveMax;
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var go in _created) UnityEngine.Object.DestroyImmediate(go);
            _created.Clear();
        }

        [Test]
        public void DamageUsesCasterAttackTimesValue()
        {
            Unit caster = CreateUnit(attack: 10f), target = CreateUnit(defense: 0f);
            Execute(caster, Skill(1, Node(ActiveEffectTarget.CurrentTarget, ActiveEffectType.Damage, EffectStatType.Attack, 1.5f)), target);
            Assert.That(target.CurrentHp, Is.EqualTo(85f).Within(0.01f));
        }

        [Test]
        public void GenieTrueDamageFiresFromThirdCastAndIgnoresDefense()
        {
            Unit caster = CreateUnit(attack: 10f), target = CreateUnit(defense: 5f);
            SkillData genie = Skill(51051, Node(ActiveEffectTarget.CurrentTarget, ActiveEffectType.TrueDamage, EffectStatType.Attack, 3.9f));
            Execute(caster, genie, target);
            Execute(caster, genie, target);
            Assert.That(target.CurrentHp, Is.EqualTo(100f).Within(0.01f), "첫 2회는 스택만 쌓인다");
            Execute(caster, genie, target);
            Assert.That(target.CurrentHp, Is.EqualTo(61f).Within(0.01f), "3회째: 공격력 10 × 3.9, 방어 무시");
        }

        [Test]
        public void HealRestoresMaxHpRatio()
        {
            Unit caster = CreateUnit();
            caster.TakeDamage(50f);
            Execute(caster, Skill(1, Node(ActiveEffectTarget.AllAllies, ActiveEffectType.Heal, EffectStatType.MaxHealth, 0.1f)), null);
            Assert.That(caster.CurrentHp, Is.EqualTo(60f).Within(0.01f));
        }

        [Test]
        public void StealStatLowersTargetAttackAndRaisesCaster()
        {
            Unit caster = CreateUnit(attack: 10f), target = CreateUnit();
            Execute(caster, Skill(1, Node(ActiveEffectTarget.CurrentTarget, ActiveEffectType.StealStat, EffectStatType.Attack, 0.2f, 5f)), target);
            Assert.That(target.HasAnyDebuff, Is.True);
            Assert.That((float)typeof(Unit).GetField("attackPoint", Private).GetValue(caster), Is.EqualTo(12f).Within(0.01f));
        }

        [Test]
        public void LionShieldScalesWithMissingHealthAndTaunts()
        {
            Unit lion = CreateUnit();
            lion.TakeDamage(50f);
            Execute(lion, Skill(51061,
                Node(ActiveEffectTarget.Self, ActiveEffectType.Taunt, EffectStatType.Unknown, 1f, 999f),
                Node(ActiveEffectTarget.Self, ActiveEffectType.GrantShield, EffectStatType.MaxHealth, 1f, 7f)), null);
            Assert.That(lion.IsTaunting, Is.True);
            Assert.That(lion.Shield, Is.EqualTo(200f).Within(0.01f), "최대HP 100 × (100 / 50)");
        }

        [Test]
        public void CleanseRemovesOnlyOnePrimaryDebuff()
        {
            Unit caster = CreateUnit();
            caster.ApplyDebuff(new DebuffProfile { type = DebuffType.Stun, duration = 5f });
            caster.ApplyDebuff(new DebuffProfile { type = DebuffType.Silence, duration = 5f });
            Execute(caster, Skill(1, Node(ActiveEffectTarget.AllAllies, ActiveEffectType.CleanseDebuff, EffectStatType.Unknown, 1f)), null);
            Assert.That(caster.TryGetPrimaryDebuff(out DebuffType remaining, out _, out _), Is.True);
            Assert.That(remaining, Is.EqualTo(DebuffType.Silence), "우선순위가 가장 높은 기절만 제거");
        }

        [Test]
        public void CooldownRecoveryReducesActiveSkillTimer()
        {
            Unit caster = CreateUnit();
            UnitSkillRuntime active = AddSkill(caster, basicCooldown: 1f, activeCooldown: 10f);
            active.timer = 5f;
            Execute(caster, Skill(1, Node(ActiveEffectTarget.AllAllies, ActiveEffectType.CooldownRecovery, EffectStatType.TurnRecovery, 1f)), null);
            Assert.That(active.timer, Is.EqualTo(4f).Within(0.001f));
        }

        [Test]
        public void TimedAttackSpeedBuffShortensBasicCooldownThenExpires()
        {
            Unit caster = CreateUnit();
            AddSkill(caster, basicCooldown: 1f, activeCooldown: 10f);
            Execute(caster, Skill(1, Node(ActiveEffectTarget.RandomAlly, ActiveEffectType.StatModifier, EffectStatType.AttackInterval, 0.15f, 10f)), null);
            Assert.That(BasicCooldown(caster), Is.EqualTo(0.85f).Within(0.001f));

            var stats = (UnitCombatStats)typeof(Unit).GetField("_stats", Private).GetValue(caster);
            stats.Tick(10f);
            Assert.That(BasicCooldown(caster), Is.EqualTo(1f).Within(0.001f));
        }

        [Test]
        public void TinkerBellAppliesSameRandomDebuffToEnemyAndSelf()
        {
            Unit caster = CreateUnit(), target = CreateUnit();
            caster.SetRandomProvider(new FixedRandom(0.99, index: 1)); // Next(4)+1 = 2 → 기절
            Execute(caster, Skill(51151,
                Node(ActiveEffectTarget.CurrentTarget, ActiveEffectType.StatModifier, EffectStatType.Unknown, 1f, 7f),
                Node(ActiveEffectTarget.Self, ActiveEffectType.StatModifier, EffectStatType.Unknown, 1f, 7f)), target);
            Assert.That(target.TryGetPrimaryDebuff(out DebuffType onTarget, out float targetSeconds, out _), Is.True);
            Assert.That(caster.TryGetPrimaryDebuff(out DebuffType onSelf, out _, out _), Is.True);
            Assert.That(onSelf, Is.EqualTo(onTarget));
            Assert.That(targetSeconds, Is.EqualTo(7f).Within(0.001f));
        }

        private Unit CreateUnit(float attack = 10f, float defense = 0f)
        {
            var go = new UnityEngine.GameObject("active skill test unit");
            _created.Add(go);
            var unit = go.AddComponent<Unit>();
            unit.Configure(new UnitData { healthPoint = 100f, attackPoint = attack, defensePoint = defense });
            typeof(Unit).GetMethod("EnsureRuntimeComponents", Private).Invoke(unit, null);
            typeof(Unit).GetMethod("InitializeRuntimeState", Private).Invoke(unit, null);
            unit.SetRandomProvider(new FixedRandom(0.99));
            return unit;
        }

        private static UnitSkillRuntime AddSkill(Unit unit, float basicCooldown, float activeCooldown)
        {
            var skills = (List<UnitSkillRuntime>)typeof(Unit).GetField("_skills", Private).GetValue(unit);
            skills.Clear();
            skills.Add(new UnitSkillRuntime { data = new SkillData { id = 900, cooldown = basicCooldown } });
            var active = new UnitSkillRuntime { data = new SkillData { id = 1, cooldown = activeCooldown } };
            skills.Add(active);
            return active;
        }

        private static float BasicCooldown(Unit unit)
        {
            var skills = (List<UnitSkillRuntime>)typeof(Unit).GetField("_skills", Private).GetValue(unit);
            return (float)typeof(Unit).GetMethod("GetEffectiveCooldown", Private).Invoke(unit, new object[] { skills[0] });
        }

        private static void Execute(Unit caster, SkillData skill, Unit target)
            => typeof(Unit).GetMethod("ExecuteActiveEffects", Private).Invoke(caster, new object[] { skill, target });

        private static SkillData Skill(int id, params ActiveSkillEffectNode[] nodes)
            => new SkillData { id = id, activeEffects = new List<ActiveSkillEffectNode>(nodes) };

        private static ActiveSkillEffectNode Node(ActiveEffectTarget target, ActiveEffectType type, EffectStatType stat,
            float value, float extraParam = 0f)
            => new ActiveSkillEffectNode { targetType = target, effectType = type, statType = stat, value = value, extraParam = extraParam };
    }
}
