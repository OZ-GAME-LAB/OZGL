using UnityEngine;

namespace OzGameLab01.Board.Views
{
    /// <summary>
    /// 생성된 보드 전체 위에 시간대 색을 곱하는 독립 오버레이입니다.
    /// 이동 범위 스텐실보다 먼저 그려지므로 두 효과를 동시에 사용할 수 있습니다.
    /// </summary>
    public sealed class BoardTimeOfDayOverlayView
    {
        private const string OverlayName = "Board Time Of Day Overlay";
        private static readonly int TintId = Shader.PropertyToID("_Tint");

        private readonly Material _sourceMaterial;
        private GameObject _overlay;
        private Material _runtimeMaterial;

        public BoardTimeOfDayOverlayView(Material sourceMaterial)
        {
            _sourceMaterial = sourceMaterial;
        }

        public void Show(Bounds boardBounds, float height, float padding, Color tint)
        {
            if (_sourceMaterial == null || boardBounds.size.x <= 0f || boardBounds.size.z <= 0f)
            {
                return;
            }

            EnsureOverlay();
            if (_overlay == null)
            {
                return;
            }

            float paddedWidth = boardBounds.size.x + Mathf.Max(0f, padding) * 2f;
            float paddedDepth = boardBounds.size.z + Mathf.Max(0f, padding) * 2f;

            _overlay.transform.SetPositionAndRotation(
                new Vector3(boardBounds.center.x, boardBounds.max.y + height, boardBounds.center.z),
                Quaternion.Euler(90f, 0f, 0f));
            _overlay.transform.localScale = new Vector3(paddedWidth, paddedDepth, 1f);
            _runtimeMaterial.SetColor(TintId, tint);
            _overlay.SetActive(true);
        }

        public void Dispose()
        {
            if (_overlay != null)
            {
                UnityEngine.Object.Destroy(_overlay);
                _overlay = null;
            }

            if (_runtimeMaterial != null)
            {
                UnityEngine.Object.Destroy(_runtimeMaterial);
                _runtimeMaterial = null;
            }
        }

        private void EnsureOverlay()
        {
            if (_overlay != null)
            {
                return;
            }

            _overlay = GameObject.CreatePrimitive(PrimitiveType.Quad);
            _overlay.name = OverlayName;

            Collider collider = _overlay.GetComponent<Collider>();
            if (collider != null)
            {
                UnityEngine.Object.Destroy(collider);
            }

            _runtimeMaterial = new Material(_sourceMaterial)
            {
                name = $"{_sourceMaterial.name} (Runtime)"
            };

            MeshRenderer renderer = _overlay.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = _runtimeMaterial;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }
    }
}
