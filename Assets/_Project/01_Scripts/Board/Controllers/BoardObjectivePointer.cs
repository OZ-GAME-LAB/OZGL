using OzGameLab01.Board.Views;
using OzGameLab01.Controllers;
using UnityEngine;

namespace OzGameLab01.Map
{
    /// <summary>
    /// 플레이어와 목표 정보를 조회하여 방향 표시 뷰에 전달합니다.
    /// 씬의 독립된 루트에 부착하고, 바늘 모델은 자식 오브젝트로 연결합니다.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("OZGL/Map/Board Objective Pointer")]
    public sealed class BoardObjectivePointer : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("현재 목표를 관리하는 씬의 MapRouteDirector를 연결하세요.")]
        [SerializeField] private MapRouteDirector mapRouteDirector;

        [Tooltip("추적할 플레이어입니다. 비워두면 BoardPlayerController.Instance를 사용합니다.")]
        [SerializeField] private Transform playerTarget;

        [Tooltip("이 루트 아래의 바늘 오브젝트입니다. 루트 자신이나 플레이어는 연결하지 마세요.")]
        [SerializeField] private Transform needle;

        [Header("Placement")]
        [Tooltip("플레이어 중심에서 바늘까지의 수평 거리입니다. 0이면 중심에서 회전합니다.")]
        [Min(0f)] [SerializeField] private float distanceFromPlayer = 1.2f;

        [Tooltip("플레이어 Transform 위치를 기준으로 한 표시 높이입니다.")]
        [SerializeField] private float heightOffset = 0.5f;

        [Header("Rotation")]
        [Tooltip("초당 회전 각도입니다. 0이면 목표 방향으로 즉시 회전합니다.")]
        [Min(0f)] [SerializeField] private float rotationSpeed = 360f;

        [Tooltip("스프라이트의 수평 방향 보정 각도입니다. 이미지의 바늘이 위쪽(+Y)을 향하면 0을 사용합니다. X축은 항상 90도로 눕힙니다.")]
        [SerializeField] private float needleYawOffset = 0f;

        [Header("Visibility")]
        [Tooltip("목표까지의 수평 거리가 이 값 이하이면 바늘을 숨깁니다.")]
        [Min(0f)] [SerializeField] private float hideDistance = 0.15f;

        private BoardObjectivePointerView _view;

        private void OnEnable()
        {
            _view = new BoardObjectivePointerView(transform, needle);

            if (!_view.HasValidNeedle)
            {
                Debug.LogError("[BoardObjectivePointer] Needle에 이 루트의 자식 바늘 오브젝트를 연결하세요.", this);
                enabled = false;
                return;
            }

            _view.Hide();
        }

        private void LateUpdate()
        {
            // Awake 실행 순서에 의존하지 않도록 플레이어가 준비될 때까지 기다립니다.
            if (playerTarget == null && BoardPlayerController.Instance != null)
            {
                playerTarget = BoardPlayerController.Instance.transform;
            }

            MapNode objective = mapRouteDirector != null && mapRouteDirector.isActiveAndEnabled
                ? mapRouteDirector.CurrentObjective
                : null;

            GameObject objectiveView = mapRouteDirector != null ? mapRouteDirector.CurrentObjectiveView : null;
            if (playerTarget == null || objective == null || objectiveView == null ||
                (objective.Type != NodeType.Elite && objective.Type != NodeType.Boss))
            {
                _view.Hide();
                return;
            }

            _view.Show(playerTarget.position, objectiveView.transform.position, distanceFromPlayer, heightOffset, rotationSpeed, needleYawOffset, hideDistance, Time.unscaledDeltaTime);
        }

        private void OnDisable()
        {
            _view?.Hide();
        }
    }
}