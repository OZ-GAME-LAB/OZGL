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

        private SynergyDefinition CreateDefinition(string displayName, params SynergyDefinition.Tier[] tiers)
        {
            SynergyDefinition definition = ScriptableObject.CreateInstance<SynergyDefinition>();
            SetPrivateField(definition, "displayName", displayName);
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
            SynergyDefinition red = CreateDefinition("Red");
            var traitsById = new Dictionary<int, List<SynergyDefinition>>
            {
                { 1, new List<SynergyDefinition> { red } },
                { 2, new List<SynergyDefinition> { red } },
                { 3, new List<SynergyDefinition>() },
            };
            Dictionary<SynergyDefinition, int> counts = SynergyPanelUtility.CountTraits(new[] { 1, 2, 3 }, traitsById);
            Assert.AreEqual(2, counts[red]);
        }

        [Test]
        public void CountTraits_SkipsIdsMissingFromTable()
        {
            SynergyDefinition red = CreateDefinition("Red");
            var traitsById = new Dictionary<int, List<SynergyDefinition>> { { 1, new List<SynergyDefinition> { red } } };
            Dictionary<SynergyDefinition, int> counts = SynergyPanelUtility.CountTraits(new[] { 1, 99 }, traitsById);
            Assert.AreEqual(1, counts.Count);
            Assert.AreEqual(1, counts[red]);
        }

        [Test]
        public void BuildDisplayItems_FiltersZeroCountAndSortsDescending()
        {
            SynergyDefinition.Tier tier = new SynergyDefinition.Tier { requiredCount = 2, hpMultiplier = 1.1f, attackMultiplier = 1.1f };
            SynergyDefinition redDef = CreateDefinition("Red", tier);
            SynergyDefinition blueDef = CreateDefinition("Blue", tier);
            SynergyDefinition unusedDef = CreateDefinition("Unused", tier);
            var counts = new Dictionary<SynergyDefinition, int> { { redDef, 3 }, { blueDef, 1 }, { unusedDef, 0 } };
            List<SynergyPanelUtility.DisplayItem> items = SynergyPanelUtility.BuildDisplayItems(new List<SynergyDefinition> { blueDef, redDef, unusedDef }, counts);
            Assert.AreEqual(2, items.Count);
            Assert.AreEqual(redDef, items[0].Definition);
            Assert.AreEqual(blueDef, items[1].Definition);
        }

        [Test]
        public void BuildDisplayItems_ComputesActiveTierAndStackText()
        {
            SynergyDefinition definition = CreateDefinition("Red",
                new SynergyDefinition.Tier { requiredCount = 2, hpMultiplier = 1.1f, attackMultiplier = 1.1f },
                new SynergyDefinition.Tier { requiredCount = 4, hpMultiplier = 1.3f, attackMultiplier = 1.3f });
            var counts = new Dictionary<SynergyDefinition, int> { { definition, 2 } };
            List<SynergyPanelUtility.DisplayItem> items = SynergyPanelUtility.BuildDisplayItems(new List<SynergyDefinition> { definition }, counts);
            Assert.AreEqual(1, items.Count);
            Assert.IsTrue(items[0].IsActive);
            Assert.AreEqual("2/4", items[0].StackText);
        }

        [Test]
        public void BuildDisplayItems_BelowFirstTier_IsInactiveWithThresholdStackText()
        {
            SynergyDefinition definition = CreateDefinition("Red", new SynergyDefinition.Tier { requiredCount = 3, hpMultiplier = 1.1f, attackMultiplier = 1.1f });
            var counts = new Dictionary<SynergyDefinition, int> { { definition, 1 } };
            List<SynergyPanelUtility.DisplayItem> items = SynergyPanelUtility.BuildDisplayItems(new List<SynergyDefinition> { definition }, counts);
            Assert.AreEqual(1, items.Count);
            Assert.IsFalse(items[0].IsActive);
            Assert.AreEqual("1/3", items[0].StackText);
        }
    }
}
