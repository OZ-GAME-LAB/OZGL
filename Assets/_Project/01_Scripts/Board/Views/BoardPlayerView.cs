using System;
using System.Collections;
using UnityEngine;

namespace OzGameLab01.Board.Views
{
    /// <summary>
    /// 기존 플레이어 Transform을 사용하여 토큰 생성과 이동 연출을 담당합니다.
    /// </summary>
    public sealed class BoardPlayerView
    {
        private readonly Transform _transform;
        private GameObject _token;
        private Vector3 _settledPosition;
        private Quaternion _tokenLandingLocalRotation;

        public BoardPlayerView(Transform playerTransform)
        {
            _transform = playerTransform;
            _settledPosition = playerTransform.position;
        }

        public void Setup(Vector3 position, GameObject tokenPrefab)
        {
            _transform.position = position;
            _settledPosition = position;
            if (tokenPrefab != null && _token == null)
            {
                _token = Object.Instantiate(
                    tokenPrefab,
                    position,
                    tokenPrefab.transform.rotation,
                    _transform);
                _token.transform.localPosition = Vector3.zero;
                _tokenLandingLocalRotation = _token.transform.localRotation;
            }
        }

        public IEnumerator PlaySpawnAnimation(
            float dropHeight,
            float duration,
            float rotationSpeed,
            Vector3 rotationAxis,
            Func<bool> skipRequested)
        {
            if (_token == null)
            {
                yield break;
            }

            Transform tokenTransform = _token.transform;
            Vector3 startLocalPosition = Vector3.up * Mathf.Max(0f, dropHeight);
            Vector3 axis = rotationAxis.sqrMagnitude > 0.0001f
                ? rotationAxis.normalized
                : Vector3.right;

            tokenTransform.localPosition = startLocalPosition;
            tokenTransform.localRotation = _tokenLandingLocalRotation;

            float animationDuration = Mathf.Max(0.0001f, duration);
            float elapsed = 0f;
            while (elapsed < animationDuration)
            {
                if (skipRequested != null && skipRequested())
                {
                    yield break;
                }

                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / animationDuration);
                float fallProgress = progress * progress;
                tokenTransform.localPosition = Vector3.LerpUnclamped(
                    startLocalPosition,
                    Vector3.zero,
                    fallProgress);

                Quaternion spinningRotation = _tokenLandingLocalRotation *
                                              Quaternion.AngleAxis(rotationSpeed * elapsed, axis);
                float landingBlend = Mathf.SmoothStep(
                    0f,
                    1f,
                    Mathf.InverseLerp(0.8f, 1f, progress));
                tokenTransform.localRotation = Quaternion.Slerp(
                    spinningRotation,
                    _tokenLandingLocalRotation,
                    landingBlend);

                yield return null;
            }
        }

        public void CompleteSpawnAnimation()
        {
            if (_token == null)
            {
                return;
            }

            _token.transform.localPosition = Vector3.zero;
            _token.transform.localRotation = _tokenLandingLocalRotation;
        }

        public IEnumerator MoveTo(Vector3 target, float speed)
        {
            target.y = 0.5f;
            while (Vector3.Distance(_transform.position, target) > 0.01f)
            {
                _transform.position = Vector3.MoveTowards(_transform.position, target, Mathf.Max(0.01f, speed) * Time.deltaTime);
                yield return null;
            }
            _transform.position = target;
            _settledPosition = target;
        }

        /// <summary>
        /// 연출 중단 시 마지막 도착 위치로 복원합니다.
        /// </summary>
        public void ResetPosition()
        {
            _transform.position = _settledPosition;
        }

        public IEnumerator Shake(float duration, float intensity)
        {
            Vector3 original = _transform.position;
            float elapsed = 0f;
            try
            {
                while (elapsed < duration)
                {
                    _transform.position = original + new Vector3(Mathf.Sin(Time.time * 50f) * intensity, 0f, Mathf.Cos(Time.time * 60f) * intensity);
                    elapsed += Time.deltaTime;
                    yield return null;
                }
            }
            finally
            {
                if (_transform != null)
                {
                    _transform.position = original;
                }
            }
        }
    }
}
