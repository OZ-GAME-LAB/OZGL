using System;
using System.Collections.Generic;
using NUnit.Framework;
using OzGameLab01.Data;

namespace OzGameLab01.Tests.EditMode
{
    public class RuntimeContentServiceTests
    {
        private static ContentCatalog Create(UnitData unit = null)
            => new ContentCatalog(new[] { unit ?? new UnitData { id = 1, skillIds = new List<int> { 9 } } },
                new[] { new MonsterData { id = 2, skillIds = new List<int> { 9 } } },
                new[] { new SkillData { id = 9, damage = 10 } },
                Array.Empty<SynergyData>(), Array.Empty<RelicData>());

        [Test]
        public void SnapshotDetachesInputAndEveryReadIncludingNestedLists()
        {
            var unit = new UnitData { id = 1, healthPoint = 100, skillIds = new List<int> { 9 } };
            ContentCatalog catalog = Create(unit);
            unit.healthPoint = 0;
            unit.skillIds.Clear();
            UnitData first = catalog.GetUnit(1);
            first.healthPoint = 3;
            first.skillIds.Clear();
            Assert.That(catalog.GetUnit(1).healthPoint, Is.EqualTo(100));
            Assert.That(catalog.GetUnit(1).skillIds, Is.EqualTo(new[] { 9 }));
            Assert.That(catalog.GetUnit(1).color.r, Is.EqualTo(0.75f));
        }

        [Test]
        public void MissingSkillRejectsWholeSnapshot()
            => Assert.Throws<ArgumentException>(() => Create(new UnitData { id = 1, skillIds = new List<int> { 99 } }));

        [Test]
        public void DuplicateIdsAreRejected()
            => Assert.Throws<ArgumentException>(() => new ContentTable<UnitData>(
                new[] { new UnitData { id = 1 }, new UnitData { id = 1 } }, row => row.id));

        [Test]
        public void FailedReloadPreservesRevisionAndPreviousSnapshotThenRecovers()
        {
            bool fail = false;
            using var service = new RuntimeContentService(() => fail ? throw new InvalidOperationException("offline") : Create());
            Assert.That(service.Reload(), Is.True);
            ContentCatalog first = service.Catalog;
            fail = true;
            Assert.That(service.Reload(), Is.False);
            Assert.That(service.IsReady, Is.True);
            Assert.That(service.Catalog, Is.SameAs(first));
            Assert.That(service.Revision, Is.EqualTo(1));
            fail = false;
            Assert.That(service.Reload(), Is.True);
            Assert.That(service.LastError, Is.Null);
            Assert.That(service.Revision, Is.EqualTo(2));
        }

        [Test]
        public void FirstFailureDoesNotExposeReadyCatalog()
        {
            using var service = new RuntimeContentService(() => throw new InvalidOperationException());
            Assert.That(service.Reload(), Is.False);
            Assert.That(service.IsReady, Is.False);
            Assert.Throws<InvalidOperationException>(() => { _ = service.Catalog; });
        }

        [Test]
        public void NotificationsSeeCompleteSnapshotAndCannotReenterReload()
        {
            using var service = new RuntimeContentService(() => Create());
            int notifications = 0;
            service.Notification += notification =>
            {
                Assert.That(service.Catalog.GetSkill(9), Is.Not.Null);
                Assert.That(service.Reload(), Is.False);
                notifications++;
            };
            Assert.That(service.Reload(), Is.True);
            Assert.That(notifications, Is.EqualTo(8));
        }

        [Test]
        public void RealResourcesProduceValidDetachedCatalog()
        {
            ContentCatalog catalog = ResourcesContentLoader.Load();
            Assert.That(catalog.UnitCount, Is.GreaterThan(0));
            Assert.That(catalog.EnemyCount, Is.GreaterThan(0));
            Assert.That(catalog.Relics.Count, Is.GreaterThan(0));
            Assert.That(catalog.EnemyGrowth.Count, Is.EqualTo(93));
            Assert.That(catalog.EnemyGrowth[200003].health, Is.EqualTo(1050));
            foreach (UnitData unit in catalog.Units)
                foreach (int id in unit.skillIds) Assert.That(catalog.GetSkill(id), Is.Not.Null);
        }

        [Test]
        public void RealResourcesSynergyEffectsHaveImplementedRuntimeTypes()
        {
            ContentCatalog catalog = ResourcesContentLoader.Load();
            var supported = new HashSet<string>
            {
                "CooldownDecrease",
                "DecreaseDmgDefense",
                "ExtraDamageOnStatus",
                "FixedDamage",
                "IncreaseDamage",
                "NoSkillStatBuff",
                "RecoveryIncrease",
                "ShieldBonusDamage",
                "ShieldOnStart",
                "StatBuff",
                "StatusEffect",
                "UseSkillTwoTimes"
            };
            int effectCount = 0;
            foreach (SynergyData synergy in catalog.Synergies.Values)
            {
                foreach (SynergyTier tier in synergy.tiers)
                {
                    foreach (SynergyEffectNode effect in tier.effects)
                    {
                        effectCount++;
                        Assert.That(supported, Does.Contain(effect.effectType),
                            $"Unsupported synergy effect: {synergy.id}/{tier.tierLevel}/{effect.effectType}");
                    }
                }
            }

            Assert.That(catalog.Synergies.Count, Is.EqualTo(12));
            Assert.That(effectCount, Is.EqualTo(50));
        }
    }
}
