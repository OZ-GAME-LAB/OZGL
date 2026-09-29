using System.Collections;
using System.Reflection;
using NUnit.Framework;
using OzGameLab01.Combat;
using OzGameLab01.Data;
using OzGameLab01.UI.Battle;
using TMPro;
using UnityEditor;
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

        [TestCase(EffectType.GrantShield)]
        [TestCase(EffectType.Heal)]
        [TestCase(EffectType.DealDamage)]
        [TestCase(EffectType.StatModifier)]
        public void MissingTarget_DoesNotReportApplication(EffectType effect)
        {
            var method = typeof(CombatEffectExecutor).GetMethod("ApplyEffect",
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.IsFalse((bool)method.Invoke(null, new object[]
            {
                new EffectInstance { effect = effect, effectParam = 10 }, null
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
                var view = CombatEffectFeedbackView.Create(root.AddComponent<CombatMainView>());
                Assert.AreSame(view, CombatEffectFeedbackView.Create(root.GetComponent<CombatMainView>()));
                var one = first.AddComponent<Unit>();
                var two = second.AddComponent<Unit>();
                view.Show(new CombatFeedback(CombatFeedbackKind.Synergy, "Human", "Attack +20%", one));
                view.Show(new CombatFeedback(CombatFeedbackKind.Synergy, "Human", "Attack +20%", two));
                var entries = (IList)typeof(CombatEffectFeedbackView).GetField("_entries",
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

        [Test]
        public void BattleUi_EnemyNameFontSupportsKoreanSpeciesNames()
        {
            const string battleUiPath = "Assets/_Project/02_Prefabs/UI/BattleUi/BattleUI.prefab";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(battleUiPath);

            Assert.That(prefab, Is.Not.Null);
            TextMeshProUGUI enemyName = System.Array.Find(
                prefab.GetComponentsInChildren<TextMeshProUGUI>(true),
                text => text.name == "EnemyNameText");

            Assert.That(enemyName, Is.Not.Null);
            Assert.That(enemyName.font, Is.Not.Null);
            Assert.That(enemyName.font.name, Does.Contain("NotoSansKR"));
            Assert.That(enemyName.font.atlasPopulationMode, Is.EqualTo(AtlasPopulationMode.Dynamic));
            Assert.That(enemyName.font.sourceFontFile, Is.Not.Null,
                "The dynamic Korean font needs its source TTF to add missing Hangul glyphs at runtime.");
        }

        [Test]
        public void BattleUi_ResultTextsSupportLocalizedCopy()
        {
            const string battleUiPath = "Assets/_Project/02_Prefabs/UI/BattleUi/BattleUI.prefab";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(battleUiPath);
            CombatResultView resultView = prefab != null
                ? prefab.GetComponentInChildren<CombatResultView>(true)
                : null;

            Assert.That(prefab, Is.Not.Null);
            Assert.That(resultView, Is.Not.Null);
            Assert.That(SupportsKorean(resultView.ResultText.font), Is.True);
            Assert.That(SupportsKorean(resultView.OptionalMessageText.font), Is.True);
            Assert.That(SupportsKorean(resultView.EndBattleButton.GetComponentInChildren<TMP_Text>(true).font),
                Is.True);
        }

        // 결과창은 영문 폰트(Oxanium)에 NotoSansKR을 폴백으로 연결해 한글을 표시합니다.
        private static bool SupportsKorean(TMP_FontAsset font)
        {
            if (font == null) return false;
            if (font.name.Contains("NotoSansKR")) return true;
            return font.fallbackFontAssetTable != null &&
                font.fallbackFontAssetTable.Exists(fallback => fallback != null && fallback.name.Contains("NotoSansKR"));
        }

        [Test]
        public void BattleUi_EmptyDpsAreaIsHiddenWhenResultIsShown()
        {
            const string battleUiPath = "Assets/_Project/02_Prefabs/UI/BattleUi/BattleUI.prefab";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(battleUiPath);
            GameObject instance = prefab != null ? Object.Instantiate(prefab) : null;

            try
            {
                CombatResultView resultView = instance != null
                    ? instance.GetComponentInChildren<CombatResultView>(true)
                    : null;

                Assert.That(resultView, Is.Not.Null);
                resultView.ClearDpsInfoItems();
                resultView.Show();

                Assert.That(resultView.DpsListRoot.gameObject.activeSelf, Is.False);
            }
            finally
            {
                if (instance != null)
                {
                    Object.DestroyImmediate(instance);
                }
            }
        }
    }
}
