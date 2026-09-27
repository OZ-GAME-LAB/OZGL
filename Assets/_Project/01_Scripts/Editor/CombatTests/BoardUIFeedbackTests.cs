using System.Reflection;
using NUnit.Framework;
using OzGameLab01.Controllers;
using OzGameLab01.Board.Views;
using OzGameLab01.UI;
using TMPro;
using UnityEngine;
using UnityEditor;

namespace OzGameLab01.Tests.EditMode
{
    public sealed class BoardUIFeedbackTests
    {
        [Test]
        public void AutomaticRollViewWaitsWhileUnitAcquirePopupIsVisible()
        {
            var controllerObject = new GameObject("Board UI controller test");
            var readyViewObject = new GameObject("Ready scene view test");
            var popupObject = new GameObject("Unit acquire popup test", typeof(RectTransform), typeof(CanvasGroup));

            try
            {
                BoardUIController controller = controllerObject.AddComponent<BoardUIController>();
                ReadySceneView readyView = readyViewObject.AddComponent<ReadySceneView>();
                UnitAcquirePopupView popup = popupObject.AddComponent<UnitAcquirePopupView>();
                CanvasGroup canvasGroup = popupObject.GetComponent<CanvasGroup>();
                canvasGroup.alpha = 1f;
                typeof(UnitAcquirePopupView)
                    .GetField("canvasGroup", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.SetValue(popup, canvasGroup);

                FieldInfo popupField = typeof(ReadySceneView)
                    .GetField("_unitAcquirePopupView", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(popupField, Is.Not.Null);
                popupField.SetValue(readyView, popup);
                controller.readySceneView = readyView;

                Assert.That(readyView.UnitAcquirePopupView, Is.SameAs(popup));
                Assert.That(popup.IsVisible, Is.True);

                object result = typeof(BoardUIController)
                    .GetMethod("TryOpenRollViewInternal", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.Invoke(controller, null);

                Assert.That(result, Is.Not.Null);
                Assert.That(result.ToString(), Is.EqualTo("Retry"));
            }
            finally
            {
                Object.DestroyImmediate(popupObject);
                Object.DestroyImmediate(readyViewObject);
                Object.DestroyImmediate(controllerObject);
            }
        }

        [Test]
        public void ReadyUi_SidePanelsHaveRuntimeItemPrefabs()
        {
            const string readyUiPath = "Assets/_Project/02_Prefabs/UI/ReadyUi/Refactor/ReadyUI.prefab";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(readyUiPath);
            ReadyMainView mainView = prefab != null
                ? prefab.GetComponentInChildren<ReadyMainView>(true)
                : null;

            Assert.That(prefab, Is.Not.Null);
            Assert.That(mainView, Is.Not.Null);
            Assert.That(mainView.ArtifactContentRoot, Is.Not.Null);
            Assert.That(mainView.ArtifactItemPrefab, Is.Not.Null);
            Assert.That(mainView.SynergyContentRoot, Is.Not.Null);
            Assert.That(mainView.SynergyItemPrefab, Is.Not.Null);
        }

        [Test]
        public void HudViewsUseLocalizedLabels()
        {
            var timeObject = new GameObject("Time HUD test");
            var actionObject = new GameObject("Action HUD test");

            try
            {
                TimeStatusHUDView timeView = timeObject.AddComponent<TimeStatusHUDView>();
                ActionPowerHUDView actionView = actionObject.AddComponent<ActionPowerHUDView>();
                TMP_Text timeText = new GameObject("Time text").AddComponent<TextMeshPro>();
                TMP_Text actionText = new GameObject("Action text").AddComponent<TextMeshPro>();
                timeText.transform.SetParent(timeObject.transform, false);
                actionText.transform.SetParent(actionObject.transform, false);
                typeof(TimeStatusHUDView)
                    .GetField("turnsUntilNightText", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.SetValue(timeView, timeText);
                typeof(ActionPowerHUDView)
                    .GetField("actionPowerText", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.SetValue(actionView, actionText);

                timeView.SetTurnsUntilNight(8);
                actionView.SetActionPower(3);

                Assert.That(timeText.text, Is.EqualTo("다음 시간대까지 8턴"));
                Assert.That(actionText.text, Is.EqualTo("행동력 3"));
            }
            finally
            {
                Object.DestroyImmediate(actionObject);
                Object.DestroyImmediate(timeObject);
            }
        }

        [Test]
        public void BoardFeedbackDoesNotCreateDebugHudWhenUnconfigured()
        {
            var feedback = new BoardSceneFeedbackView(null, null);

            feedback.ShowTurns(8);

            Assert.That(Object.FindFirstObjectByType<TimeStatusHUDView>(), Is.Null);
        }

        [Test]
        public void EventUi_LocalizedLabelsUseKoreanFont()
        {
            const string eventUiPath = "Assets/_Project/02_Prefabs/UI/EventUi/EventUi.prefab";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(eventUiPath);
            TextMeshProUGUI[] labels = prefab != null
                ? prefab.GetComponentsInChildren<TextMeshProUGUI>(true)
                : null;

            Assert.That(prefab, Is.Not.Null);
            Assert.That(labels, Is.Not.Null.And.Length.EqualTo(3));
            Assert.That(labels, Has.All.Matches<TextMeshProUGUI>(label =>
                label.font != null && label.font.name.Contains("NotoSansKR")));
        }

        [TestCase("Assets/_Project/02_Prefabs/UI/BattleUi/BattleUI.prefab")]
        [TestCase("Assets/_Project/02_Prefabs/UI/ReadyUi/Runtime/Synergy_Item.prefab")]
        public void SynergyItemPrefabsUseReadableLocalizedText(string prefabPath)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            SynergyItemView item = prefab != null
                ? prefab.GetComponentInChildren<SynergyItemView>(true)
                : null;

            Assert.That(prefab, Is.Not.Null);
            Assert.That(item, Is.Not.Null);
            Assert.That(item.TitleText.font.name, Does.Contain("NotoSansKR"));
            Assert.That(item.StackText.font.name, Does.Contain("NotoSansKR"));
            Assert.That(item.TitleText.color.r, Is.LessThan(0.2f));
            Assert.That(item.TitleText.color.g, Is.LessThan(0.2f));
            Assert.That(item.TitleText.color.b, Is.LessThan(0.2f));
            Assert.That(item.StackText.color, Is.EqualTo(item.TitleText.color));
        }
    }
}
