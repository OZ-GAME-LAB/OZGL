using OzGameLab01.Board.Models;
using OzGameLab01.UI;
using UnityEngine;

namespace OzGameLab01.Controllers
{
    /// <summary>
    /// 보드 씬의 카메라, 플레이어, 평면 기준 Transform을
    /// BoardSightEffectView에 연결합니다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BoardSightEffectController : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("안개 효과를 표시할 BoardSightEffectView입니다.")]
        [SerializeField] private BoardSightEffectView boardSightEffectView;

        [Tooltip("_boardEnvironmentRoot 아래의 Main Camera입니다.")]
        [SerializeField] private Camera boardCamera;

        [Tooltip("Locate 연출 시작과 종료에 맞춰 안개의 가시 영역을 이동할 보드 카메라 컨트롤러입니다. 비워두면 자동으로 찾습니다.")]
        [SerializeField] private BoardCameraController boardCameraController;

        [Tooltip("보드 위 플레이어를 관리하는 BoardPlayerController입니다.")]
        [SerializeField] private BoardPlayerController boardPlayerController;

        [Tooltip("보드 평면의 위치와 방향을 제공할 Transform입니다. MapGenerator 오브젝트를 연결할 수 있습니다.")]
        [SerializeField] private Transform boardPlane;

        [Header("Time Of Day")]
        [Tooltip("현재 보드 시간대를 제공하는 컨트롤러입니다.")]
        [SerializeField] private BoardSceneController boardSceneController;

        private void OnEnable()
        {
            ResolveReferences();

            if (boardSceneController != null)
            {
                boardSceneController.TimeOfDayChanged += HandleTimeOfDayChanged;
            }

            if (boardCameraController != null)
            {
                boardCameraController.LocateStarted += HandleLocateStarted;
                boardCameraController.LocateReturnStarted += HandleLocateReturnStarted;
                boardCameraController.LocateCompleted += HandleLocateCompleted;
            }
        }

        private void Start()
        {
            Bind();

            if (boardSceneController != null)
            {
                ApplyTimeOfDay(boardSceneController.CurrentTimeOfDay, true);
            }
        }

        private void OnDisable()
        {
            if (boardSceneController != null)
            {
                boardSceneController.TimeOfDayChanged -= HandleTimeOfDayChanged;
            }

            if (boardCameraController != null)
            {
                boardCameraController.LocateStarted -= HandleLocateStarted;
                boardCameraController.LocateReturnStarted -= HandleLocateReturnStarted;
                boardCameraController.LocateCompleted -= HandleLocateCompleted;
            }

            boardSightEffectView?.ResetFocus();
        }

        /// <summary>
        /// 인스펙터에 연결된 보드 씬 참조를 안개 View에 전달합니다.
        /// </summary>
        public void Bind()
        {
            if (!ValidateReferences())
            {
                return;
            }

            boardSightEffectView.Bind(
                boardCamera,
                boardPlayerController.transform,
                boardPlane);
        }

        private void ResolveReferences()
        {
            if (boardSceneController == null)
            {
                boardSceneController = FindFirstObjectByType<BoardSceneController>();
            }

            if (boardCameraController == null && boardCamera != null)
            {
                boardCameraController = boardCamera.GetComponent<BoardCameraController>();
            }

            if (boardCameraController == null)
            {
                boardCameraController = FindFirstObjectByType<BoardCameraController>();
            }
        }

        private void HandleLocateStarted(Transform focusTarget)
        {
            boardSightEffectView?.SetFocusTarget(focusTarget);
        }

        private void HandleLocateCompleted()
        {
            boardSightEffectView?.ResetFocus();
        }

        private void HandleLocateReturnStarted()
        {
            boardSightEffectView?.ResetFocus();
        }

        private void HandleTimeOfDayChanged(BoardTimeOfDay timeOfDay)
        {
            ApplyTimeOfDay(timeOfDay, false);
        }

        private void ApplyTimeOfDay(BoardTimeOfDay timeOfDay, bool immediate)
        {
            if (boardSightEffectView == null)
            {
                return;
            }

            BoardSightState targetState = timeOfDay switch
            {
                BoardTimeOfDay.Noon => BoardSightState.State2,
                BoardTimeOfDay.Night => BoardSightState.State3,
                _ => BoardSightState.State1
            };

            if (boardSightEffectView.CurrentState == targetState)
            {
                return;
            }

            boardSightEffectView.SetState(targetState, immediate);
        }

        private bool ValidateReferences()
        {
            bool isValid = true;

            if (boardSightEffectView == null)
            {
                Debug.LogError(
                    "[BoardSightEffectController] BoardSightEffectView가 연결되지 않았습니다.",
                    this);
                isValid = false;
            }

            if (boardCamera == null)
            {
                Debug.LogError(
                    "[BoardSightEffectController] 보드 Main Camera가 연결되지 않았습니다.",
                    this);
                isValid = false;
            }

            if (boardPlayerController == null)
            {
                Debug.LogError(
                    "[BoardSightEffectController] BoardPlayerController가 연결되지 않았습니다.",
                    this);
                isValid = false;
            }

            if (boardPlane == null)
            {
                Debug.LogError(
                    "[BoardSightEffectController] 보드 Plane Transform이 연결되지 않았습니다.",
                    this);
                isValid = false;
            }

            return isValid;
        }
    }
}
