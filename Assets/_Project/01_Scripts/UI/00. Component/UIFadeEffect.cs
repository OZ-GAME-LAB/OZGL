using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace OzGameLab01.UI
{
    [DisallowMultipleComponent]
    public sealed class UIFadeEffect : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Graphic targetGraphic;

        [Header("Fade")]
        [SerializeField, Min(0f)] private float defaultDuration = 0.2f;
        [SerializeField, Range(0f, 1f)] private float resetAlpha = 0f;

        [Header("Editor Test")]
        [SerializeField, Range(0f, 1f)] private float testTargetAlpha = 0.6f;

        private Coroutine _fadeRoutine;

        #region Unity Lifecycle

        private void Awake()
        {
            ResetEffect();
        }

        private void OnDisable()
        {
            StopEffect();
        }

        #endregion

        #region Public API

        /// <summary>
        /// 지정한 Alpha 값까지 기본 시간 동안 Fade 연출을 실행합니다.
        /// 반환된 Coroutine을 이용해 외부 연출 시퀀스에서 완료 시점을 기다릴 수 있습니다.
        /// </summary>
        /// <param name="targetAlpha">최종 Alpha 값입니다. 0~1 범위로 제한됩니다.</param>
        /// <param name="onComplete">Fade 완료 후 호출되는 단발성 콜백입니다.</param>
        /// <returns>현재 실행 중인 Fade Coroutine입니다.</returns>
        public Coroutine PlayTo(float targetAlpha,Action onComplete = null)
        {
            return PlayTo(targetAlpha,defaultDuration,onComplete);
        }

        /// <summary>
        /// 지정한 Alpha 값까지 지정한 시간 동안 Fade 연출을 실행합니다.
        /// 반환된 Coroutine을 이용해 외부 연출 시퀀스에서 완료 시점을 기다릴 수 있습니다.
        /// </summary>
        /// <param name="targetAlpha">최종 Alpha 값입니다. 0~1 범위로 제한됩니다.</param>
        /// <param name="duration">Fade 연출 시간입니다.</param>
        /// <param name="onComplete">Fade 완료 후 호출되는 단발성 콜백입니다.</param>
        /// <returns>현재 실행 중인 Fade Coroutine입니다.</returns>
        public Coroutine PlayTo(float targetAlpha,float duration,Action onComplete = null)
        {
            StopEffect();
            _fadeRoutine = StartCoroutine(PlayRoutine(Mathf.Clamp01(targetAlpha), Mathf.Max(0f, duration), onComplete));

            return _fadeRoutine;
        }

        /// <summary>
        /// Fade 연출 없이 대상 Graphic의 Alpha 값을 즉시 변경합니다.
        /// </summary>
        /// <remarks>
        /// 재생 중인 Fade는 중단하지 않으므로 다음 프레임에 Alpha가 다시 변경될 수 있습니다.
        /// 값을 고정하려면 StopEffect()를 먼저 호출한 뒤 SetAlpha(alpha)를 호출하세요.
        /// </remarks>
        /// <param name="alpha">적용할 Alpha 값입니다. 0~1 범위로 제한됩니다.</param>
        public void SetAlpha(float alpha)
        {
            if (targetGraphic == null)
            {
                return;
            }

            Color color = targetGraphic.color;
            color.a = Mathf.Clamp01(alpha);

            targetGraphic.color = color;
        }

        /// <summary>
        /// 현재 실행 중인 Fade 연출을 즉시 중단합니다.
        /// 현재 Alpha 값은 그대로 유지됩니다.
        /// </summary>
        public void StopEffect()
        {
            if (_fadeRoutine == null)
            {
                return;
            }

            StopCoroutine(_fadeRoutine);
            _fadeRoutine = null;
        }

        /// <summary>
        /// Fade 연출을 중단하고 대상 Graphic의 Alpha를 설정된 초기값으로 복구합니다.
        /// </summary>
        public void ResetEffect()
        {
            StopEffect();
            SetAlpha(resetAlpha);
        }

        #endregion

        #region Fade Effect

        private IEnumerator PlayRoutine(float targetAlpha,float duration,Action onComplete)
        {
            if (targetGraphic == null)
            {
                _fadeRoutine = null;

                onComplete?.Invoke();
                yield break;
            }

            Color color = targetGraphic.color;
            float startAlpha = color.a;

            if (duration <= 0f)
            {
                SetAlpha(targetAlpha);

                _fadeRoutine = null;
                onComplete?.Invoke();

                yield break;
            }

            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                color.a = Mathf.Lerp(startAlpha,targetAlpha,t);

                targetGraphic.color = color;

                yield return null;
            }

            SetAlpha(targetAlpha);

            _fadeRoutine = null;

            onComplete?.Invoke();
        }

        #endregion

#if UNITY_EDITOR
        #region Editor Test

        [ContextMenu("Test/Play Fade")]
        private void TestPlayFade()
        {
            PlayTo(testTargetAlpha);
        }

        [ContextMenu("Test/Reset")]
        private void TestReset()
        {
            ResetEffect();
        }

        [ContextMenu("Test/Set Alpha 1")]
        private void TestSetAlphaOne()
        {
            StopEffect();
            SetAlpha(1f);
        }

        [ContextMenu("Test/Set Alpha 0")]
        private void TestSetAlphaZero()
        {
            StopEffect();
            SetAlpha(0f);
        }

        #endregion
#endif
    }
}
