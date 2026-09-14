using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using OzGameLab01.Combat;

namespace OzGameLab01.Tests.EditMode
{
    public class PassiveEventBusTests
    {
        private readonly List<GameObject> _spawned = new List<GameObject>();

        [SetUp]
        public void SetUp()
        {
            PassiveEventBus.ResetRunState();
        }

        [TearDown]
        public void TearDown()
        {
            PassiveEventBus.ResetRunState();

            foreach (GameObject go in _spawned)
            {
                if (go != null)
                {
                    Object.DestroyImmediate(go);
                }
            }

            _spawned.Clear();
        }

        private Unit CreateUnit(Unit.Team team)
        {
            GameObject go = new GameObject("TestUnit");
            _spawned.Add(go);
            Unit unit = go.AddComponent<Unit>();

            FieldInfo teamField = typeof(Unit).GetField("team", BindingFlags.NonPublic | BindingFlags.Instance);
            teamField.SetValue(unit, team);

            return unit;
        }

        [Test]
        public void RaiseBattleStart_InvokesSubscribers()
        {
            int callCount = 0;
            PassiveEventBus.OnBattleStart += () => callCount++;

            PassiveEventBus.RaiseBattleStart();

            Assert.AreEqual(1, callCount);
        }

        [Test]
        public void RaiseDeath_AllyUnit_FiresBothSelfAndAllyDeath()
        {
            Unit ally = CreateUnit(Unit.Team.Ally);
            bool selfDeathFired = false;
            bool allyDeathFired = false;
            PassiveEventBus.OnSelfDeath += u => selfDeathFired = u == ally;
            PassiveEventBus.OnAllyDeath += u => allyDeathFired = u == ally;

            PassiveEventBus.RaiseDeath(ally);

            Assert.IsTrue(selfDeathFired);
            Assert.IsTrue(allyDeathFired);
        }

        [Test]
        public void RaiseDeath_EnemyUnit_FiresOnlySelfDeath()
        {
            Unit enemy = CreateUnit(Unit.Team.Enemy);
            bool selfDeathFired = false;
            bool allyDeathFired = false;
            PassiveEventBus.OnSelfDeath += u => selfDeathFired = true;
            PassiveEventBus.OnAllyDeath += u => allyDeathFired = true;

            PassiveEventBus.RaiseDeath(enemy);

            Assert.IsTrue(selfDeathFired);
            Assert.IsFalse(allyDeathFired);
        }

        [Test]
        public void RaiseDeath_Null_DoesNotThrowOrFire()
        {
            bool fired = false;
            PassiveEventBus.OnSelfDeath += u => fired = true;

            Assert.DoesNotThrow(() => PassiveEventBus.RaiseDeath(null));
            Assert.IsFalse(fired);
        }

        [Test]
        public void RaiseSkillUsed_AllyCaster_FiresOnAllySkillUsedOnly()
        {
            Unit ally = CreateUnit(Unit.Team.Ally);
            SkillData skill = new SkillData { id = 1 };
            bool allyFired = false;
            bool enemyFired = false;
            PassiveEventBus.OnAllySkillUsed += (u, s) => allyFired = u == ally && s == skill;
            PassiveEventBus.OnEnemySkillUsed += (u, s) => enemyFired = true;

            PassiveEventBus.RaiseSkillUsed(ally, skill);

            Assert.IsTrue(allyFired);
            Assert.IsFalse(enemyFired);
        }

        [Test]
        public void RaiseSkillUsed_EnemyCaster_FiresOnEnemySkillUsedOnly()
        {
            Unit enemy = CreateUnit(Unit.Team.Enemy);
            SkillData skill = new SkillData { id = 2 };
            bool allyFired = false;
            bool enemyFired = false;
            PassiveEventBus.OnAllySkillUsed += (u, s) => allyFired = true;
            PassiveEventBus.OnEnemySkillUsed += (u, s) => enemyFired = u == enemy && s == skill;

            PassiveEventBus.RaiseSkillUsed(enemy, skill);

            Assert.IsFalse(allyFired);
            Assert.IsTrue(enemyFired);
        }

        [Test]
        public void RaiseAttackLanded_InvokesSubscribersWithAttackerAndTarget()
        {
            Unit attacker = CreateUnit(Unit.Team.Ally);
            Unit target = CreateUnit(Unit.Team.Enemy);
            Unit capturedAttacker = null;
            Unit capturedTarget = null;
            PassiveEventBus.OnAttackLanded += (a, t) =>
            {
                capturedAttacker = a;
                capturedTarget = t;
            };

            PassiveEventBus.RaiseAttackLanded(attacker, target);

            Assert.AreEqual(attacker, capturedAttacker);
            Assert.AreEqual(target, capturedTarget);
        }

        [Test]
        public void ResetRunState_ClearsAllSubscribers()
        {
            int callCount = 0;
            PassiveEventBus.OnBattleStart += () => callCount++;

            PassiveEventBus.ResetRunState();
            PassiveEventBus.RaiseBattleStart();

            Assert.AreEqual(0, callCount);
        }
    }
}
