using System.Reflection;
using NUnit.Framework;
using OzGameLab01.Controllers;
using OzGameLab01.UI;
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
    }
}
