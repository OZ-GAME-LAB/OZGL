using OzGameLab01.Controllers;
using UnityEngine;

namespace OZGL.Map
{
    /// <summary>
    /// 플레이어 주변에서 현재 중간 보스/보스 목표의 직선 방향을 표시합니다.
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

        private Quaternion currentHeading;
        private bool hasHeading;

        private void OnEnable()
        {
            hasHeading = false;

            if (needle == null || needle == transform || !needle.IsChildOf(transform))
            {
                Debug.LogError("[BoardObjectivePointer] Needle에 이 루트의 자식 바늘 오브젝트를 연결하세요.", this);
                enabled = false;
                return;
            }

            SetNeedleVisible(false);
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

            if (playerTarget == null || objective == null || objective.NodeView == null ||
                (objective.Type != NodeType.Elite && objective.Type != NodeType.Boss))
            {
                HideNeedle();
                return;
            }

            Vector3 direction = objective.NodeView.transform.position - playerTarget.position;
            direction.y = 0f;
            float threshold = Mathf.Max(0.001f, hideDistance);

            if (direction.sqrMagnitude <= threshold * threshold)
            {
                HideNeedle();
                return;
            }

            Quaternion desiredHeading = Quaternion.LookRotation(direction, Vector3.up);
            currentHeading = !hasHeading || rotationSpeed <= 0f
                ? desiredHeading
                : Quaternion.RotateTowards(currentHeading, desiredHeading, rotationSpeed * Time.unscaledDeltaTime);
            hasHeading = true;

            transform.position = playerTarget.position + Vector3.up * heightOffset;
            needle.position = transform.position + currentHeading * Vector3.forward * Mathf.Max(0f, distanceFromPlayer);
            // 스프라이트를 X축 90도로 눕힌 상태에서 월드 Y축 방향만 회전합니다.
            needle.rotation = currentHeading * Quaternion.Euler(0f, needleYawOffset, 0f)
                * Quaternion.Euler(90f, 0f, 0f);
            SetNeedleVisible(true);
        }

        private void OnDisable()
        {
            HideNeedle();
        }

        private void HideNeedle()
        {
            hasHeading = false;
            SetNeedleVisible(false);
        }

        private void SetNeedleVisible(bool visible)
        {
            // 잘못된 참조로 루트나 플레이어를 비활성화하지 않도록 자식 바늘만 제어합니다.
            if (needle != null && needle != transform && needle.IsChildOf(transform) &&
                needle.gameObject.activeSelf != visible)
            {
                needle.gameObject.SetActive(visible);
            }
        }
    }
}
