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
        // [추가] 비동기 New Game 저장 중 중복 요청 방지
        private bool _isStartingGame;

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
            // [추가] Continue 버튼 요청을 저장 데이터 복원 흐름에 연결
            _titleView.ContinueRequested += HandleContinueRequested;
            _titleView.ExitConfirmed += HandleExitConfirmed;

            // [추가] 유효한 런 저장 파일이 있을 때만 Continue 버튼 활성화
            _titleView.SetContinueInteractable(SaveManager.Instance.HasContinueData);
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
            // [추가] Continue 요청 이벤트 구독 해제
            _titleView.ContinueRequested -= HandleContinueRequested;
            _titleView.ExitConfirmed -= HandleExitConfirmed;
        }

        #endregion

        #region Event Handlers

        // 게임 시작 요청을 받아 보드 씬으로 이동합니다.
        //private void HandleStartRequested()
        /// <summary>
        /// [수정] New Game 초기화와 저장이 끝난 뒤 메인보드로 이동
        /// </summary>
        private async void HandleStartRequested()
        {
            if (_isStartingGame)
            {
                return;
            }

            if (!TryGetSceneTransitioner(
                    out SceneTransitioner transitioner))
            {
                return;
            }

            // [추가] New Game 초기화와 저장이 끝날 때까지 중복 입력 방지
            _isStartingGame = true;

            Debug.Log(
                "[TitleSceneController] 게임 시작 요청 | 보드 씬 이동",
                this);

            //transitioner.LoadBoardScene();
            // [수정] 이전 런을 초기화하고 새 Map Seed와 빈 편성을 저장한 뒤 이동
            SaveManager saveManager = SaveManager.Instance;
            saveManager.BeginNewRun();
            bool saved = await saveManager.SaveAsync();
            if (!saved)
            {
                Debug.LogError("[TitleSceneController] New Game 초기 상태 저장에 실패했습니다.", this);
            }

            transitioner.LoadBoardScene();
            _isStartingGame = false;
        }

        /// <summary>
        /// Continue 요청 시 저장된 런 상태를 복원하고 메인보드로 이동합니다.
        /// </summary>
        private void HandleContinueRequested()
        {
            if (_isStartingGame)
            {
                return;
            }

            if (!TryGetSceneTransitioner(out SceneTransitioner transitioner))
            {
                return;
            }

            _isStartingGame = true;

            SaveManager saveManager = SaveManager.Instance;
            if (!saveManager.RestoreCurrentRun())
            {
                Debug.LogWarning("[TitleSceneController] 복원 가능한 Continue 데이터가 없습니다.", this);
                _titleView.SetContinueInteractable(false);
                // 복원 실패 후 New Game을 다시 선택할 수 있도록 입력 잠금 해제
                _isStartingGame = false;
                return;
            }

            Debug.Log("[TitleSceneController] Continue 데이터 복원 완료 | 보드 씬 이동", this);
            transitioner.LoadBoardScene();
            _isStartingGame = false;
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
                "[TitleSceneController] SceneTransitioner가 없어 게임 시작 요청을 처리할 수 없습니다.", this);

            return false;
        }

        #endregion
    }
}
