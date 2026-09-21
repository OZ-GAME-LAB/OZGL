using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using OzGameLab01.Combat;
using OzGameLab01.Controllers;
using OzGameLab01.Data;
using OzGameLab01.Rewards;
using OzGameLab01.UI.Battle;

namespace OzGameLab01.Editor
{
    public static class CombatSceneValidator
    {
        private const string ProductionCombatScene =
            "Assets/_Project/03_Scenes/03_Combat/03_Combat.unity";

        [MenuItem("OZGL/Validation/Validate Production Combat Scene")]
        public static void ValidateProductionCombatScene()
        {
            ValidateScene(ProductionCombatScene);
        }

        public static bool ValidateScene(string scenePath)
        {
            if (!File.Exists(scenePath))
            {
                Debug.LogError($"[CombatSceneValidator] 씬 파일을 찾을 수 없습니다: {scenePath}");
                return false;
            }

            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
            int errorCount = 0;

            CombatSceneController sceneController = FindInScene<CombatSceneController>(scene);
            CombatUIController uiController = FindInScene<CombatUIController>(scene);
            CombatSession combatSession = FindInScene<CombatSession>(scene);
            CombatUIView battleUIView = FindInScene<CombatUIView>(scene);

            errorCount += Require(sceneController, "CombatSceneController");
            errorCount += Require(uiController, "CombatUIController");
            errorCount += Require(combatSession, "CombatSession");
            errorCount += Require(battleUIView, "CombatUIView");

            if (uiController != null)
            {
                errorCount += RequireReference(uiController, uiController.battleUIView, "CombatUIController.battleUIView");
                errorCount += RequireReference(uiController, uiController.combatSceneController, "CombatUIController.combatSceneController");
            }

            if (battleUIView != null)
            {
                errorCount += Require(battleUIView.MainView, "CombatUIView.MainView");
                errorCount += Require(battleUIView.RewardView, "CombatUIView.RewardView");
                errorCount += Require(battleUIView.ResultView, "CombatUIView.ResultView");

                if (battleUIView.RewardView != null)
                {
                    if (battleUIView.RewardView.ConfiguredRewards != null)
                    {
                        foreach (BattleRewardData reward in battleUIView.RewardView.ConfiguredRewards)
                        {
                            if (!CombatDataValidator.ValidateReward(reward, battleUIView.RewardView))
                            {
                                errorCount++;
                            }
                        }
                    }
                }

                if (battleUIView.MainView != null)
                {
                    // 현재 BattleUI에서 사용하지 않는 전투 타이머 선택 참조
                    LogOptionalReference(
                        battleUIView.MainView,
                        "timerView",
                        "현재 BattleUI에는 전투 타이머 표시를 사용하지 않습니다.");
                    errorCount += Require(battleUIView.MainView.ControlView, "CombatMainView.ControlView");
                    errorCount += Require(battleUIView.MainView.UnitInfoView, "CombatMainView.UnitInfoView");
                    errorCount += Require(battleUIView.MainView.SynergyView, "CombatMainView.SynergyView");
                }
            }

            if (combatSession != null)
            {
                // 월드 전투 배치에 필요한 BattleMap 프리팹 필수 참조
                errorCount += RequireSerializedReference(combatSession, "battleMapPrefab");
                errorCount += RequireSerializedReference(combatSession, "allyTemplatePrefab");
                LogOptionalReference(combatSession, "battleMainView", "CombatSession이 런타임 자동 탐색합니다.");
                errorCount += RequireSerializedReference(combatSession, "rosterData");
                errorCount += RequireSerializedReference(combatSession, "synergyPanelRoot");
                errorCount += RequireSerializedReference(combatSession, "synergyItemTemplate");
            }

            EditorSceneManager.CloseScene(scene, false);

            if (errorCount == 0)
            {
                Debug.Log($"[CombatSceneValidator] 검증 통과: {scenePath}");
                return true;
            }

            Debug.LogError($"[CombatSceneValidator] 검증 실패: {scenePath} | 오류 {errorCount}개");
            return false;
        }

        private static T FindInScene<T>(Scene scene) where T : Component
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                T component = root.GetComponentInChildren<T>(true);
                if (component != null)
                {
                    return component;
                }
            }

            return null;
        }

        private static int Require(Object value, string label)
        {
            if (value != null)
            {
                return 0;
            }

            LogError($"{label}가 씬에 없습니다.");
            return 1;
        }

        private static int RequireReference(Object owner, Object value, string label)
        {
            if (value != null)
            {
                return 0;
            }

            LogError($"{label} 참조가 비어 있습니다.");
            return 1;
        }

        private static int RequireSerializedReference(Object owner, string propertyPath)
        {
            SerializedObject serialized = new SerializedObject(owner);
            SerializedProperty property = serialized.FindProperty(propertyPath);
            if (property != null && property.propertyType == SerializedPropertyType.ObjectReference &&
                property.objectReferenceValue != null)
            {
                return 0;
            }

            if (owner is Component component &&
                PrefabUtility.IsPartOfPrefabInstance(component))
            {
                Component source = PrefabUtility.GetCorrespondingObjectFromSource(component);
                if (source != null)
                {
                    SerializedObject sourceSerialized = new SerializedObject(source);
                    SerializedProperty sourceProperty = sourceSerialized.FindProperty(propertyPath);
                    if (sourceProperty != null && sourceProperty.propertyType == SerializedPropertyType.ObjectReference &&
                        sourceProperty.objectReferenceValue != null)
                    {
                        return 0;
                    }
                }
            }

            LogError($"{owner.GetType().Name}.{propertyPath} 참조가 비어 있습니다.");
            return 1;
        }

        private static void LogOptionalReference(Object owner, string propertyPath, string reason)
        {
            SerializedObject serialized = new SerializedObject(owner);
            SerializedProperty property = serialized.FindProperty(propertyPath);
            if (property != null && property.propertyType == SerializedPropertyType.ObjectReference &&
                property.objectReferenceValue != null)
            {
                return;
            }

            Debug.LogWarning($"[CombatSceneValidator] 선택 참조가 비어 있습니다: {owner.GetType().Name}.{propertyPath} ({reason})");
        }

        private static void LogError(string message)
        {
            Debug.LogError($"[CombatSceneValidator] {message}");
        }
    }
}
