using UnityEngine;
using OzGameLab01.Controllers;

public class BoardCameraController : MonoBehaviour
{
    [Header("추적 설정")]
    [Tooltip("추적할 대상 (비워두면 시작할 때 알아서 플레이어를 찾습니다)")]
    public Transform target;
    [Tooltip("카메라가 플레이어를 따라가는 부드러운 속도")]
    public float followSpeed = 5f;

    [Header("위치 및 줌 설정")]
    [Tooltip("플레이어를 기준으로 한 기본 카메라 위치 (Y가 높이, Z가 뒤쪽 거리)")]
    public Vector3 defaultOffset = new Vector3(0f, 15f, -10f);

    [Tooltip("마우스 휠 스크롤 감도")]
    public float zoomSpeed = 1f;
    [Tooltip("최대 확대 한계 (작을수록 가까움)")]
    public float minZoomMultiplier = 0.3f;
    [Tooltip("최대 축소 한계 (클수록 멀어짐)")]
    public float maxZoomMultiplier = 2.5f;

    private float _currentZoom = 1.0f;

    private void LateUpdate()
    {
        // 1. 타겟이 없으면 BoardPlayerController를 자동으로 찾아서 연결합니다.
        if (target == null)
        {
            if (BoardPlayerController.Instance != null)
                target = BoardPlayerController.Instance.transform;

            // 아직 플레이어가 생성되지 않았다면 아무것도 하지 않고 대기합니다.
            if (target == null) return;
        }

        // 2. 마우스 휠 스크롤 값 받기
        float scroll = Input.mouseScrollDelta.y;
        if (Mathf.Abs(scroll) > 0.01f)
        {
            // 휠을 위로 굴리면(+) 줌 인, 아래로 굴리면(-) 줌 아웃되도록 처리
            _currentZoom -= scroll * zoomSpeed * 0.1f;

            // 너무 줌아웃되거나 줌인되는 것을 방지
            _currentZoom = Mathf.Clamp(_currentZoom, minZoomMultiplier, maxZoomMultiplier);
        }

        // 3. 목표 위치 계산 (기본 거리 오프셋에 줌 배율을 곱함)
        Vector3 desiredPosition = target.position + (defaultOffset * _currentZoom);

        // 4. 현재 위치에서 목표 위치로 부드럽게 이동 (Lerp 보간)
        transform.position = Vector3.Lerp(transform.position, desiredPosition, followSpeed * Time.deltaTime);
    }
}