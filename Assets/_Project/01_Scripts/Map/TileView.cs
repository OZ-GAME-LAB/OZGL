using OzGameLab01.Controllers;
using OZGL.Map;
using UnityEngine;

namespace OzGameLab01.Map
{
    // 이 스크립트는 모든 타일 프리팹(Normal, Battle, Tree 등)에 부착되어야 합니다.
    // ※ 주의: 프리팹에 BoxCollider 등 충돌체가 있어야 마우스 이벤트를 감지할 수 있습니다!
    [RequireComponent(typeof(Collider))]
    public class TileView : MonoBehaviour
    {
        public MapNode MyNode { get; private set; }

        [SerializeField] private MeshRenderer _renderer;
        private Color _originalColor;

        public void Init(MapNode node)
        {
            MyNode = node;
            if (_renderer == null) _renderer = GetComponentInChildren<MeshRenderer>();

            if (_renderer != null)
                _originalColor = _renderer.material.color;
        }

        private void OnMouseEnter()
        {
            if (BoardPlayerController.Instance == null) return;
            BoardPlayerController.Instance.OnTileHovered(this);
        }

        private void OnMouseExit()
        {
            if (BoardPlayerController.Instance == null) return;
            BoardPlayerController.Instance.ClearHover();
            ResetHighlight();
        }

        private void OnMouseDown()
        {
            if (BoardPlayerController.Instance == null) return;
            BoardPlayerController.Instance.OnTileClicked(this);
        }

        public void SetHighlight(bool isReachable)
        {
            if (_renderer == null) return;

            // 이동 가능하면 흰색, 불가능하면 붉은색
            _renderer.material.color = isReachable ? Color.white : Color.red;
        }

        public void ResetHighlight()
        {
            if (_renderer == null) return;
            _renderer.material.color = _originalColor;
        }
    }
}