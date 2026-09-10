using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace OzGameLab01.UI
{
    [DisallowMultipleComponent]
    public sealed class EventChoiceButtonView : MonoBehaviour,IPointerEnterHandler,IPointerExitHandler
    {
        [Header("References")]
        [SerializeField] private Button button;
        [SerializeField] private Image artImage;
        [SerializeField] private TextMeshProUGUI labelText;

        [Header("Effects")]
        [SerializeField] private UITransformEffect transformEffect;
        [SerializeField] private UIFadeEffect disabledFadeEffect;
        [SerializeField] private UIBurnEffect burnEffect;

        [Header("Unselected")]
        [SerializeField, Range(0f, 1f)] private float disabledAlpha = 0.6f;
        [SerializeField, Min(0f)] private float unselectedHoldDuration = 0.1f;

        private int _choiceId;
        private Action<int> _onClick;
        private Coroutine _exitRoutine;
        private bool _isExiting;

        #region Unity Lifecycle

        private void OnEnable()
        {
            if (button == null || _onClick == null)
            {
                return;
            }

            button.onClick.RemoveListener(HandleClick);
            button.onClick.AddListener(HandleClick);
        }

        private void OnDisable()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(HandleClick);
            }

            StopExitRoutine();
        }

        #endregion

        #region Public API

        /// <summary>
        /// Choice 표시 데이터와 클릭 콜백을 View에 연결하고
        /// 모든 UI 연출 상태를 초기 상태로 복구합니다.
        /// </summary>
        /// <param name="data">표시할 Choice 데이터입니다.</param>
        /// <param name="onClick">
        /// 버튼 클릭 시 Choice ID와 함께 호출되는 콜백입니다.
        /// </param>
        
        public void Bind(int choiceId, EventChoice data, Action<int> onClick)
        {
            ResetVisual();

            _choiceId = choiceId;
            _onClick = onClick;

            if (labelText != null)
            {
                labelText.text =
                    data.ChoiceDialog ?? string.Empty;
            }
            if (artImage != null)
            {
                artImage.sprite = data.ChoiceSprite;
                artImage.enabled = data.ChoiceSprite != null;
            }
            if (button != null)
            {
                button.onClick.RemoveListener(HandleClick);
                button.onClick.AddListener(HandleClick);
                button.interactable = true;
            }
        }

        /// <summary>
        /// Choice 버튼의 입력 가능 여부를 변경합니다.
        /// </summary>
        /// <param name="isInteractable">
        /// true면 입력을 허용하고 false면 입력을 차단합니다.
        /// </param>
        public void SetInteractable(bool isInteractable)
        {
            if (button != null)
            {
                button.interactable = isInteractable;
            }
        }

        /// <summary>
        /// 선택되지 않은 Choice의 종료 연출을 실행합니다.
        /// Disabled Fade 후 Burn 연출을 순차적으로 재생합니다.
        /// </summary>
        /// <param name="onComplete">
        /// 종료 연출 완료 후 호출되는 단발성 콜백입니다.
        /// </param>
        public void PlayUnselectedExit(Action onComplete = null)
        {
            BeginExit();

            _exitRoutine = StartCoroutine(PlayUnselectedExitRoutine(onComplete));
        }

        /// <summary>
        /// 선택된 Choice의 종료 연출을 실행합니다.
        /// 선택 강조 Scale 연출 후 Burn 연출을 순차적으로 재생합니다.
        /// </summary>
        /// <param name="onComplete">
        /// 종료 연출 완료 후 호출되는 단발성 콜백입니다.
        /// </param>
        public void PlaySelectedExit(Action onComplete = null)
        {
            BeginExit();

            _exitRoutine = StartCoroutine(PlaySelectedExitRoutine(onComplete));
        }

        /// <summary>
        /// Choice의 모든 연출 상태를 초기 상태로 복구합니다.
        /// 재사용되는 런타임 프리팹을 초기화할 때 사용할 수 있습니다.
        /// </summary>
        public void ResetVisual()
        {
            StopExitRoutine();

            _isExiting = false;

            transformEffect?.ResetEffect();
            disabledFadeEffect?.ResetEffect();
            burnEffect?.ResetEffect();
        }

        #endregion

        #region Pointer Events

        public void OnPointerEnter(
            PointerEventData eventData)
        {
            HandlePointerEnter();
        }

        public void OnPointerExit(
            PointerEventData eventData)
        {
            HandlePointerExit();
        }

        private void HandlePointerEnter()
        {
            if (!CanPlayHover())
            {
                return;
            }

            transformEffect?.PlayHover();
        }

        private void HandlePointerExit()
        {
            if (!CanPlayHover())
            {
                return;
            }

            transformEffect?.StopHover();
        }

        private bool CanPlayHover()
        {
            return !_isExiting && button != null && button.interactable;
        }

        #endregion

        #region Event Handler

        private void HandleClick()
        {
            if (_isExiting)
            {
                return;
            }
            burnEffect.Play(()=> _onClick?.Invoke(_choiceId));
            //_onClick?.Invoke(_choiceId);
        }

        #endregion

        #region Exit Sequence

        private void BeginExit()
        {
            StopExitRoutine();

            _isExiting = true;

            if (button != null)
            {
                button.interactable = false;
            }
        }

        private IEnumerator PlayUnselectedExitRoutine(Action onComplete)
        {
            transformEffect?.StopHover();

            if (disabledFadeEffect != null)
            {
                yield return disabledFadeEffect.PlayTo(disabledAlpha);
            }

            if (unselectedHoldDuration > 0f)
            {
                yield return new WaitForSecondsRealtime(unselectedHoldDuration);
            }

            if (burnEffect != null)
            {
                yield return burnEffect.Play();
            }

            _exitRoutine = null;

            onComplete?.Invoke();
        }

        private IEnumerator PlaySelectedExitRoutine(Action onComplete)
        {
            if (transformEffect != null)
            {
                yield return transformEffect.PlaySelected();
            }

            if (burnEffect != null)
            {
                yield return burnEffect.Play();
            }

            _exitRoutine = null;

            onComplete?.Invoke();
        }

        private void StopExitRoutine()
        {
            if (_exitRoutine == null)
            {
                return;
            }

            StopCoroutine(_exitRoutine);
            _exitRoutine = null;
        }

        #endregion

#if UNITY_EDITOR
        #region Editor Test

        [ContextMenu("Test/Reset")]
        private void TestReset()
        {
            ResetVisual();

            if (button != null)
            {
                button.interactable = true;
            }
        }

        [ContextMenu("Test/Play Unselected Exit")]
        private void TestPlayUnselectedExit()
        {
            ResetVisual();
            PlayUnselectedExit();
        }

        [ContextMenu("Test/Play Selected Exit")]
        private void TestPlaySelectedExit()
        {
            ResetVisual();
            PlaySelectedExit();
        }

        #endregion
#endif
    }
}