using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using OzGameLab01.Combat;

namespace OzGameLab01.Tests.EditMode
{
    public class BattleUnitRegistryTests
    {
        private readonly List<GameObject> spawned = new List<GameObject>();

        [SetUp]
        public void SetUp() => BattleUnitRegistry.Clear();

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in spawned)
            {
                if (go != null) Object.DestroyImmediate(go);
            }

            spawned.Clear();
            BattleUnitRegistry.Clear();
        }

        private Unit CreateUnit()
        {
            GameObject go = new GameObject("TestUnit");
            spawned.Add(go);
            return go.AddComponent<Unit>();
        }

        [Test]
        public void Register_AddsUnitOnce()
        {
            Unit unit = CreateUnit();
            BattleUnitRegistry.Register(unit);
            Assert.That(BattleUnitRegistry.Units, Does.Contain(unit));
            Assert.AreEqual(1, BattleUnitRegistry.Units.Count);
        }

        [Test]
        public void Register_SameUnitTwice_DoesNotDuplicate()
        {
            Unit unit = CreateUnit();
            BattleUnitRegistry.Register(unit);
            BattleUnitRegistry.Register(unit);
            Assert.AreEqual(1, BattleUnitRegistry.Units.Count);
        }

        [Test]
        public void Register_Null_DoesNothing()
        {
            BattleUnitRegistry.Register(null);
            Assert.AreEqual(0, BattleUnitRegistry.Units.Count);
        }

        [Test]
        public void Unregister_RemovesUnit()
        {
            Unit unit = CreateUnit();
            BattleUnitRegistry.Unregister(unit);
            Assert.AreEqual(0, BattleUnitRegistry.Units.Count);
        }

        [Test]
        public void Unregister_UnknownUnit_DoesNotThrow()
        {
            Unit unit = CreateUnit();
            BattleUnitRegistry.Clear();
            Assert.DoesNotThrow(() => BattleUnitRegistry.Unregister(unit));
        }

        [Test]
        public void Clear_EmptiesRegistry()
        {
            CreateUnit();
            CreateUnit();
            BattleUnitRegistry.Clear();
            Assert.AreEqual(0, BattleUnitRegistry.Units.Count);
        }

        [Test]
        public void DestroyingUnit_UnregistersIt()
        {
            Unit unit = CreateUnit();
            GameObject go = unit.gameObject;
            Object.DestroyImmediate(go);
            spawned.Remove(go);
            Assert.AreEqual(0, BattleUnitRegistry.Units.Count);
        }
    }
}
