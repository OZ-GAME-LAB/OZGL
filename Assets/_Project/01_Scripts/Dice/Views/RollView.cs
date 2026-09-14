using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OzGameLab01.UI
{
    [DisallowMultipleComponent]
    public sealed class RollView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Button rollButton;
        [SerializeField] private TMP_Text rollText;

        [Header("3D Dice")]
        [SerializeField] private Transform diceTransform;
        [SerializeField] private Camera diceCamera;

        [Header("Roll Animation")]
        [Tooltip("감속을 포함한 전체 연출 시간입니다. 최소 2초입니다.")]
        [SerializeField, Min(2f)]
        private float rollDuration = 3f;

        [Tooltip("랜덤 회전 횟수의 최솟값과 최댓값")]
        [SerializeField]
        private Vector2Int rollRevolutionRange = new (6, 9);

        private Coroutine rollRoutine;
        private Action<int> completionCallback;
        private bool requestedInteractable = true;
        private bool interactableInitialized;

        #region Properties

        public Button RollButton => rollButton;
        public TMP_Text RollText => rollText;

        public bool IsVisible => gameObject.activeSelf;
        public bool IsRolling { get; private set; }

        public bool IsInteractable
        {
            get => rollButton != null && rollButton.interactable;
            set => SetInteractable(value);
        }

        public string Label
        {
            get => rollText != null ? rollText.text : string.Empty;
            set
            {
                if (rollText != null)
                    rollText.text = value ?? string.Empty;
            }
        }

        #endregion

        #region Events

        public event Action<RollView> RollClicked;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            InitializeInteractable();
        }

        private void OnEnable()
        {
            InitializeInteractable();

            if (rollButton != null)
                rollButton.onClick.AddListener(HandleRollClick);

            if (diceCamera != null)
                diceCamera.enabled = true;

            RefreshButton();
        }

        private void OnDisable()
        {
            if (rollButton != null)
                rollButton.onClick.RemoveListener(HandleRollClick);

            CancelRoll();

            if (diceCamera != null)
                diceCamera.enabled = false;
        }

        #endregion

        #region Public API

        public void Show()
        {
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        public void SetLabel(string value)
        {
            Label = value;
        }

        public void SetInteractable(bool value)
        {
            interactableInitialized = true;
            requestedInteractable = value;
            RefreshButton();
        }

        public bool PlayRoll(
            int result,
            Action<int> onCompleted = null)
        {
            if (!Application.isPlaying || !isActiveAndEnabled || IsRolling)
            {
                return false;
            }

            if (!ValidateDice(result))
                return false;

            if (!diceTransform.gameObject.activeInHierarchy || !diceCamera.gameObject.activeInHierarchy)
            {
                Debug.LogWarning( "[RollView] DiceCube와 DiceCamera를 활성화하세요.", this);
                return false;
            }

            diceCamera.enabled = true;

            completionCallback = onCompleted;
            IsRolling = true;
            RefreshButton();

            rollRoutine = StartCoroutine(RollRoutine(result));
            return true;
        }

        /// <summary>
        /// 진행 중인 연출을 취소하고 결과 면을 즉시 표시합니다.
        /// 완료 콜백은 호출하지 않습니다.
        /// </summary>
        public bool SetFaceImmediate(int result)
        {
            if (!ValidateDice(result))
                return false;

            CancelRoll();

            diceTransform.rotation = GetResultRotation(result);
            return true;
        }

        /// <summary>
        /// 현재 자세에서 연출을 중단합니다.
        /// 완료 콜백은 호출하지 않습니다.
        /// </summary>
        public void CancelRoll()
        {
            if (rollRoutine != null)
                StopCoroutine(rollRoutine);

            rollRoutine = null;
            completionCallback = null;
            IsRolling = false;

            RefreshButton();
        }

        #endregion

        #region Animation

        private IEnumerator RollRoutine(int result)
        {
            yield return null;

            if (diceTransform == null || diceCamera == null)
            {
                ClearRollState();
                yield break;
            }

            float duration = Mathf.Max(2f, rollDuration);

            int minTurns = Mathf.Clamp(rollRevolutionRange.x, 4, 12);
            int maxTurns = Mathf.Clamp(rollRevolutionRange.y, minTurns, 12);

            int revolutions = UnityEngine.Random.Range(minTurns, maxTurns + 1);

            Quaternion startRotation = diceTransform.rotation;
            Quaternion targetRotation = GetResultRotation(result);

            Vector3 localAxis = new Vector3(UnityEngine.Random.Range(0.6f, 1f),UnityEngine.Random.Range(0.6f, 1f) * (UnityEngine.Random.value < 0.5f ? -1f : 1f), UnityEngine.Random.Range(-0.4f, 0.4f)).normalized;
            Vector3 worldAxis = diceCamera.transform.TransformDirection(localAxis).normalized;

            float direction = UnityEngine.Random.value < 0.5f ? -1f : 1f;
            float totalAngle = 360f * revolutions * direction;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                if (diceTransform == null || diceCamera == null)
                {
                    ClearRollState();
                    yield break;
                }

                elapsed += Time.unscaledDeltaTime;

                float t = Mathf.Clamp01(elapsed / duration);
                float spinProgress = EvaluateSpinProgress(t);
                float alignmentProgress = t * t * (3f - 2f * t);

                Quaternion alignedRotation = Quaternion.Slerp(startRotation,targetRotation, alignmentProgress);
                Quaternion spinRotation = Quaternion.AngleAxis( totalAngle * spinProgress, worldAxis);

                diceTransform.rotation = spinRotation * alignedRotation;

                yield return null;
            }

            if (diceTransform == null)
            {
                ClearRollState();
                yield break;
            }

            diceTransform.rotation = targetRotation;

            Action<int> callback = completionCallback;

            ClearRollState();

            callback?.Invoke(result);
        }

        private static float EvaluateSpinProgress(float t)
        {
            t = Mathf.Clamp01(t);

            const float fastEnd = 0.35f;
            const float totalDistance = fastEnd + (1f - fastEnd) * 0.5f;

            float traveled;

            if (t <= fastEnd)
            {
                traveled = t;
            }
            else
            {
                float u = (t - fastEnd) / (1f - fastEnd);
                float u2 = u * u;
                float u3 = u2 * u;
                float u4 = u3 * u;

                // 1 - SmoothStep(u)의 적분
                float decelerationDistance =
                    u - u3 + 0.5f * u4;

                traveled = fastEnd + (1f - fastEnd) * decelerationDistance;
            }

            return Mathf.Clamp01(traveled / totalDistance);
        }

        private Quaternion GetResultRotation(int result)
        {
            Vector3 faceNormal;
            Vector3 faceUp;

            switch (result)
            {
                case 1:
                    faceNormal = Vector3.forward;
                    faceUp = Vector3.up;
                    break;

                case 2:
                    faceNormal = Vector3.up;
                    faceUp = Vector3.back;
                    break;

                case 3:
                    faceNormal = Vector3.right;
                    faceUp = Vector3.up;
                    break;

                case 4:
                    faceNormal = Vector3.left;
                    faceUp = Vector3.up;
                    break;

                case 5:
                    faceNormal = Vector3.down;
                    faceUp = Vector3.forward;
                    break;

                default: // 6
                    faceNormal = Vector3.back;
                    faceUp = Vector3.up;
                    break;
            }

            Quaternion localFaceRotation = Quaternion.LookRotation(faceNormal, faceUp);
            Quaternion cameraFacingRotation = Quaternion.LookRotation( -diceCamera.transform.forward, diceCamera.transform.up);
            return cameraFacingRotation * Quaternion.Inverse(localFaceRotation);
        }

        private void ClearRollState()
        {
            rollRoutine = null;
            completionCallback = null;
            IsRolling = false;

            RefreshButton();
        }

        #endregion

        #region Validation

        private bool ValidateDice(int result)
        {
            if (result < 1 || result > 6)
            {
                Debug.LogWarning("[RollView] 주사위 결과는 1~6이어야 합니다.",this);
                return false;
            }

            if (diceTransform == null || diceCamera == null)
            {
                Debug.LogWarning("[RollView] Dice Transform과 Dice Camera를 연결하세요.", this);
                return false;
            }

            return true;
        }

        #endregion

        #region UI

        private void InitializeInteractable()
        {
            if (interactableInitialized)
                return;

            requestedInteractable = rollButton == null || rollButton.interactable;
            interactableInitialized = true;
        }

        private void RefreshButton()
        {
            if (rollButton != null)
            {
                rollButton.interactable = requestedInteractable && !IsRolling;
            }
        }

        private void HandleRollClick()
        {
            if (!IsRolling && IsInteractable)
                RollClicked?.Invoke(this);
        }

        #endregion

        #region Inspector Test

#if UNITY_EDITOR
        [ContextMenu("Test/Play Dice Roll")]
        private void TestPlayDiceRoll()
        {
            if (!Application.isPlaying || !isActiveAndEnabled)
            {
                Debug.LogWarning("[RollView] Play 모드에서 RollView를 활성화한 뒤 테스트하세요.", this);
                return;
            }

            int randomResult = UnityEngine.Random.Range(1, 7);

            bool started = PlayRoll(randomResult, result =>
            {
                Debug.Log( $"[RollView] 연출 테스트 완료: {result}", this);
            });

            if (!started)
            {
                Debug.LogWarning("[RollView] 테스트를 시작하지 못했습니다. " +"진행 중인 연출과 참조 연결을 확인하세요.", this);
            }
        }
#endif

        #endregion
    }
}