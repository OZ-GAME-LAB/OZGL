using UnityEngine;

namespace OzGameLab01.Board.Views
{
    /// <summary>
    /// 전달받은 위치를 기준으로 목표 방향 바늘을 표시합니다.
    /// </summary>
    public sealed class BoardObjectivePointerView
    {
        private readonly Transform _root;
        private readonly Transform _needle;
        private Quaternion _currentHeading;
        private bool _hasHeading;

        public BoardObjectivePointerView(Transform root, Transform needle)
        {
            _root = root;
            _needle = needle;
        }

        public bool HasValidNeedle => _root != null && _needle != null && _needle != _root && _needle.IsChildOf(_root);

        public void Show(Vector3 playerPosition, Vector3 objectivePosition, float distanceFromPlayer, float heightOffset, float rotationSpeed, float needleYawOffset, float hideDistance, float deltaTime)
        {
            if (!HasValidNeedle)
            {
                Hide();
                return;
            }
            Vector3 direction = objectivePosition - playerPosition;
            direction.y = 0f;
            float threshold = Mathf.Max(0.001f, hideDistance);
            if (direction.sqrMagnitude <= threshold * threshold)
            {
                Hide();
                return;
            }
            Quaternion desiredHeading = Quaternion.LookRotation(direction, Vector3.up);
            _currentHeading = !_hasHeading || rotationSpeed <= 0f ? desiredHeading : Quaternion.RotateTowards(_currentHeading, desiredHeading, rotationSpeed * deltaTime);
            _hasHeading = true;
            _root.position = playerPosition + Vector3.up * heightOffset;
            _needle.position = _root.position + _currentHeading * Vector3.forward * Mathf.Max(0f, distanceFromPlayer);
            // 기존 스프라이트의 X축 90도 회전 및 Y축 보정 유지
            _needle.rotation = _currentHeading * Quaternion.Euler(0f, needleYawOffset, 0f) * Quaternion.Euler(90f, 0f, 0f);
            SetVisible(true);
        }

        public void Hide()
        {
            _hasHeading = false;
            SetVisible(false);
        }

        private void SetVisible(bool visible)
        {
            if (HasValidNeedle && _needle.gameObject.activeSelf != visible)
            {
                _needle.gameObject.SetActive(visible);
            }
        }
    }
}
