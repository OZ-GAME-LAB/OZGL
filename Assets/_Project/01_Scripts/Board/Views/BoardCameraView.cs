using System.Collections;
using UnityEngine;
namespace OzGameLab01.Board.Views
{
    // 카메라 위치 보간 및 목표 이동 연출
    public sealed class BoardCameraView
    {
        private readonly Transform _camera;
        public BoardCameraView(Transform camera) { _camera = camera; }
        public void Follow(Vector3 targetPosition, Vector3 offset, float zoom, float speed, float deltaTime)
        {
            _camera.position = Vector3.Lerp(_camera.position, targetPosition + offset * zoom, speed * deltaTime);
        }
        public IEnumerator MoveTo(Vector3 targetPosition, Vector3 offset, float zoom, float moveDuration)
        {
            Vector3 start = _camera.position;
            Vector3 destination = targetPosition + offset * zoom;
            float elapsed = 0f;
            float duration = Mathf.Max(0.01f, moveDuration);
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                _camera.position = Vector3.Lerp(start, destination, progress);
                yield return null;
            }
            _camera.position = destination;
        }
    }
}
