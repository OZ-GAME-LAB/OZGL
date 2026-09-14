using System.Collections;
using System.Reflection;
using NUnit.Framework;
using OzGameLab01.Combat;
using OzGameLab01.UI.Battle;
using UnityEngine;
using UnityEngine.UI;

namespace OzGameLab01.Tests.EditMode
{
    public class CombatFeedbackTests
    {
        [TestCase(EffectStatType.Unknown)]
        [TestCase(EffectStatType.Lifesteal)]
        [TestCase(EffectStatType.CurrentDiceValue)]
        public void UnsupportedStat_DoesNotReportApplication(EffectStatType stat)
        {
            var go = new GameObject("Feedback test unit");
            try
            {
                var unit = go.AddComponent<Unit>();
                float hp = unit.MaxHp;
                Assert.IsFalse(unit.ApplyStatEffect(stat, 20));
                Assert.AreEqual(hp, unit.MaxHp);
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void UnsupportedEffect_DoesNotReportApplication()
        {
            var method = typeof(CombatEffectExecutor).GetMethod("ApplyEffect",
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.IsFalse((bool)method.Invoke(null, new object[]
            {
                new EffectInstance { effect = EffectType.GrantShield, effectParam = 10 }, null
            }));
        }

        [Test]
        public void Feedback_GroupsSameFrameTargets_AndBoundsHistoryWithoutBlockingInput()
        {
            var root = new GameObject("Feedback test", typeof(RectTransform));
            var first = new GameObject("First");
            var second = new GameObject("Second");
            try
            {
                var view = BattleEffectFeedbackView.Create(root.AddComponent<BattleMainView>());
                Assert.AreSame(view, BattleEffectFeedbackView.Create(root.GetComponent<BattleMainView>()));
                var one = first.AddComponent<Unit>();
                var two = second.AddComponent<Unit>();
                view.Show(new CombatFeedback(CombatFeedbackKind.Synergy, "Human", "Attack +20%", one));
                view.Show(new CombatFeedback(CombatFeedbackKind.Synergy, "Human", "Attack +20%", two));
                var entries = (IList)typeof(BattleEffectFeedbackView).GetField("_entries",
                    BindingFlags.Instance | BindingFlags.NonPublic).GetValue(view);
                Assert.AreEqual(1, entries.Count);
                Assert.IsTrue(System.Array.Exists(root.GetComponentsInChildren<Text>(true),
                    t => t.text.Contains("2명")));
                for (int i = 0; i < 20; i++)
                    view.Show(new CombatFeedback(CombatFeedbackKind.Skill, "Skill " + i, "Cast", one));
                Assert.AreEqual(5, entries.Count);
                foreach (Graphic graphic in root.GetComponentsInChildren<Graphic>(true))
                    Assert.IsFalse(graphic.raycastTarget);
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(first);
                Object.DestroyImmediate(second);
            }
        }
    }
}
