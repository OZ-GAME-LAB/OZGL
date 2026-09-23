using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using OzGameLab01.Common;
using OzGameLab01.Combat;
using OzGameLab01.Data;
using OzGameLab01.Events;
using OzGameLab01.Managers;

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
        public void CombatPauseReasonsDoNotReleaseEachOther()
        {
            var go = new UnityEngine.GameObject("Combat pause reason test");
            float previousScale = UnityEngine.Time.timeScale;

            try
            {
                var controller =
                    go.AddComponent<OzGameLab01.Controllers.CombatSceneController>();
                controller.SetFastForward(true);
                controller.SetPauseReason(
                    OzGameLab01.Controllers.CombatSceneController.PauseReason.UserInterface,
                    true);
                controller.SetPauseReason(
                    OzGameLab01.Controllers.CombatSceneController.PauseReason.Tutorial,
                    true);

                controller.SetPauseReason(
                    OzGameLab01.Controllers.CombatSceneController.PauseReason.UserInterface,
                    false);

                Assert.That(controller.IsPaused, Is.True);
                Assert.That(UnityEngine.Time.timeScale, Is.Zero);

                controller.SetPauseReason(
                    OzGameLab01.Controllers.CombatSceneController.PauseReason.Tutorial,
                    false);

                Assert.That(controller.IsPaused, Is.False);
                Assert.That(UnityEngine.Time.timeScale, Is.EqualTo(2f));
            }
            finally
            {
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
        public void EffectCatalogIncludesBattleLocalSynergySources()
        {
            var catalog = new ContentCatalog(new[] { new UnitData { id = 1 } },
                new[] { new MonsterData { id = 1 } }, new[] { new SkillData { id = 1 } },
                Array.Empty<SynergyData>(), Array.Empty<RelicData>());
            var synergy = new OzGameLab01.Managers.RuntimeEffectManager.EffectSource(
                OzGameLab01.Managers.RuntimeEffectManager.EffectSourceKind.Synergy, 7,
                new EffectInstance { trigger = TriggerType.Always, effect = EffectType.StatModifier,
                    statType = EffectStatType.Attack, target = EffectTarget.AllAllies }, 0);
            var effects = new CombatEffectCatalog();
            effects.Rebuild(catalog, Array.Empty<UnitData>(), Array.Empty<RelicData>(), new[] { synergy });

            Assert.That(effects.GetEffects(TriggerType.Always), Has.Count.EqualTo(1));
            Assert.That(effects.GetEffects(TriggerType.Always)[0].Kind,
                Is.EqualTo(OzGameLab01.Managers.RuntimeEffectManager.EffectSourceKind.Synergy));
        }

        [Test]
        public void InjectedSeedReproducesRandomSequence()
        {
            IRandomProvider first = new CombatRandom(7), second = new CombatRandom(7);
            for (int i = 0; i < 20; i++) Assert.That(first.Next(10), Is.EqualTo(second.Next(10)));
        }

        [Test]
        public void TargetSelectorRandomUsesEachTargetAtMostOnceAndSkipsDeadUnits()
        {
            var aliveA = CreateUnit("random alive A", 100f);
            var aliveB = CreateUnit("random alive B", 100f);
            var dead = CreateUnit("random dead", 100f);
            dead.TakeDamage(100f);
            try
            {
                var result = CombatTargetSelector.SelectRandom(
                    new[] { aliveA, dead, aliveB }, 3, new CombatRandom(7));
                Assert.That(result, Is.EqualTo(new[] { aliveA, aliveB }));
                Assert.That(new HashSet<Unit>(result).Count, Is.EqualTo(result.Count));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(aliveA.gameObject);
                UnityEngine.Object.DestroyImmediate(aliveB.gameObject);
                UnityEngine.Object.DestroyImmediate(dead.gameObject);
            }
        }

        [Test]
        public void TargetSelectorWorstHpUsesCurrentHpRatioAndDeclarationOrderForTies()
        {
            var full = CreateUnit("worst full", 100f);
            var half = CreateUnit("worst half", 100f);
            var low = CreateUnit("worst low", 200f);
            half.TakeDamage(50f);
            low.TakeDamage(150f);
            try
            {
                var result = CombatTargetSelector.SelectWorstHp(new[] { full, half, low }, 2);
                Assert.That(result, Is.EqualTo(new[] { low, half }));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(full.gameObject);
                UnityEngine.Object.DestroyImmediate(half.gameObject);
                UnityEngine.Object.DestroyImmediate(low.gameObject);
            }
        }

        [Test]
        public void HpThresholdUsesPartyAggregateIncludesDeadAndFiresOnlyOnceBelowBoundary()
        {
            var sessionObject = new UnityEngine.GameObject("HP threshold session");
            sessionObject.SetActive(false);
            var session = sessionObject.AddComponent<CombatSession>();
            var front = CreateUnit("threshold front", 100f);
            var back = CreateUnit("threshold back", 100f);
            var catalog = BuildThresholdCatalog(EffectTarget.AllAllies);
            CombatEffectExecutor executor = null;
            try
            {
                session.State.SlotUnits[0, (int)CombatManager.SlotRow.Front] = front;
                session.State.SlotUnits[0, (int)CombatManager.SlotRow.Back] = back;
                SystemBus.Unregister<CombatEffectCatalog>();
                SystemBus.Register(catalog);
                executor = new CombatEffectExecutor(new CombatFacade(), new CombatRandom(7));

                front.TakeDamage(100f);
                Assert.That(back.Shield, Is.EqualTo(0f).Within(0.001f),
                    "Exactly 50% party HP must not satisfy a '< 50%' trigger.");

                back.TakeDamage(1f);
                Assert.That(back.Shield, Is.EqualTo(10f).Within(0.001f),
                    "The dead ally must remain in the party aggregate denominator.");

                back.Heal(1f);
                back.TakeDamage(11f);
                Assert.That(back.Shield, Is.EqualTo(0f).Within(0.001f),
                    "The same threshold effect may fire only once per battle.");
            }
            finally
            {
                executor?.Dispose();
                PassiveEventBus.ResetRunState();
                SystemBus.Unregister<CombatEffectCatalog>(catalog);
                UnityEngine.Object.DestroyImmediate(front.gameObject);
                UnityEngine.Object.DestroyImmediate(back.gameObject);
                UnityEngine.Object.DestroyImmediate(sessionObject);
            }
        }

        [Test]
        public void HpThresholdUsesConfiguredRowAndIgnoresDamageInOtherRows()
        {
            var sessionObject = new UnityEngine.GameObject("Row threshold session");
            sessionObject.SetActive(false);
            var session = sessionObject.AddComponent<CombatSession>();
            var front = CreateUnit("row threshold front", 100f);
            var back = CreateUnit("row threshold back", 100f);
            var catalog = BuildThresholdCatalog(EffectTarget.FrontRow);
            CombatEffectExecutor executor = null;
            try
            {
                session.State.SlotUnits[0, (int)CombatManager.SlotRow.Front] = front;
                session.State.SlotUnits[0, (int)CombatManager.SlotRow.Back] = back;
                SystemBus.Unregister<CombatEffectCatalog>();
                SystemBus.Register(catalog);
                executor = new CombatEffectExecutor(new CombatFacade(), new CombatRandom(7));

                back.TakeDamage(100f);
                Assert.That(front.Shield, Is.EqualTo(0f).Within(0.001f));

                front.TakeDamage(50f);
                Assert.That(front.Shield, Is.EqualTo(0f).Within(0.001f),
                    "Exactly 50% row HP must not fire.");

                front.TakeDamage(1f);
                Assert.That(front.Shield, Is.EqualTo(10f).Within(0.001f));
            }
            finally
            {
                executor?.Dispose();
                PassiveEventBus.ResetRunState();
                SystemBus.Unregister<CombatEffectCatalog>(catalog);
                UnityEngine.Object.DestroyImmediate(front.gameObject);
                UnityEngine.Object.DestroyImmediate(back.gameObject);
                UnityEngine.Object.DestroyImmediate(sessionObject);
            }
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

        [Test]
        public void EnemyPreparationUsesRealGrowthStepsAndFinalBossFallsBackToSemibossStageThree()
        {
            ContentCatalog content = ResourcesContentLoader.Load();
            EnemyGrowthRow normalFirst = content.EnemyGrowth.Values.Single(row =>
                row.type == MonsterType.normal && row.step == 1);
            EnemyGrowthRow normalLast = content.EnemyGrowth.Values
                .Where(row => row.type == MonsterType.normal)
                .OrderByDescending(row => row.step)
                .First();
            EnemyGrowthRow finalBossFallback = content.EnemyGrowth.Values.Single(row =>
                row.type == MonsterType.semiboss && row.step == 3);
            var cache = new EnemyPreparationCache();
            var normal = new MonsterData { id = 9001, type = MonsterType.normal };
            var boss = new MonsterData { id = 9002, type = MonsterType.boss };

            MonsterData first = cache.Prepare(content, 1, normal, 0, 0, 123,
                Array.Empty<UnitData>());
            MonsterData capped = cache.Prepare(content, 1, normal, 100, 10, 123,
                Array.Empty<UnitData>());
            MonsterData finalBoss = cache.Prepare(content, 1, boss, 0, 0, 123,
                Array.Empty<UnitData>());

            Assert.That(first.healthPoint, Is.EqualTo(UnityEngine.Mathf.RoundToInt(normalFirst.health)));
            Assert.That(capped.healthPoint, Is.EqualTo(UnityEngine.Mathf.RoundToInt(normalLast.health)));
            Assert.That(finalBoss.healthPoint,
                Is.EqualTo(UnityEngine.Mathf.RoundToInt(finalBossFallback.health)));
            Assert.That(finalBoss.attackPoint,
                Is.EqualTo(UnityEngine.Mathf.RoundToInt(finalBossFallback.attack)));
        }

        [Test]
        public void EnemyPreparationSkipsFullWaveOnElitesAndWrapsTurnWithinWave()
        {
            ContentCatalog content = ResourcesContentLoader.Load();
            var cache = new EnemyPreparationCache();
            var normal = new MonsterData { id = 9003, type = MonsterType.normal };

            // 중간보스 0마리, 15턴째(파도 경계 직전) — 해당 파도의 마지막 행(step 15)이어야 한다.
            MonsterData beforeWave = cache.Prepare(content, 1, normal, 14, 0, 1, Array.Empty<UnitData>());
            EnemyGrowthRow step15 = content.EnemyGrowth.Values.Single(
                row => row.type == MonsterType.normal && row.step == 15);
            Assert.That(beforeWave.healthPoint, Is.EqualTo(UnityEngine.Mathf.RoundToInt(step15.health)));

            // 중간보스 1마리 처치 직후(같은 턴) — 다음 파도 시작 행(step 16, +1.07 점프)으로
            // 즉시 넘어가야 하며, 파도 안에서의 진행은 누적 턴 수를 15로 나눈 나머지로 정한다.
            MonsterData afterFirstElite = cache.Prepare(content, 1, normal, 0, 1, 1, Array.Empty<UnitData>());
            EnemyGrowthRow step16 = content.EnemyGrowth.Values.Single(
                row => row.type == MonsterType.normal && row.step == 16);
            Assert.That(afterFirstElite.healthPoint, Is.EqualTo(UnityEngine.Mathf.RoundToInt(step16.health)));

            // 두 번째 중간보스 처치 후 5턴 진행 — 세 번째 파도(step 31~) 안에서 5턴만큼 진행한
            // step 36이어야 한다(턴 수가 파도 경계를 넘어도 wave index는 elites 기준으로 고정).
            MonsterData secondWaveProgress = cache.Prepare(content, 1, normal, 5, 2, 1, Array.Empty<UnitData>());
            EnemyGrowthRow step36 = content.EnemyGrowth.Values.Single(
                row => row.type == MonsterType.normal && row.step == 36);
            Assert.That(secondWaveProgress.healthPoint, Is.EqualTo(UnityEngine.Mathf.RoundToInt(step36.health)));
        }

        [Test]
        public void EnemyPreparationNeverWeakensAsTurnsPassWithoutElites()
        {
            // 중간보스를 한 마리도 못 잡은 채 파도 경계(15턴)를 넘겨도, turnCount % 15로 순환시켜
            // 최약체로 되돌아가면 안 된다 — 시간 경과만으로도 계속 다음 파도로 넘어가야 한다.
            ContentCatalog content = ResourcesContentLoader.Load();
            var cache = new EnemyPreparationCache();
            var normal = new MonsterData { id = 9004, type = MonsterType.normal };

            MonsterData turn20 = cache.Prepare(content, 1, normal, 20, 0, 1, Array.Empty<UnitData>());
            EnemyGrowthRow step21 = content.EnemyGrowth.Values.Single(
                row => row.type == MonsterType.normal && row.step == 21);
            Assert.That(turn20.healthPoint, Is.EqualTo(UnityEngine.Mathf.RoundToInt(step21.health)));

            MonsterData turn30 = cache.Prepare(content, 1, normal, 30, 0, 1, Array.Empty<UnitData>());
            EnemyGrowthRow step31 = content.EnemyGrowth.Values.Single(
                row => row.type == MonsterType.normal && row.step == 31);
            Assert.That(turn30.healthPoint, Is.EqualTo(UnityEngine.Mathf.RoundToInt(step31.health)));
        }

        [Test]
        public void SkillEffectsExecuteInDeclarationOrderAtImpact()
        {
            var casterObject = new UnityEngine.GameObject("Skill caster");
            var targetObject = new UnityEngine.GameObject("Skill target");
            try
            {
                var caster = casterObject.AddComponent<Unit>();
                var target = targetObject.AddComponent<Unit>();
                typeof(Unit).GetMethod("EnsureRuntimeComponents", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                    .Invoke(caster, null);
                typeof(Unit).GetMethod("EnsureRuntimeComponents", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                    .Invoke(target, null);
                typeof(Unit).GetMethod("InitializeRuntimeState", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                    .Invoke(caster, null);
                typeof(Unit).GetMethod("InitializeRuntimeState", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                    .Invoke(target, null);
                var effects = new[]
                {
                    new EffectInstance { effect = EffectType.GrantShield, target = EffectTarget.Enemy, effectParam = 10, untilBattleEnd = true },
                    new EffectInstance { effect = EffectType.DealDamage, target = EffectTarget.Enemy, effectParam = 15 }
                };
                typeof(Unit).GetMethod("ExecuteSkillEffects", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                    .Invoke(caster, new object[] { effects, target, 1f });
                Assert.That(target.CurrentHp, Is.EqualTo(95).Within(0.001f));
                Assert.That(target.Shield, Is.EqualTo(0).Within(0.001f));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(casterObject);
                UnityEngine.Object.DestroyImmediate(targetObject);
            }
        }

        [Test]
        public void ReviveRestoresDeadUnitAndRegistersItAgain()
        {
            var unitObject = new UnityEngine.GameObject("Revive target");
            try
            {
                var unit = unitObject.AddComponent<Unit>();
                typeof(Unit).GetMethod("EnsureRuntimeComponents", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                    .Invoke(unit, null);
                typeof(Unit).GetMethod("InitializeRuntimeState", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                    .Invoke(unit, null);

                unit.TakeDamage(200f);
                Assert.That(unit.IsDead, Is.True);
                Assert.That(unit.Revive(30f), Is.True);
                Assert.That(unit.IsDead, Is.False);
                Assert.That(unit.CurrentHp, Is.EqualTo(30f).Within(0.001f));
                Assert.That(CombatUnitRegistry.Units, Does.Contain(unit));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(unitObject);
            }
        }

        [Test]
        public void RecoveryAmountModifierAmplifiesHealWithoutChangingMaxHp()
        {
            var unitObject = new UnityEngine.GameObject("Recovery target");
            try
            {
                var unit = unitObject.AddComponent<Unit>();
                typeof(Unit).GetMethod("EnsureRuntimeComponents", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                    .Invoke(unit, null);
                typeof(Unit).GetMethod("InitializeRuntimeState", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                    .Invoke(unit, null);
                unit.TakeDamage(50f);
                Assert.That(unit.ApplyStatEffect(EffectStatType.RecoveryAmount, 20f), Is.True);
                Assert.That(unit.Heal(10f), Is.True);
                Assert.That(unit.CurrentHp, Is.EqualTo(62f).Within(0.001f));
                Assert.That(unit.MaxHp, Is.EqualTo(100f).Within(0.001f));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(unitObject);
            }
        }

        [Test]
        public void FixedDamageOverridesRequestedAttackDamage()
        {
            var casterObject = new UnityEngine.GameObject("Fixed damage caster");
            var targetObject = new UnityEngine.GameObject("Fixed damage target");
            try
            {
                var caster = casterObject.AddComponent<Unit>();
                var target = targetObject.AddComponent<Unit>();
                InitializeUnit(caster);
                InitializeUnit(target);
                caster.SetFixedDamage(30f);
                typeof(Unit).GetMethod("ApplySkillDamage", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                    .Invoke(caster, new object[] { target, 999f });
                Assert.That(target.CurrentHp, Is.EqualTo(70f).Within(0.001f));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(casterObject);
                UnityEngine.Object.DestroyImmediate(targetObject);
            }
        }

        [Test]
        public void ExtraDamageOnStatusAmplifiesDamageAgainstDebuffedTarget()
        {
            var casterObject = new UnityEngine.GameObject("Status damage caster");
            var targetObject = new UnityEngine.GameObject("Status damage target");
            try
            {
                var caster = casterObject.AddComponent<Unit>();
                var target = targetObject.AddComponent<Unit>();
                InitializeUnit(caster);
                InitializeUnit(target);
                caster.SetExtraDamageOnStatus(20f);
                target.ApplyDebuff(new DebuffProfile { type = DebuffType.Stun, duration = 5f });
                typeof(Unit).GetMethod("ApplySkillDamage", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                    .Invoke(caster, new object[] { target, 10f });
                Assert.That(target.CurrentHp, Is.EqualTo(88f).Within(0.001f));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(casterObject);
                UnityEngine.Object.DestroyImmediate(targetObject);
            }
        }

        [Test]
        public void StatusEffectSynergyAppliesRandomDebuffAfterAttack()
        {
            var casterObject = new UnityEngine.GameObject("Status effect caster");
            var targetObject = new UnityEngine.GameObject("Status effect target");
            try
            {
                var caster = casterObject.AddComponent<Unit>();
                var target = targetObject.AddComponent<Unit>();
                InitializeUnit(caster);
                InitializeUnit(target);
                caster.SetStatusEffectChance(100f);
                typeof(Unit).GetMethod("ApplySkillDamage", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                    .Invoke(caster, new object[] { target, 1f });
                Assert.That(target.HasAnyDebuff, Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(casterObject);
                UnityEngine.Object.DestroyImmediate(targetObject);
            }
        }

        [Test]
        public void UseSkillTwoTimesEnablesActiveSkillRepeatState()
        {
            var unitObject = new UnityEngine.GameObject("Double skill unit");
            try
            {
                var unit = unitObject.AddComponent<Unit>();
                InitializeUnit(unit);
                Assert.That(unit.SetUseSkillTwice(true), Is.True);
                Assert.That(typeof(Unit).GetField("_useSkillTwice", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                    .GetValue(unit), Is.EqualTo(true));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(unitObject);
            }
        }

        [Test]
        public void NoSkillStatBuffDisablesActiveSkillsAndAppliesStatsOnce()
        {
            var unitObject = new UnityEngine.GameObject("No skill stat buff unit");
            try
            {
                var unit = unitObject.AddComponent<Unit>();
                InitializeUnit(unit);
                Assert.That(unit.SetNoSkillStatBuff(100f), Is.True);
                Assert.That(unit.AreActiveSkillsDisabled, Is.True);
                Assert.That(unit.MaxHp, Is.EqualTo(200f).Within(0.001f));
                Assert.That(unit.SetNoSkillStatBuff(100f), Is.True);
                Assert.That(unit.MaxHp, Is.EqualTo(200f).Within(0.001f));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(unitObject);
            }
        }

        [Test]
        public void DefenseBasedDamageReductionScalesWithTargetDefense()
        {
            var unitObject = new UnityEngine.GameObject("Defense reduction target");
            try
            {
                var unit = unitObject.AddComponent<Unit>();
                InitializeUnit(unit);
                unit.Configure(new UnitData { healthPoint = 100f, defensePoint = 20f });
                Assert.That(unit.SetDefenseBasedDamageReduction(10f, 5f), Is.True);
                unit.TakeDamage(50f);
                Assert.That(unit.CurrentHp, Is.EqualTo(55f).Within(0.001f));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(unitObject);
            }
        }

        [Test]
        public void ShieldBonusDamageUsesDefenseWhileShielded()
        {
            var casterObject = new UnityEngine.GameObject("Shield bonus caster");
            var targetObject = new UnityEngine.GameObject("Shield bonus target");
            try
            {
                var caster = casterObject.AddComponent<Unit>();
                var target = targetObject.AddComponent<Unit>();
                InitializeUnit(caster);
                InitializeUnit(target);
                caster.Configure(new UnitData { healthPoint = 100f, defensePoint = 20f });
                caster.GrantShield(10f, 0f, true);
                Assert.That(caster.SetShieldBonusDamage(0.4f, 0.2f), Is.True);
                typeof(Unit).GetMethod("ApplySkillDamage", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                    .Invoke(caster, new object[] { target, 50f });
                Assert.That(target.CurrentHp, Is.EqualTo(46f).Within(0.001f));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(casterObject);
                UnityEngine.Object.DestroyImmediate(targetObject);
            }
        }

        [Test]
        public void ShieldBonusDamageReducesDamageReceivedWhileShielded()
        {
            var unitObject = new UnityEngine.GameObject("Shield bonus receiver");
            try
            {
                var unit = unitObject.AddComponent<Unit>();
                InitializeUnit(unit);
                unit.Configure(new UnitData { healthPoint = 100f, defensePoint = 20f });
                unit.GrantShield(100f, 0f, true);
                Assert.That(unit.SetShieldBonusDamage(0.4f, 0.2f), Is.True);
                unit.TakeDamage(50f);
                Assert.That(unit.Shield, Is.EqualTo(52f).Within(0.001f));
                Assert.That(unit.CurrentHp, Is.EqualTo(100f).Within(0.001f));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(unitObject);
            }
        }

        private static void InitializeUnit(Unit unit)
        {
            typeof(Unit).GetMethod("EnsureRuntimeComponents", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .Invoke(unit, null);
            typeof(Unit).GetMethod("InitializeRuntimeState", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .Invoke(unit, null);
        }

        private static Unit CreateUnit(string name, float maxHp)
        {
            var unit = new UnityEngine.GameObject(name).AddComponent<Unit>();
            unit.Configure(new UnitData { healthPoint = maxHp });
            InitializeUnit(unit);
            return unit;
        }

        private static CombatEffectCatalog BuildThresholdCatalog(EffectTarget target)
        {
            var catalog = new CombatEffectCatalog();
            catalog.Rebuild(null, null, null, new[]
            {
                new RuntimeEffectManager.EffectSource(
                    RuntimeEffectManager.EffectSourceKind.UnitPassive,
                    100,
                    new EffectInstance
                    {
                        trigger = TriggerType.OnHpBelowThreshold,
                        triggerParam = 50f,
                        target = target,
                        effect = EffectType.GrantShield,
                        effectParam = 10f,
                        untilBattleEnd = true
                    },
                    0)
            });
            return catalog;
        }
    }
}
