using DG.DOTweenEditor;
using DG.Tweening;
using OzGameLab01.Controllers;
using UnityEditor;
using UnityEngine;

namespace OzGameLab01.Editor
{
    internal static class BoardUIControllerClockPreviewEditor
    {
        private const string ContextRoot = "CONTEXT/BoardUIController/Clock Animation/";

        [MenuItem(ContextRoot + "Play Day Transition")]
        private static void PlayDayTransition(MenuCommand command)
        {
            Play(command, "dayClockAngle");
        }

        [MenuItem(ContextRoot + "Play Night Transition")]
        private static void PlayNightTransition(MenuCommand command)
        {
            Play(command, "nightClockAngle");
        }

        [MenuItem(ContextRoot + "Stop Preview")]
        private static void StopPreview(MenuCommand command)
        {
            DOTweenEditorPreview.Stop(resetTweenTargets: false, clearTweens: true);

            if (command.context is BoardUIController controller)
            {
                controller.StopClockTransitionPreview();
            }

            RepaintEditor();
        }

        private static void Play(MenuCommand command, string anglePropertyName)
        {
            if (command.context is not BoardUIController controller)
            {
                return;
            }

            SerializedProperty angleProperty =
                new SerializedObject(controller).FindProperty(anglePropertyName);

            if (angleProperty == null)
            {
                Debug.LogError("시계 애니메이션 미리보기에 필요한 설정을 찾을 수 없습니다.", controller);
                return;
            }

            if (!Application.isPlaying)
            {
                DOTweenEditorPreview.Stop(resetTweenTargets: false, clearTweens: true);
            }

            Sequence sequence = controller.PlayClockTransitionPreview(angleProperty.floatValue);

            if (sequence == null || Application.isPlaying)
            {
                return;
            }

            DOTweenEditorPreview.PrepareTweenForPreview(
                sequence,
                clearCallbacks: false,
                preventAutoKill: false,
                andPlay: true);
            DOTweenEditorPreview.Start(RepaintEditor);
        }

        private static void RepaintEditor()
        {
            EditorApplication.QueuePlayerLoopUpdate();
            SceneView.RepaintAll();
        }
    }
}
