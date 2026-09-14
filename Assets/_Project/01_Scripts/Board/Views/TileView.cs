using OzGameLab01.Controllers;
using UnityEngine;
using UnityEngine.EventSystems;

namespace OzGameLab01.Map
{
    // 이 스크립트는 모든 타일 프리팹(Normal, Battle, Tree 등)에 부착되어야 합니다.
    // ※ 주의: 프리팹에 Collider가 있어야 하며, 보드 카메라에는 PhysicsRaycaster가 필요합니다.
    [RequireComponent(typeof(Collider))]
    public class TileView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
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

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (BoardPlayerController.Instance == null) return;
            BoardPlayerController.Instance.OnTileHovered(this);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (BoardPlayerController.Instance == null) return;
            BoardPlayerController.Instance.ClearHover();
            ResetHighlight();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;
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
