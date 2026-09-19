using System;
using System.Collections.Generic;
using NUnit.Framework;
using OzGameLab01.Combat;
using OzGameLab01.Data;
using OzGameLab01.Events;

namespace OzGameLab01.Tests.EditMode
{
    public class CombatFoundationTests
    {
        [Test]
        public void BattleResolutionPublishesEndOnlyOnceBeforeUi()
        {
            var go = new UnityEngine.GameObject("Battle resolution test");
            var sequence = new List<string>();
            System.Action<bool> ended = won => sequence.Add("end");
            PassiveEventBus.OnBattleEnd += ended;
            float previousScale = UnityEngine.Time.timeScale;
            try
            {
                var controller = go.AddComponent<OzGameLab01.Controllers.CombatSceneController>();
                controller.OnBattleResolved += won => sequence.Add("ui");
                var resolve = controller.GetType().GetMethod("ResolveBattle",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                resolve.Invoke(controller, new object[] { false });
                resolve.Invoke(controller, new object[] { false });
                Assert.That(sequence, Is.EqualTo(new[] { "end", "ui" }));
                Assert.That(controller.IsResolved, Is.True);
            }
            finally
            {
                PassiveEventBus.OnBattleEnd -= ended;
                UnityEngine.Object.DestroyImmediate(go);
                UnityEngine.Time.timeScale = previousScale;
            }
        }

        [Test]
        public void CachedStatsUsePercentGroupsAndKeepDeclarationOrder()
        {
            var cache = new OzGameLab01.Effects.Models.RuntimeEffectCache<EffectInstance>(
                e => e, e => 0, e => 1, e => e.targetCount);
            cache.Rebuild(new[] {
                new EffectInstance { trigger = TriggerType.Always, effect = EffectType.StatModifier,
                    statType = EffectStatType.Attack, operation = EffectOperation.Multiply, effectParam = 5, targetCount = 0 },
                new EffectInstance { trigger = TriggerType.Always, effect = EffectType.StatModifier,
                    statType = EffectStatType.Attack, operation = EffectOperation.Add, effectParam = 50, targetCount = 1 },
                new EffectInstance { trigger = TriggerType.Always, effect = EffectType.StatModifier,
                    statType = EffectStatType.Attack, operation = EffectOperation.Multiply, effectParam = 5, targetCount = 2 }
            });
            Assert.That(cache.OrderedEffects[0].operation, Is.EqualTo(EffectOperation.Multiply));
            Assert.That(cache.TryGetAlwaysStatModifier(EffectStatType.Attack, default, out var modifier), Is.True);
            Assert.That(modifier.Apply(100), Is.EqualTo(165).Within(0.001f));
        }

        [Test]
        public void StatsSumEachOperationAndRestoreBaseOnExpiry()
        {
            var stats = new UnitCombatStats();
            stats.SetBase(EffectStatType.Attack, 100);
            stats.Add(EffectStatType.Attack, 20, EffectOperation.Add, 2, false);
            stats.Add(EffectStatType.Attack, 30, EffectOperation.Add, 2, false);
            stats.Add(EffectStatType.Attack, 10, EffectOperation.Multiply, 0, true);
            Assert.That(stats.Get(EffectStatType.Attack), Is.EqualTo(165).Within(0.001f));
            Assert.That(stats.Tick(2), Is.True);
            Assert.That(stats.Get(EffectStatType.Attack), Is.EqualTo(110).Within(0.001f));
            Assert.That(stats.GetBase(EffectStatType.Attack), Is.EqualTo(100));
        }

        [Test]
        public void ShieldConsumesOldestAndExpiryOnlyRemovesItsRemainder()
        {
            var shields = new UnitShieldPool();
            Assert.That(shields.Add(20, 1, false), Is.True);
            Assert.That(shields.Add(30, 3, false), Is.False);
            Assert.That(shields.Absorb(15), Is.Zero);
            shields.Tick(1);
            Assert.That(shields.Total, Is.EqualTo(30));
            Assert.That(shields.Absorb(40), Is.EqualTo(10));
            Assert.That(shields.Total, Is.Zero);
        }

        [Test]
        public void BattleEndShieldSurvivesTimeButClearsBetweenBattles()
        {
            var shields = new UnitShieldPool();
            shields.Add(25, 0, true);
            shields.Tick(999);
            Assert.That(shields.Total, Is.EqualTo(25));
            shields.Clear();
            Assert.That(shields.Total, Is.Zero);
            Assert.That(shields.Add(5, 1, false), Is.True);
        }

        [Test]
        public void InvalidShieldAndNegativeDamageDoNotAddProtection()
        {
            var shields = new UnitShieldPool();
            Assert.That(shields.Add(10, 0, false), Is.False);
            shields.Add(10, 1, false);
            Assert.That(shields.Absorb(-10), Is.Zero);
            Assert.That(shields.Total, Is.EqualTo(10));
            shields.Tick(2);
            Assert.That(shields.Total, Is.Zero);
        }

        [Test]
        public void EventChoiceEditsDoNotMutateCatalog()
        {
            var choice = new EventChoice();
            choice.SetEventChoice("original", "1");
            var row = new EventContent { id = "event", choices = new List<EventChoice> { choice } };
            var catalog = new ContentCatalog(new[] { new UnitData { id = 1 } },
                new[] { new MonsterData { id = 1 } }, new[] { new SkillData { id = 1 } },
                Array.Empty<SynergyData>(), Array.Empty<RelicData>(), events: new[] { row });
            choice.SetEventChoice("input changed", "2");
            catalog.GetEvent("event").choices[0].SetEventChoice("reward", "3");
            Assert.That(catalog.GetEvent("event").choices[0].ChoiceDialog, Is.EqualTo("original"));
        }

        [Test]
        public void EffectCatalogReadsDefinitionsNotOwnedMutableCopies()
        {
            var original = new UnitData { id = 1, passiveEffects = new List<EffectInstance>
                { new EffectInstance { trigger = TriggerType.OnBattleStart, effectParam = 25 } } };
            var catalog = new ContentCatalog(new[] { original }, new[] { new MonsterData { id = 1 } },
                new[] { new SkillData { id = 1 } }, Array.Empty<SynergyData>(), Array.Empty<RelicData>());
            var effects = new CombatEffectCatalog();
            effects.Rebuild(catalog, new[] { new UnitData { id = 1 } }, Array.Empty<RelicData>());
            var oldSnapshot = effects.GetEffects(TriggerType.OnBattleStart);
            Assert.That(oldSnapshot[0].Definition.effectParam, Is.EqualTo(25));
            effects.Rebuild(catalog, Array.Empty<UnitData>(), Array.Empty<RelicData>());
            Assert.That(effects.CachedEffectCount, Is.Zero);
            Assert.That(oldSnapshot.Count, Is.EqualTo(1));
        }

        [Test]
        public void InjectedSeedReproducesRandomSequence()
        {
            IRandomProvider first = new CombatRandom(7), second = new CombatRandom(7);
            for (int i = 0; i < 20; i++) Assert.That(first.Next(10), Is.EqualTo(second.Next(10)));
        }

        [Test]
        public void EnemyPreparationUsesGrowthRowAndReturnsDetachedCachedSpecs()
        {
            var catalog = new ContentCatalog(
                new[] { new UnitData { id = 1, skillIds = new List<int> { 1, 10 } } },
                new[] { new MonsterData { id = 1, type = MonsterType.normal, healthPoint = 999, skillIds = new List<int> { 1 } } },
                new[] { new SkillData { id = 1 }, new SkillData { id = 10 } },
                Array.Empty<SynergyData>(), Array.Empty<RelicData>(),
                new[] { new EnemyGrowthRow { id = 1, type = MonsterType.normal, step = 1, health = 70, attack = 3.5f, defense = 1.4f, attackInterval = 1, criticalMultiplier = 150, criticalChance = 10, dodgeChance = 10 } });
            var cache = new EnemyPreparationCache();
            var owned = new[] { new UnitData { id = 1, skillIds = new List<int> { 1, 10 } } };
            var first = cache.Prepare(catalog, 1, catalog.GetEnemy(1), 0, 0, 42, owned);
            first.healthPoint = 1;
            var second = cache.Prepare(catalog, 1, catalog.GetEnemy(1), 0, 0, 42, owned);
            Assert.That(second.healthPoint, Is.EqualTo(70));
            Assert.That(second.attackPoint, Is.EqualTo(4));
            Assert.That(second.skillIds, Does.Contain(10));
            Assert.That(cache.Count, Is.EqualTo(1));
        }

        [Test]
        public void EnemyPreparationCacheInvalidatesWhenOwnershipChanges()
        {
            var catalog = new ContentCatalog(
                new[] { new UnitData { id = 1, skillIds = new List<int> { 1, 10 } }, new UnitData { id = 2, skillIds = new List<int> { 1, 11 } } },
                new[] { new MonsterData { id = 1, type = MonsterType.normal, skillIds = new List<int> { 1 } } },
                new[] { new SkillData { id = 1 }, new SkillData { id = 10 }, new SkillData { id = 11 } },
                Array.Empty<SynergyData>(), Array.Empty<RelicData>(),
                new[] { new EnemyGrowthRow { id = 1, type = MonsterType.normal, step = 1, health = 70 } });
            var cache = new EnemyPreparationCache();
            var first = cache.Prepare(catalog, 1, catalog.GetEnemy(1), 0, 0, 42,
                new[] { new UnitData { id = 1, skillIds = new List<int> { 1, 10 } } });
            var second = cache.Prepare(catalog, 1, catalog.GetEnemy(1), 0, 0, 42,
                new[] { new UnitData { id = 2, skillIds = new List<int> { 1, 11 } } });
            Assert.That(first.skillIds, Does.Contain(10));
            Assert.That(second.skillIds, Does.Contain(11));
            Assert.That(cache.Count, Is.EqualTo(2));
        }
    }
}
