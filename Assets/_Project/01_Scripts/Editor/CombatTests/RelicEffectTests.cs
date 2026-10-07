using System.Collections.Generic;
using NUnit.Framework;
using OzGameLab01.Combat;
using OzGameLab01.Common;
using OzGameLab01.Data;
using OzGameLab01.Effects.Models;
using OzGameLab01.Managers;
using UnityEngine;

namespace OzGameLab01.Tests.EditMode
{
    public class RelicEffectTests
    {
        [Test]
        public void EveryRelicHasEffects()
        {
            TextAsset json = Resources.Load<TextAsset>("RelicData");
            List<RelicData> relics = JsonDataParser.Parse<RelicData, RelicDataList>(json.text);

            foreach (RelicData relic in relics)
            {
                Assert.That(relic.effects, Is.Not.Empty, $"RelicData.id={relic.id} ({relic.name}) effects가 비어 있습니다.");
            }
        }

        [Test]
        public void UnitPassiveValuesUsePercentScale()
        {
            TextAsset json = Resources.Load<TextAsset>("SkillData");
            List<SkillData> skills = JsonDataParser.Parse<SkillData, SkillDataList>(json.text);

            foreach (SkillData skill in skills)
            {
                if (skill.passiveEffects == null) continue;
                foreach (EffectInstance effect in skill.passiveEffects)
                {
                    Assert.That(effect.chance == 0f || effect.chance > 1f, Is.True,
                        $"SkillData.id={skill.id} chance={effect.chance}는 0~100 퍼센트여야 합니다.");
                }
            }
        }

        [Test]
        public void CooldownModifierRecoversPercentOfActiveSkillCooldown()
        {
            Unit unit = CreateSkilledUnit();
            try
            {
                unit.TryGetActiveSkillCooldown(out float before, out float duration);
                Assert.That(unit.RecoverSkillCooldownPercent(10f), Is.True);
                unit.TryGetActiveSkillCooldown(out float after, out _);
                Assert.That(after, Is.EqualTo(before - duration * 0.1f).Within(0.001f));
            }
            finally
            {
                Object.DestroyImmediate(unit.gameObject);
            }
        }

        [Test]
        public void SkillCooldownReductionShortensActiveSkillCooldown()
        {
            Unit unit = CreateSkilledUnit();
            try
            {
                unit.TryGetActiveSkillCooldown(out _, out float before);
                Assert.That(unit.ApplyStatEffect(EffectStatType.SkillCooldownReduction, 10f, EffectOperation.Multiply, 0f, true), Is.True);
                unit.TryGetActiveSkillCooldown(out float remaining, out float after);
                Assert.That(after, Is.EqualTo(before * 0.9f).Within(0.001f));
                Assert.That(remaining, Is.LessThanOrEqualTo(after + 0.0001f));
            }
            finally
            {
                Object.DestroyImmediate(unit.gameObject);
            }
        }

        [Test]
        public void ReceivedDebuffDurationAddsSecondsAndScalesByPercent()
        {
            Unit addUnit = CreateUnit("Debuff add", 100f);
            Unit halfUnit = CreateUnit("Debuff half", 100f);
            try
            {
                addUnit.ModifyReceivedDebuffDuration(EffectOperation.Add, 1f);
                addUnit.ApplyDebuff(new DebuffProfile { type = DebuffType.Stun, duration = 1f });
                addUnit.TryGetPrimaryDebuff(out _, out _, out float added);
                Assert.That(added, Is.EqualTo(2f).Within(0.001f));

                halfUnit.ModifyReceivedDebuffDuration(EffectOperation.Multiply, -50f);
                halfUnit.ApplyDebuff(new DebuffProfile { type = DebuffType.Stun, duration = 1f });
                halfUnit.TryGetPrimaryDebuff(out _, out _, out float halved);
                Assert.That(halved, Is.EqualTo(0.5f).Within(0.001f));
            }
            finally
            {
                Object.DestroyImmediate(addUnit.gameObject);
                Object.DestroyImmediate(halfUnit.gameObject);
            }
        }

