using OzGameLab01.Managers;
using OzGameLab01.UI.Title;
using UnityEngine;

namespace OzGameLab01.Controllers
{
    /// <summary>
    /// 타이틀 UI에서 발생한 요청을 받아
    /// 게임 시작과 종료 흐름을 처리합니다.
    ///
    /// 화면 표시와 버튼 입력 감지는 TitleUIView가 담당하고,
    /// 실제 씬 전환은 SceneTransitioner가 담당합니다.
    /// </summary>
    public sealed class TitleSceneController : MonoBehaviour
    {
        [Header("타이틀 UI")]
        [Tooltip("타이틀 화면과 버튼 이벤트를 제공하는 View입니다.")]
        [SerializeField] private TitleUIView _titleView;

        #region Unity Lifecycle

        private void OnEnable()
        {
            // TitleUIView 참조 검사
            if (_titleView == null)
            {
                Debug.LogError(
                    "[TitleSceneController] TitleUIView가 등록되지 않았습니다.",
                    this);

                return;
            }

            // 타이틀 UI 요청 이벤트 구독
            _titleView.StartRequested += HandleStartRequested;
            _titleView.ExitConfirmed += HandleExitConfirmed;
        }

        private void OnDisable()
        {
            // TitleUIView가 없으면 해제 작업 생략
            if (_titleView == null)
            {
                return;
            }

            // 타이틀 UI 요청 이벤트 구독 해제
            _titleView.StartRequested -= HandleStartRequested;
            _titleView.ExitConfirmed -= HandleExitConfirmed;
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// 게임 시작 요청을 받아 보드 씬으로 이동합니다.
        /// </summary>
        private void HandleStartRequested()
        {
            if (!TryGetSceneTransitioner(
                    out SceneTransitioner transitioner))
            {
                return;
            }

            Debug.Log(
                "[TitleSceneController] 게임 시작 요청 | 보드 씬 이동",
                this);

            transitioner.LoadBoardScene();
        }

        /// <summary>
        /// 종료 확인 요청을 받아 애플리케이션을 종료합니다.
        /// </summary>
        private void HandleExitConfirmed()
        {
            Debug.Log(
                "[TitleSceneController] 게임 종료 요청",
                this);

#if UNITY_EDITOR
            // Unity Editor에서는 Play 모드 종료
            UnityEditor.EditorApplication.isPlaying = false;
#else
            // 실제 빌드에서는 애플리케이션 종료
            Application.Quit();
#endif
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// 현재 유지 중인 SceneTransitioner를 가져옵니다.
        /// 사용할 수 없다면 오류를 출력합니다.
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
                "[TitleSceneController] SceneTransitioner가 없어 " +
                "게임 시작 요청을 처리할 수 없습니다.",
                this);

            return false;
        }

        #endregion
    }
}
