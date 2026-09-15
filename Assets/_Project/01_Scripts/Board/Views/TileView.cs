// 입력 인터페이스 의존성 전환
using OzGameLab01.Board.Controllers;
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
        private IBoardTileInput _input;

        public void BindInput(IBoardTileInput input)
        {
            _input = input;
        }

        [SerializeField] private MeshRenderer _renderer;
        private Color _originalColor;

        public void Init(MapNode node)
        {
            MyNode = node;
            if (_renderer == null)
            {
                _renderer = GetComponentInChildren<MeshRenderer>();
            }

            if (_renderer != null)
            {
                _originalColor = _renderer.material.color;
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            // 주입된 입력 수신자 연결
            _input?.OnTileHovered(this);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            // 주입된 입력 수신자 연결
            _input?.ClearHover();
            ResetHighlight();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
            {
                return;
            }
            // 주입된 입력 수신자 연결
            _input?.OnTileClicked(this);
        }

        public void SetHighlight(bool isReachable)
        {
            if (_renderer == null)
            {
                return;
            }

            // 이동 가능하면 흰색, 불가능하면 붉은색
            _renderer.material.color = isReachable ? Color.white : Color.red;
        }

        public void ResetHighlight()
        {
            if (_renderer == null)
            {
                return;
            }
            _renderer.material.color = _originalColor;
        }
    }
}