        [Test]
        public void AddBasicAttackRateConvertsRateIncreaseIntoShorterCooldown()
        {
            Unit unit = CreateUnit("Attack rate", 100f, attackSpeed: 1f);
            try
            {
                // 초당 1회 + 0.25회 = 1.25회 → 쿨다운 0.8초
                Assert.That(unit.AddBasicAttackRate(0.25f), Is.True);
                var skills = (System.Collections.IList)typeof(Unit)
                    .GetField("_skills", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                    .GetValue(unit);
                Assert.That(((UnitSkillRuntime)skills[0]).Cooldown, Is.EqualTo(0.8f).Within(0.001f));
            }
            finally
            {
                Object.DestroyImmediate(unit.gameObject);
            }
        }

        [TestCase(EffectCondition.AllyCountAtMost, 4f, 4, true)]
        [TestCase(EffectCondition.AllyCountAtMost, 4f, 5, false)]
        [TestCase(EffectCondition.FrontRowCountEquals, 1f, 1, true)]
        [TestCase(EffectCondition.FrontRowCountEquals, 1f, 2, false)]
        [TestCase(EffectCondition.ActiveSynergyCountAtLeast, 3f, 3, true)]
        [TestCase(EffectCondition.ActiveSynergyCountAtLeast, 3f, 2, false)]
        [TestCase(EffectCondition.ActiveSynergyCountEquals, 0f, 0, true)]
        [TestCase(EffectCondition.ActiveSynergyCountEquals, 0f, 1, false)]
        public void ConditionComparesAgainstContext(EffectCondition condition, float param, int value, bool expected)
        {
            var effect = new EffectInstance { condition = condition, conditionParam = param };
            var context = new EffectConditionContext(value, value, value, false);

            Assert.That(EffectConditionEvaluator.IsMet(effect, context), Is.EqualTo(expected));
        }

        [Test]
        public void NotNightBattleConditionBlocksNightBattles()
        {
            var effect = new EffectInstance { condition = EffectCondition.NotNightBattle };

            Assert.That(EffectConditionEvaluator.IsMet(effect, new EffectConditionContext(0, 0, 0, false)), Is.True);
            Assert.That(EffectConditionEvaluator.IsMet(effect, new EffectConditionContext(0, 0, 0, true)), Is.False);
        }

        [Test]
        public void PromoteTierMovesUpByStepsAndStopsAtHighestTier()
        {
            var tier2 = new SynergyTier { requiredCount = 2 };
            var tier3 = new SynergyTier { requiredCount = 3 };
            var tier4 = new SynergyTier { requiredCount = 4 };
            var data = new SynergyData { tiers = new List<SynergyTier> { tier4, tier2, tier3 } };

            Assert.That(SynergyModel.PromoteTier(data, tier2, 1), Is.SameAs(tier3));
            Assert.That(SynergyModel.PromoteTier(data, tier2, 5), Is.SameAs(tier4));
            Assert.That(SynergyModel.PromoteTier(data, tier2, 0), Is.SameAs(tier2));
            Assert.That(SynergyModel.PromoteTier(data, null, 1), Is.Null, "비활성 시너지는 새로 켜지 않습니다.");
        }

        [Test]
        public void ConditionalEffectOnlyFiresWhenFrontRowHasExactlyOneAlly()
        {
            var effect = new EffectInstance
            {
                trigger = TriggerType.OnBattleStart,
                target = EffectTarget.FrontRow,
                effect = EffectType.GrantShield,
                effectParam = 10f,
                untilBattleEnd = true,
                condition = EffectCondition.FrontRowCountEquals,
                conditionParam = 1f
            };

            Assert.That(RunBattleStart(effect, frontCount: 2), Is.EqualTo(0f).Within(0.001f));
            Assert.That(RunBattleStart(effect, frontCount: 1), Is.EqualTo(10f).Within(0.001f));
        }

        [Test]
        public void PercentHealUsesMissingHpWhenRequested()
        {
            var sessionObject = new GameObject("Relic heal session");
            sessionObject.SetActive(false);
            var session = sessionObject.AddComponent<CombatSession>();
            Unit unit = CreateUnit("Relic heal ally", 100f);
            var catalog = BuildCatalog(new EffectInstance
            {
                trigger = TriggerType.OnBattleEnd,
                target = EffectTarget.AllAllies,
                effect = EffectType.Heal,
                effectParam = 50f,
                effectParamIsPercent = true,
                percentOfMissingHp = true,
                condition = EffectCondition.NotNightBattle
            });
            CombatEffectExecutor executor = null;
            try
            {
                session.State.SlotUnits[0, (int)CombatManager.SlotRow.Front] = unit;
                SystemBus.Unregister<CombatEffectCatalog>();
                SystemBus.Register(catalog);
                executor = new CombatEffectExecutor(new CombatFacade(), new CombatRandom(7));

                unit.TakeDamage(40f);
                PassiveEventBus.RaiseBattleEnd(true);

                Assert.That(unit.CurrentHp, Is.EqualTo(80f).Within(0.001f), "잃은 체력 40의 50%인 20만 회복해야 합니다.");
            }
            finally
            {
                executor?.Dispose();
                PassiveEventBus.ResetRunState();
                SystemBus.Unregister<CombatEffectCatalog>(catalog);
                Object.DestroyImmediate(unit.gameObject);
                Object.DestroyImmediate(sessionObject);
            }
        }

        private static float RunBattleStart(EffectInstance effect, int frontCount)
        {
            var sessionObject = new GameObject("Relic condition session");
            sessionObject.SetActive(false);
            var session = sessionObject.AddComponent<CombatSession>();
            var units = new List<Unit>();
            var catalog = BuildCatalog(effect);
            CombatEffectExecutor executor = null;
            try
            {
                for (int column = 0; column < frontCount; column++)
                {
                    Unit unit = CreateUnit($"Relic condition front {column}", 100f);
                    session.State.SlotUnits[column, (int)CombatManager.SlotRow.Front] = unit;
                    units.Add(unit);
                }

                SystemBus.Unregister<CombatEffectCatalog>();
                SystemBus.Register(catalog);
                executor = new CombatEffectExecutor(new CombatFacade(), new CombatRandom(7));
                PassiveEventBus.RaiseBattleStart();
                return units[0].Shield;
            }
            finally
            {
                executor?.Dispose();
                PassiveEventBus.ResetRunState();
                SystemBus.Unregister<CombatEffectCatalog>(catalog);
                foreach (Unit unit in units) Object.DestroyImmediate(unit.gameObject);
                Object.DestroyImmediate(sessionObject);
            }
        }

        private static CombatEffectCatalog BuildCatalog(EffectInstance effect)
        {
            var catalog = new CombatEffectCatalog();
            catalog.Rebuild(null, null, null, new[]
            {
                new RuntimeEffectManager.EffectSource(RuntimeEffectManager.EffectSourceKind.Relic, 999, effect, 0)
            });
            return catalog;
        }

        // 기본공격(900)과 액티브 스킬(51001)을 가진 유닛. 스킬 정의는 Resources 카탈로그에서 읽습니다.
        private static Unit CreateSkilledUnit()
        {
            var unit = new GameObject("Skilled unit").AddComponent<Unit>();
            unit.Configure(new UnitData { healthPoint = 100f, attackSpeed = 1f, skillIds = new List<int> { 900, 51001 } });
            InitializeUnit(unit);
            return unit;
        }

        private static Unit CreateUnit(string name, float maxHp, float attackSpeed = 0f)
        {
            var unit = new GameObject(name).AddComponent<Unit>();
            unit.Configure(new UnitData { healthPoint = maxHp, attackSpeed = attackSpeed, skillIds = attackSpeed > 0f ? new List<int> { 900 } : new List<int>() });
            InitializeUnit(unit);
            return unit;
        }

        private static void InitializeUnit(Unit unit)
        {
            typeof(Unit).GetMethod("EnsureRuntimeComponents", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .Invoke(unit, null);
            typeof(Unit).GetMethod("InitializeRuntimeState", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .Invoke(unit, null);
        }
    }
}
