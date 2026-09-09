using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using OzGameLab01.Combat;

namespace OzGameLab01.Tests.EditMode
{
    public class SynergyPanelUtilityTests
    {
        private readonly List<Object> created = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (Object obj in created)
            {
                if (obj != null) Object.DestroyImmediate(obj);
            }

            created.Clear();
        }

        private SynergyTrait CreateTrait(string displayName)
        {
            SynergyTrait trait = ScriptableObject.CreateInstance<SynergyTrait>();
            SetPrivateField(trait, "displayName", displayName);
            created.Add(trait);
            return trait;
        }

        private SynergyDefinition CreateDefinition(SynergyTrait trait, params SynergyDefinition.Tier[] tiers)
        {
            SynergyDefinition definition = ScriptableObject.CreateInstance<SynergyDefinition>();
            SetPrivateField(definition, "trait", trait);
            SetPrivateField(definition, "tiers", new List<SynergyDefinition.Tier>(tiers));
            created.Add(definition);
            return definition;
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            field.SetValue(target, value);
        }

        [Test]
        public void CountTraits_AggregatesAcrossUnits()
        {
            SynergyTrait red = CreateTrait("Red");
            var traitsById = new Dictionary<int, List<SynergyTrait>>
            {
                { 1, new List<SynergyTrait> { red } },
                { 2, new List<SynergyTrait> { red } },
                { 3, new List<SynergyTrait>() },
            };
            Dictionary<SynergyTrait, int> counts = SynergyPanelUtility.CountTraits(new[] { 1, 2, 3 }, traitsById);
            Assert.AreEqual(2, counts[red]);
        }

        [Test]
        public void CountTraits_SkipsIdsMissingFromTable()
        {
            SynergyTrait red = CreateTrait("Red");
            var traitsById = new Dictionary<int, List<SynergyTrait>> { { 1, new List<SynergyTrait> { red } } };
            Dictionary<SynergyTrait, int> counts = SynergyPanelUtility.CountTraits(new[] { 1, 99 }, traitsById);
            Assert.AreEqual(1, counts.Count);
            Assert.AreEqual(1, counts[red]);
        }

        [Test]
        public void BuildDisplayItems_FiltersZeroCountAndSortsDescending()
        {
            SynergyTrait red = CreateTrait("Red");
            SynergyTrait blue = CreateTrait("Blue");
            SynergyTrait unused = CreateTrait("Unused");
            SynergyDefinition.Tier tier = new SynergyDefinition.Tier { requiredCount = 2, hpMultiplier = 1.1f, attackMultiplier = 1.1f };
            SynergyDefinition redDef = CreateDefinition(red, tier);
            SynergyDefinition blueDef = CreateDefinition(blue, tier);
            SynergyDefinition unusedDef = CreateDefinition(unused, tier);
            var counts = new Dictionary<SynergyTrait, int> { { red, 3 }, { blue, 1 }, { unused, 0 } };
            List<SynergyPanelUtility.DisplayItem> items = SynergyPanelUtility.BuildDisplayItems(new List<SynergyDefinition> { blueDef, redDef, unusedDef }, counts);
            Assert.AreEqual(2, items.Count);
            Assert.AreEqual(redDef, items[0].Definition);
            Assert.AreEqual(blueDef, items[1].Definition);
        }

        [Test]
        public void BuildDisplayItems_ComputesActiveTierAndStackText()
        {
            SynergyTrait red = CreateTrait("Red");
            SynergyDefinition definition = CreateDefinition(red,
                new SynergyDefinition.Tier { requiredCount = 2, hpMultiplier = 1.1f, attackMultiplier = 1.1f },
                new SynergyDefinition.Tier { requiredCount = 4, hpMultiplier = 1.3f, attackMultiplier = 1.3f });
            var counts = new Dictionary<SynergyTrait, int> { { red, 2 } };
            List<SynergyPanelUtility.DisplayItem> items = SynergyPanelUtility.BuildDisplayItems(new List<SynergyDefinition> { definition }, counts);
            Assert.AreEqual(1, items.Count);
            Assert.IsTrue(items[0].IsActive);
            Assert.AreEqual("2/4", items[0].StackText);
        }

        [Test]
        public void BuildDisplayItems_BelowFirstTier_IsInactiveWithThresholdStackText()
        {
            SynergyTrait red = CreateTrait("Red");
            SynergyDefinition definition = CreateDefinition(red, new SynergyDefinition.Tier { requiredCount = 3, hpMultiplier = 1.1f, attackMultiplier = 1.1f });
            var counts = new Dictionary<SynergyTrait, int> { { red, 1 } };
            List<SynergyPanelUtility.DisplayItem> items = SynergyPanelUtility.BuildDisplayItems(new List<SynergyDefinition> { definition }, counts);
            Assert.AreEqual(1, items.Count);
            Assert.IsFalse(items[0].IsActive);
            Assert.AreEqual("1/3", items[0].StackText);
        }
    }
}
