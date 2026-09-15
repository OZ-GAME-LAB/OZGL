using UnityEngine;
namespace OzGameLab01.Board.Models
{
    // 카메라 확대 배율 상태
    public sealed class BoardCameraModel
    {
        public float Zoom { get; private set; } = 1f;
        public void ApplyScroll(float scroll, float speed, float minimum, float maximum)
        {
            if (Mathf.Abs(scroll) > 0.01f) { Zoom = Mathf.Clamp(Zoom - Mathf.Sign(scroll) * speed * 0.1f, minimum, maximum); }
        }
    }
}
