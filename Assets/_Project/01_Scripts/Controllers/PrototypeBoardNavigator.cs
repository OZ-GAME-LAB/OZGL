using OzGameLab01.Managers;
using UnityEngine;

namespace OzGameLab01.Controllers
{
    /// <summary>
    /// ProtoBoardScene의 임시 씬 이동을 담당합니다.
    ///
    /// 실제 보드 UI가 완성되기 전까지
    /// 타이틀 씬과 전투 씬 이동을 테스트하기 위해 사용합니다.
    ///
    /// Editor 또는 Development Build에서만 동작합니다.
    /// </summary>
    public sealed class PrototypeBoardNavigator : MonoBehaviour
    {
        [Header("임시 이동 UI")]

        [Tooltip("화면에 표시할 임시 이동 버튼의 너비입니다.")]
        [SerializeField] private float _buttonWidth = 160f;

        [Tooltip("화면에 표시할 임시 이동 버튼의 높이입니다.")]
        [SerializeField] private float _buttonHeight = 45f;

        [Tooltip("버튼 사이의 간격입니다.")]
        [SerializeField] private float _buttonSpacing = 10f;

        private void Update()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (!CanRequestTransition())
            {
                return;
            }

            // 왼쪽 방향키: 타이틀 씬으로 이동
            if (Input.GetKeyDown(KeyCode.LeftArrow))
            {
                LoadPreviousScene();
                return;
            }

            // 오른쪽 방향키: 전투 씬으로 이동
            if (Input.GetKeyDown(KeyCode.RightArrow))
            {
                LoadNextScene();
            }
#endif
        }

        private void OnGUI()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            float totalWidth =
                (_buttonWidth * 2f) + _buttonSpacing;

            float startX =
                (Screen.width - totalWidth) * 0.5f;

            float buttonY =
                Screen.height - _buttonHeight - 20f;

            Rect previousButtonRect = new(
                startX,
                buttonY,
                _buttonWidth,
                _buttonHeight);

            Rect nextButtonRect = new(
                startX + _buttonWidth + _buttonSpacing,
                buttonY,
                _buttonWidth,
                _buttonHeight);

            bool previousEnabled = GUI.enabled;
            GUI.enabled = CanRequestTransition();

            if (GUI.Button(previousButtonRect, "이전: 타이틀  ←"))
            {
                LoadPreviousScene();
            }

            if (GUI.Button(nextButtonRect, "다음: 전투  →"))
            {
                LoadNextScene();
            }

            GUI.enabled = previousEnabled;
#endif
        }

        /// <summary>
        /// 이전 단계인 타이틀 씬으로 이동합니다.
        /// </summary>
        private void LoadPreviousScene()
        {
            if (!TryGetSceneTransitioner(out SceneTransitioner transitioner))
            {
                return;
            }

            Debug.Log(
                "[PrototypeBoardNavigator] 타이틀 씬 이동을 요청합니다.",
                this);

            transitioner.LoadTitleScene();
        }

        /// <summary>
        /// 다음 단계인 전투 씬으로 이동합니다.
        /// </summary>
        private void LoadNextScene()
        {
            if (!TryGetSceneTransitioner(out SceneTransitioner transitioner))
            {
                return;
            }

            Debug.Log(
                "[PrototypeBoardNavigator] 전투 씬 이동을 요청합니다.",
                this);

            transitioner.LoadCombatScene();
        }

        /// <summary>
        /// 현재 씬 전환 요청을 받을 수 있는지 확인합니다.
        /// </summary>
        private bool CanRequestTransition()
        {
            return SceneTransitioner.Instance != null &&
                   !SceneTransitioner.Instance.IsTransitioning;
        }

        /// <summary>
        /// 유지 중인 SceneTransitioner를 가져옵니다.
        /// </summary>
        private bool TryGetSceneTransitioner(
            out SceneTransitioner transitioner)
        {
            transitioner = SceneTransitioner.Instance;

            if (transitioner != null)
            {
                return true;
            }

            Debug.LogError(
                "[PrototypeBoardNavigator] SceneTransitioner가 없어 " +
                "씬 이동 요청을 처리할 수 없습니다. " +
                "00_Boot 씬부터 실행했는지 확인해주세요.",
                this);

            return false;
        }
    }
}