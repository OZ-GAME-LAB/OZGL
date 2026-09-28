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
        private static readonly int BaseColorPropertyId =
            Shader.PropertyToID("_BaseColor");
        private static readonly int ColorPropertyId =
            Shader.PropertyToID("_Color");

        public MapNode MyNode { get; private set; }
        private IBoardTileInput _input;

        public void BindInput(IBoardTileInput input)
        {
            _input = input;
        }

        [SerializeField] private MeshRenderer _renderer;
        [SerializeField] private SpriteRenderer _spriteRenderer;
        private Color _originalMeshColor;
        private Color _originalSpriteColor;
        private MaterialPropertyBlock _meshPropertyBlock;
        private int _meshColorPropertyId = -1;

        public void Init(MapNode node)
        {
            MyNode = node;
            if (_renderer == null)
            {
                _renderer = GetComponentInChildren<MeshRenderer>();
            }

            if (_renderer != null)
            {
                CacheMeshColorProperty();
            }

            if (_spriteRenderer == null)
            {
                _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            }

            if (_spriteRenderer != null)
            {
                _originalSpriteColor = _spriteRenderer.color;
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
            Color highlightColor = isReachable ? Color.cyan : Color.red;

            if (_renderer != null && _meshColorPropertyId >= 0)
            {
                SetMeshColor(highlightColor);
            }

            if (_spriteRenderer != null)
            {
                _spriteRenderer.color = highlightColor;
            }
        }

        public void ResetHighlight()
        {
            if (_renderer != null && _meshColorPropertyId >= 0)
            {
                SetMeshColor(_originalMeshColor);
            }

            if (_spriteRenderer != null)
            {
                _spriteRenderer.color = _originalSpriteColor;
            }
        }

        private void CacheMeshColorProperty()
        {
            Material sharedMaterial = _renderer.sharedMaterial;
            if (sharedMaterial == null)
            {
                _meshColorPropertyId = -1;
                return;
            }

            if (sharedMaterial.HasProperty(BaseColorPropertyId))
            {
                _meshColorPropertyId = BaseColorPropertyId;
            }
            else if (sharedMaterial.HasProperty(ColorPropertyId))
            {
                _meshColorPropertyId = ColorPropertyId;
            }
            else
            {
                _meshColorPropertyId = -1;
                return;
            }

            _originalMeshColor = sharedMaterial.GetColor(_meshColorPropertyId);
            _meshPropertyBlock ??= new MaterialPropertyBlock();
        }

        private void SetMeshColor(Color color)
        {
            _meshPropertyBlock ??= new MaterialPropertyBlock();
            _renderer.GetPropertyBlock(_meshPropertyBlock);
            _meshPropertyBlock.SetColor(_meshColorPropertyId, color);
            _renderer.SetPropertyBlock(_meshPropertyBlock);
        }
    }
}
