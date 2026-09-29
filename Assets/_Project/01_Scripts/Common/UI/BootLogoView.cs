using System;
using System.Collections;
using UnityEngine;

namespace OzGameLab01.GameFlow.Views
{
    [DisallowMultipleComponent]
    public sealed class BootLogoView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup logoCanvasGroup;
        [SerializeField, Min(0f)] private float fadeInDuration = 0.6f;
        [SerializeField, Min(0f)] private float displayDuration = 1f;
        [SerializeField, Min(0f)] private float fadeOutDuration = 0.6f;
        [SerializeField] private bool playOnStart = true;

        private Coroutine _playRoutine;

        #region Properties

        /// <summary>
        /// 현재 로고 연출이 재생 중인지 반환합니다.
        /// </summary>
        public bool IsPlaying => _playRoutine != null;

        /// <summary>
        /// 로고 연출이 정상 완료되었거나 건너뛰기 처리되었는지 반환합니다.
        /// </summary>
        public bool IsCompleted { get; private set; }

        #endregion

        #region Events

        /// <summary>
        /// 로고의 페이드 인, 유지, 페이드 아웃 연출이 모두 끝났을 때 발생합니다.
        /// </summary>
        public event Action Completed;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            Initialize();
        }

        private void Start()
        {
            if (playOnStart)
            {
                Play();
            }
        }

        private void OnDisable()
        {
            if (_playRoutine == null)
            {
                return;
            }

            StopCoroutine(_playRoutine);
            _playRoutine = null;

            SetLogoAlpha(0f);
            Complete();
        }

        #endregion

        #region API

        /// <summary>
        /// 로고의 페이드 인, 유지, 페이드 아웃 연출을 재생합니다.
        /// 이미 재생 중이면 현재 실행 중인 코루틴을 반환합니다.
        /// </summary>
        /// <returns>현재 실행 중인 로고 연출 코루틴입니다.</returns>
        public Coroutine Play()
        {
            if (_playRoutine != null)
            {
                return _playRoutine;
            }

            IsCompleted = false;
            _playRoutine = StartCoroutine(PlaySequence());

            return _playRoutine;
        }

        /// <summary>
        /// 로고 연출을 시작하고 완료될 때까지 대기합니다.
        /// 게임 초기화 흐름에서 타이틀 진입 시점을 제어할 때 사용합니다.
        /// </summary>
        /// <returns>로고 연출 완료를 기다리는 코루틴입니다.</returns>
        public IEnumerator PlayAndWait()
        {
            Play();

            while (!IsCompleted)
            {
                yield return null;
            }
        }

        /// <summary>
        /// 현재 로고 연출을 즉시 종료하고 완료 상태로 전환합니다.
        /// </summary>
        public void Skip()
        {
            if (_playRoutine != null)
            {
                StopCoroutine(_playRoutine);
                _playRoutine = null;
            }

            SetLogoAlpha(0f);
            Complete();
        }

        /// <summary>
        /// 로고 연출을 중단하고 최초 대기 상태로 초기화합니다.
        /// </summary>
        public void ResetView()
        {
            if (_playRoutine != null)
            {
                StopCoroutine(_playRoutine);
                _playRoutine = null;
            }

            IsCompleted = false;
            SetLogoAlpha(0f);
        }

        #endregion

        #region Internal

        private void Initialize()
        {
            if (logoCanvasGroup != null)
            {
                logoCanvasGroup.interactable = false;
                logoCanvasGroup.blocksRaycasts = false;
            }

            IsCompleted = false;
            SetLogoAlpha(0f);
        }

        private IEnumerator PlaySequence()
        {
            SetLogoAlpha(0f);

            yield return FadeLogo(0f,1f,fadeInDuration);

            if (displayDuration > 0f)
            {
                yield return new WaitForSecondsRealtime(displayDuration);
            }

            yield return FadeLogo(1f,0f,fadeOutDuration);
            _playRoutine = null;
            Complete();
        }

        private IEnumerator FadeLogo(float startAlpha,float targetAlpha,float duration)
        {
            if (logoCanvasGroup == null)
            {
                yield break;
            }

            SetLogoAlpha(startAlpha);

            if (duration <= 0f)
            {
                SetLogoAlpha(targetAlpha);
                yield break;
            }

            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);
                float easedProgress = Mathf.SmoothStep(0f,1f,progress);
                float alpha = Mathf.Lerp(startAlpha,targetAlpha,easedProgress);
                SetLogoAlpha(alpha);
                yield return null;
            }

            SetLogoAlpha(targetAlpha);
        }

        private void SetLogoAlpha(float alpha)
        {
            if (logoCanvasGroup == null)
            {
                return;
            }

            logoCanvasGroup.alpha = Mathf.Clamp01(alpha);
        }

        private void Complete()
        {
            if (IsCompleted)
            {
                return;
            }

            IsCompleted = true;
            Completed?.Invoke();
        }

        #endregion

#if UNITY_EDITOR
        #region Editor

        private void OnValidate()
        {
            fadeInDuration = Mathf.Max(0f, fadeInDuration);
            displayDuration = Mathf.Max(0f, displayDuration);
            fadeOutDuration = Mathf.Max(0f, fadeOutDuration);
        }

        [ContextMenu("Test/Play")]
        private void TestPlay()
        {
            if (Application.isPlaying)
            {
                Play();
            }
        }

        [ContextMenu("Test/Skip")]
        private void TestSkip()
        {
            if (Application.isPlaying)
            {
                Skip();
            }
        }

        [ContextMenu("Test/Reset")]
        private void TestReset()
        {
            ResetView();
        }

        [ContextMenu("Test/Show Logo")]
        private void TestShowLogo()
        {
            SetLogoAlpha(1f);
        }

        [ContextMenu("Test/Hide Logo")]
        private void TestHideLogo()
        {
            SetLogoAlpha(0f);
        }

        #endregion
#endif
    }
}