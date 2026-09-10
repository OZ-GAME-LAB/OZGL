using System.Collections;
using UnityEngine;
using OzGameLab01.UI;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace TEST.FogUI
{
    /// <summary>
    /// 안개 및 보스 등장 위치 안내 테스트용 컨트롤러입니다.
    /// WASD: 플레이어 이동 / 마우스 휠: 줌.
    /// F: 지정 대상으로 이동 → 대기 → 플레이어 복귀.
    /// TODO: 실제 보드 카메라 연결 완료 후 삭제 예정.
    /// </summary>
    public sealed class FogTestController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private Transform player;
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float zoomStep = 2f;

        [Header("Fog Focus Test")]
        [SerializeField] private BoardSightEffectView fogView;
        [SerializeField] private Transform alternateFocusTarget;

        [Header("Spawn Presentation")]
        [SerializeField, Min(0.01f)]
        private float moveDuration = 1.2f;

        [SerializeField, Min(0f)]
        private float holdDuration = 2f;

        private float _distance;
        private Vector3 _cameraOffsetDirection;

        // 카메라와 안개가 함께 추적하는 임시 중심입니다.
        private Transform _presentationFocus;
        private Coroutine _presentationRoutine;
        private bool _isPresenting;

        public bool IsPresenting => _isPresenting;

        private void Start()
        {
            if (player == null)
            {
                Debug.LogWarning("Player를 연결하세요.", this);
                enabled = false;
                return;
            }

            Vector3 offset = transform.position - player.position;

            if (offset.sqrMagnitude < 0.0001f)
            {
                offset = new Vector3(0f, 15f, -10f);
            }

            _distance = Mathf.Clamp(offset.magnitude, 5f, 50f);
            _cameraOffsetDirection = offset.normalized;

            transform.position =
                player.position + _cameraOffsetDirection * _distance;

            transform.LookAt(player.position);

            GameObject focusObject =
                new GameObject("FogTest_PresentationFocus");

            _presentationFocus = focusObject.transform;
            _presentationFocus.position = player.position;

            if (fogView != null)
            {
                fogView.ResetFocus();
            }
        }

        private void Update()
        {
            if (player == null || _isPresenting)
            {
                return;
            }

            Vector2 move = Vector2.zero;
            float scroll = 0f;
            bool showTarget = false;

#if ENABLE_INPUT_SYSTEM
            Keyboard keyboard = Keyboard.current;

            if (keyboard != null)
            {
                move.x =
                    (keyboard.dKey.isPressed ? 1f : 0f) -
                    (keyboard.aKey.isPressed ? 1f : 0f);

                move.y =
                    (keyboard.wKey.isPressed ? 1f : 0f) -
                    (keyboard.sKey.isPressed ? 1f : 0f);

                showTarget = keyboard.fKey.wasPressedThisFrame;
            }

            if (Mouse.current != null)
            {
                scroll = Mouse.current.scroll.ReadValue().y;
            }
#elif ENABLE_LEGACY_INPUT_MANAGER
            move.x =
                (Input.GetKey(KeyCode.D) ? 1f : 0f) -
                (Input.GetKey(KeyCode.A) ? 1f : 0f);

            move.y =
                (Input.GetKey(KeyCode.W) ? 1f : 0f) -
                (Input.GetKey(KeyCode.S) ? 1f : 0f);

            scroll = Input.mouseScrollDelta.y;
            showTarget = Input.GetKeyDown(KeyCode.F);
#endif

            if (showTarget)
            {
                ShowSpawnTarget(alternateFocusTarget);
                return;
            }

            move = Vector2.ClampMagnitude(move, 1f);

            player.position +=
                new Vector3(move.x, 0f, move.y) *
                moveSpeed * Time.deltaTime;

            if (Mathf.Abs(scroll) > 0.01f)
            {
                _distance = Mathf.Clamp(
                    _distance - Mathf.Sign(scroll) * zoomStep,
                    5f,
                    50f);
            }
        }

        private void LateUpdate()
        {
            if (player == null)
            {
                return;
            }

            Vector3 center =
                _isPresenting && _presentationFocus != null
                    ? _presentationFocus.position
                    : player.position;

            transform.position =
                center + _cameraOffsetDirection * _distance;
        }

        /// <summary>
        /// 지정 대상으로 카메라와 안개 중심을 이동하고,
        /// 잠시 보여준 뒤 플레이어에게 복귀합니다.
        /// 진행 중인 요청은 중복 실행하지 않습니다.
        /// </summary>
        public void ShowSpawnTarget(Transform target)
        {
            if (!isActiveAndEnabled || _isPresenting)
            {
                return;
            }

            if (player == null ||
                fogView == null ||
                _presentationFocus == null)
            {
                Debug.LogWarning(
                    "초기화 상태와 Player, Fog View 연결을 확인하세요.",
                    this);
                return;
            }

            if (target == null)
            {
                Debug.LogWarning("안내할 대상을 연결하세요.", this);
                return;
            }

            _presentationFocus.position = player.position;
            _isPresenting = true;

            fogView.SetFocusTarget(_presentationFocus);

            _presentationRoutine =
                StartCoroutine(ShowTargetRoutine(target));
        }

        private IEnumerator ShowTargetRoutine(Transform target)
        {
            // 1. 보스 위치로 이동
            yield return MoveFocusTo(target);

            // 2. 보스를 잠시 보여줌
            float elapsed = 0f;

            while (elapsed < holdDuration && target != null)
            {
                _presentationFocus.position = target.position;
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            // 3. 현재 플레이어 위치로 복귀
            yield return MoveFocusTo(player);

            if (fogView != null)
            {
                fogView.ResetFocus();
            }

            _isPresenting = false;
            _presentationRoutine = null;
        }

        private IEnumerator MoveFocusTo(Transform target)
        {
            if (target == null)
            {
                yield break;
            }

            Vector3 startPosition = _presentationFocus.position;
            float duration = Mathf.Max(0.01f, moveDuration);
            float elapsed = 0f;

            while (elapsed < duration)
            {
                if (target == null)
                {
                    yield break;
                }

                elapsed += Time.unscaledDeltaTime;

                float progress = Mathf.Clamp01(elapsed / duration);
                float eased = Mathf.SmoothStep(0f, 1f, progress);

                _presentationFocus.position = Vector3.Lerp(
                    startPosition,
                    target.position,
                    eased);

                yield return null;
            }

            if (target != null)
            {
                _presentationFocus.position = target.position;
            }
        }

        private void OnDisable()
        {
            if (_presentationRoutine != null)
            {
                StopCoroutine(_presentationRoutine);
                _presentationRoutine = null;
            }

            if (_isPresenting)
            {
                if (fogView != null)
                {
                    fogView.ResetFocus();
                }

                _isPresenting = false;

                if (player != null)
                {
                    transform.position =
                        player.position +
                        _cameraOffsetDirection * _distance;
                }
            }
        }

        private void OnDestroy()
        {
            if (_presentationFocus != null)
            {
                Destroy(_presentationFocus.gameObject);
            }
        }
    }
}