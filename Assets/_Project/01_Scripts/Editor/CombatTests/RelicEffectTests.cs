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
        // 효과 연결이 보류된 유물(Docs/RELIC_EFFECT_STATUS.md 3절).
        private static readonly HashSet<int> PendingRelicIds = new HashSet<int> { 718, 726, 727 };

        [Test]
        public void EveryRelicExceptPendingHasEffects()
        {
            TextAsset json = Resources.Load<TextAsset>("RelicData");
            List<RelicData> relics = JsonDataParser.Parse<RelicData, RelicDataList>(json.text);

            foreach (RelicData relic in relics)
            {
                if (PendingRelicIds.Contains(relic.id)) continue;
                Assert.That(relic.effects, Is.Not.Empty, $"RelicData.id={relic.id} ({relic.name}) effects가 비어 있습니다.");
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

        private static Unit CreateUnit(string name, float maxHp)
        {
            var unit = new GameObject(name).AddComponent<Unit>();
            unit.Configure(new UnitData { healthPoint = maxHp });
            typeof(Unit).GetMethod("EnsureRuntimeComponents", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .Invoke(unit, null);
            typeof(Unit).GetMethod("InitializeRuntimeState", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .Invoke(unit, null);
            return unit;
        }
    }
}
