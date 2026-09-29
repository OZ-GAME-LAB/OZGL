using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using OzGameLab01.Combat;
using OzGameLab01.Data;
using UnityEngine;

namespace OzGameLab01.Tests.EditMode
{
    public class UnitPassiveTests
    {
        [Test]
        public void PassiveSourcesReadSkillDataOncePerUnitType()
        {
            ContentCatalog content = RuntimeContent.Catalog;
            UnitData alice = content.GetUnit(100);
            int expected = content.GetSkill(alice.passiveSkillId).passiveEffects.Count;
            Assert.That(expected, Is.GreaterThan(0), "앨리스 패시브(51002)에 효과가 있어야 합니다.");

            var sources = UnitPassiveSources.Build(new[] { alice, alice, null }, content);

            Assert.That(sources, Has.Count.EqualTo(expected), "같은 유닛이 두 칸에 있어도 패시브는 한 번만 적용합니다.");
            Assert.That(sources.TrueForAll(source => source.SourceId == 100 &&
                source.Kind == OzGameLab01.Managers.RuntimeEffectManager.EffectSourceKind.UnitPassive), Is.True);
        }

        [Test]
        public void DebuffImmunityBlocksOnlyThatDebuffType()
        {
            Unit unit = CreateUnit(100f, attack: 10f);
            try
            {
                Assert.That(unit.AddDebuffImmunity(DebuffType.DamageOverTime), Is.True);

                unit.ApplyDebuff(new DebuffProfile { type = DebuffType.DamageOverTime, duration = 3f, magnitude = 5f, tickInterval = 1f });
                Assert.That(unit.HasAnyDebuff, Is.False);

                unit.ApplyDebuff(new DebuffProfile { type = DebuffType.Stun, duration = 1f });
                Assert.That(unit.HasAnyDebuff, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(unit.gameObject);
            }
        }

        [Test]
        public void FlatStatModifierAddsFixedAmount()
        {
            Unit unit = CreateUnit(100f, attack: 10f);
            var executor = new CombatEffectExecutor(new CombatFacade());
            try
            {
                MethodInfo apply = typeof(CombatEffectExecutor).GetMethod("ApplyEffect", BindingFlags.Instance | BindingFlags.NonPublic);
                var effect = new EffectInstance
                {
                    effect = EffectType.StatModifier,
                    statType = EffectStatType.Attack,
                    operation = EffectOperation.Add,
                    effectParam = 3f,
                    flatValue = true
                };

                Assert.That((bool)apply.Invoke(executor, new object[] { effect, unit }), Is.True);
                Assert.That(CurrentStat(unit, EffectStatType.Attack), Is.EqualTo(13f).Within(0.001f));
            }
            finally
            {
                executor.Dispose();
                Object.DestroyImmediate(unit.gameObject);
            }
        }

        private static float CurrentStat(Unit unit, EffectStatType stat)
        {
            var stats = (UnitCombatStats)typeof(Unit).GetField("_stats", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(unit);
            return stats.Get(stat);
        }

        private static Unit CreateUnit(float maxHp, float attack)
        {
            var unit = new GameObject("Passive test unit").AddComponent<Unit>();
            unit.Configure(new UnitData { healthPoint = maxHp, attackPoint = attack, skillIds = new List<int>() });
            typeof(Unit).GetMethod("EnsureRuntimeComponents", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(unit, null);
            typeof(Unit).GetMethod("InitializeRuntimeState", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(unit, null);
            return unit;
        }
    }
}
