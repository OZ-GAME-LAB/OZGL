using System;
using System.Collections;
using UnityEngine;

namespace OzGameLab01.UI
{
    [DisallowMultipleComponent]
    public sealed class UITransformEffect : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private RectTransform target;

        [Header("Hover")]
        [SerializeField, Min(0.01f)] private float hoverScaleDuration = 0.15f;
        [SerializeField, Min(1f)] private float hoverScale = 1.05f;
        [SerializeField, Min(0.1f)] private float hoverTiltSpeed = 4f;
        [SerializeField, Range(0f, 10f)] private float hoverTiltAngle = 2f;

        [Header("Selected")]
        [SerializeField, Min(1f)] private float selectedScale = 1.12f;
        [SerializeField, Min(0.01f)] private float selectedScaleDuration = 0.18f;
        [SerializeField, Min(0f)] private float selectedHoldDuration = 0.12f;

        private Coroutine _hoverRoutine;
        private Coroutine _transformRoutine;

        #region Unity Lifecycle

        private void Awake()
        {
            ResetEffect();
        }

        private void OnDisable()
        {
            StopAllEffects();
        }

        #endregion

        #region Public API

        /// <summary>
        /// Hover 연출을 시작합니다.
        /// 대상 UI를 확대한 뒤 Z축 기준으로 좌우 흔들림을 반복합니다.
        /// </summary>
        public void PlayHover()
        {
            StopAllEffects();

            _hoverRoutine = StartCoroutine(PlayHoverRoutine());
        }

        /// <summary>
        /// Hover 연출을 종료하고 대상 UI의 Scale과 Rotation을 기본 상태로 복구합니다.
        /// </summary>
        public Coroutine StopHover(Action onComplete = null)
        {
            if (_hoverRoutine != null)
            {
                StopCoroutine(_hoverRoutine);
                _hoverRoutine = null;
            }

            if (_transformRoutine != null)
            {
                StopCoroutine(_transformRoutine);
            }

            _transformRoutine = StartCoroutine(RestoreRoutine(onComplete));

            return _transformRoutine;
        }

        /// <summary>
        /// 선택 강조 연출을 실행합니다.
        /// 현재 상태에서 선택용 Scale까지 확대하고 회전을 중앙으로 복구한 뒤 잠시 유지합니다.
        /// </summary>
        /// <param name="onComplete">
        /// 선택 강조 연출이 종료된 후 호출되는 단발성 콜백입니다.
        /// </param>
        /// <returns>현재 실행 중인 선택 강조 Coroutine입니다.</returns>
        public Coroutine PlaySelected(
            Action onComplete = null)
        {
            StopAllEffects();

            _transformRoutine = StartCoroutine(PlaySelectedRoutine(onComplete));

            return _transformRoutine;
        }

        /// <summary>
        /// 현재 실행 중인 Transform 연출을 모두 중단합니다.
        /// 현재 Scale과 Rotation 상태는 유지됩니다.
        /// </summary>
        public void StopAllEffects()
        {
            if (_hoverRoutine != null)
            {
                StopCoroutine(_hoverRoutine);
                _hoverRoutine = null;
            }

            if (_transformRoutine != null)
            {
                StopCoroutine(_transformRoutine);
                _transformRoutine = null;
            }
        }

        /// <summary>
        /// 모든 Transform 연출을 중단하고 대상 UI의 Scale과 Rotation을 기본 상태로 즉시 초기화합니다.
        /// </summary>
        public void ResetEffect()
        {
            StopAllEffects();

            if (target == null)
            {
                return;
            }

            target.localScale = Vector3.one;
            target.localRotation = Quaternion.identity;
        }

        #endregion

        #region Hover Effect

        private IEnumerator PlayHoverRoutine()
        {
            if (target == null)
            {
                _hoverRoutine = null;
                yield break;
            }

            Vector3 startScale = target.localScale;
            Vector3 targetScale = Vector3.one * hoverScale;

            float elapsed = 0f;

            while (elapsed < hoverScaleDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / hoverScaleDuration);
                float easedT = 1f - Mathf.Pow(1f - t, 3f);

                target.localScale = Vector3.Lerp(startScale,targetScale,easedT);

                yield return null;
            }

            target.localScale = targetScale;

            elapsed = 0f;

            while (true)
            {
                elapsed += Time.unscaledDeltaTime;
                float angle = Mathf.Sin(elapsed * hoverTiltSpeed) * hoverTiltAngle;
                target.localRotation = Quaternion.Euler(0f,0f,angle);

                yield return null;
            }
        }

        #endregion

        #region Selected Effect

        private IEnumerator PlaySelectedRoutine(Action onComplete)
        {
            if (target == null)
            {
                _transformRoutine = null;

                onComplete?.Invoke();
                yield break;
            }

            Vector3 startScale = target.localScale;
            Quaternion startRotation = target.localRotation;

            Vector3 targetScale = Vector3.one * selectedScale;
            float elapsed = 0f;

            while (elapsed < selectedScaleDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / selectedScaleDuration);
                float easedT = 1f - Mathf.Pow(1f - t, 3f);

                target.localScale = Vector3.Lerp(startScale,targetScale,easedT);
                target.localRotation = Quaternion.Lerp(startRotation,Quaternion.identity,easedT);

                yield return null;
            }

            target.localScale = targetScale;
            target.localRotation = Quaternion.identity;

            if (selectedHoldDuration > 0f)
            {
                yield return new WaitForSecondsRealtime(selectedHoldDuration);
            }

            _transformRoutine = null;

            onComplete?.Invoke();
        }

        #endregion

        #region Restore Effect

        private IEnumerator RestoreRoutine(Action onComplete)
        {
            if (target == null)
            {
                _transformRoutine = null;

                onComplete?.Invoke();
                yield break;
            }

            Vector3 startScale = target.localScale;
            Quaternion startRotation = target.localRotation;

            float elapsed = 0f;

            while (elapsed < hoverScaleDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / hoverScaleDuration);
                float easedT = 1f - Mathf.Pow(1f - t, 3f);

                target.localScale = Vector3.Lerp(startScale,Vector3.one,easedT);
                target.localRotation = Quaternion.Lerp(startRotation,Quaternion.identity,easedT);

                yield return null;
            }

            target.localScale = Vector3.one;
            target.localRotation = Quaternion.identity;

            _transformRoutine = null;

            onComplete?.Invoke();
        }

        #endregion

#if UNITY_EDITOR
        #region Editor Test

        [ContextMenu("Test/Play Hover")]
        private void TestPlayHover()
        {
            ResetEffect();
            PlayHover();
        }

        [ContextMenu("Test/Stop Hover")]
        private void TestStopHover()
        {
            StopHover();
        }

        [ContextMenu("Test/Play Selected")]
        private void TestPlaySelected()
        {
            ResetEffect();
            PlaySelected();
        }

        [ContextMenu("Test/Reset")]
        private void TestReset()
        {
            ResetEffect();
        }

        #endregion
#endif
    }
}
