using System.Collections.Generic;
using OzGameLab01.Map;
using UnityEngine;
using UnityEngine.Rendering;

namespace OzGameLab01.Controllers
{
    public class BoardMoveRangeController : MonoBehaviour
    {
        public static BoardMoveRangeController Instance { get; private set; }

        [Header("Materials")]
        public Material stencilWriterMaterial;
        public Material stencilReaderMaterial;

        [Header("Layout")]
        [SerializeField, Min(0.001f)] private float surfaceOffset = 0.01f;
        [SerializeField, Min(0f)] private float overlayPadding = 0.5f;

        [Header("Reachable Distance Gradient")]
        [Tooltip("플레이어와 가장 가까운 이동 가능 타일에 추가로 적용할 암전 강도입니다.")]
        [SerializeField, Range(0f, 1f)] private float nearestReachableDimAlpha = 0f;
        [Tooltip("가장 먼 이동 가능 타일에 추가로 적용할 암전 강도입니다.")]
        [SerializeField, Range(0f, 1f)] private float farthestReachableDimAlpha = 0.55f;
        [Tooltip("가장 먼 이동 가능 타일과 이동 불가능 영역 사이에 보장할 최소 밝기 차이입니다.")]
        [SerializeField, Range(0f, 1f)] private float minimumUnavailableAlphaGap = 0.05f;

        private GameObject _globalDimOverlay;
        private readonly List<GameObject> _activeTileMasks = new List<GameObject>();
        private Material _reachableDimMaterial;

        private MapGenerator _mapGenerator;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                // 중복 컴포넌트 때문에 Map처럼 다른 책임을 가진 오브젝트 전체가
                // 삭제되지 않도록 이 컴포넌트만 제거합니다.
                Destroy(this);
                return;
            }

            Instance = this;
            ResolveMapGenerator();
            CreateGlobalDimOverlay();
        }

        /// <summary>
        /// 주사위 UI가 닫히면 이동 가능한 범위를 표시합니다.
        /// </summary>
        private void OnEnable()
        {
            BoardUIController.OnRollViewClosed += TryDrawRange;
            BoardPlayerController.OnPlayerFinishedMoving += TryDrawRange;
            BoardPlayerController.OnPlayerStartedMoving += ClearMoveRange;
            BoardPlayerController.OnPlayerSetupCompleted += TryDrawRange;
        }

        /// <summary>
        /// 이벤트 구독을 해제합니다.
        /// </summary>
        private void OnDisable()
        {
            BoardUIController.OnRollViewClosed -= TryDrawRange;
            BoardPlayerController.OnPlayerFinishedMoving -= TryDrawRange;
            BoardPlayerController.OnPlayerStartedMoving -= ClearMoveRange;
            BoardPlayerController.OnPlayerSetupCompleted -= TryDrawRange;

            ClearMoveRange();
        }

        private void CreateGlobalDimOverlay()
        {
            _globalDimOverlay = GameObject.CreatePrimitive(PrimitiveType.Quad);
            _globalDimOverlay.name = "GlobalDimOverlay";

            Destroy(_globalDimOverlay.GetComponent<Collider>());

            _globalDimOverlay.GetComponent<MeshRenderer>().sharedMaterial = stencilReaderMaterial;
            _globalDimOverlay.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            _globalDimOverlay.transform.localScale = Vector3.one;

            _globalDimOverlay.SetActive(false);
        }

        /// <summary>
        /// 현재 플레이어가 이동할 수 있는 타일 범위를 가져와 표시합니다.
        /// </summary>
        private void TryDrawRange()
        {
            BoardPlayerController player = BoardPlayerController.Instance;

            // 플레이어가 없거나
            // 행동력이 없거나
            // 현재 이동 중이면 범위를 표시하지 않습니다.
            if (player == null ||
                player.CurrentDiceValue <= 0 ||
                player.IsMoving)
            {
                ClearMoveRange();
                return;
            }

            // MapManager를 사용하지 않고
            // BoardPlayerController -> BoardMovementModel -> BoardPathfinder를 통해
            // 이동 가능한 노드를 가져옵니다.
            IReadOnlyDictionary<MapNode, int> reachableNodes =
                player.GetReachableNodeDistances();

            if (reachableNodes == null)
            {
                ClearMoveRange();
                return;
            }

            DrawMoveRange(reachableNodes, player.CurrentNode);
        }

        /// <summary>
        /// 현재 위치와 이동 가능한 노드에는 스텐실 마스크를 만들고,
        /// 이동 거리에 따라 단계적으로 암전 강도를 높입니다.
        /// </summary>
        private void DrawMoveRange(
            IReadOnlyDictionary<MapNode, int> reachableNodes,
            MapNode currentNode)
        {
            ClearMoveRange();

            if (reachableNodes == null || currentNode == null ||
                stencilWriterMaterial == null || stencilReaderMaterial == null)
            {
                return;
            }

            ResolveMapGenerator();
            List<MapNode> visibleNodes = new List<MapNode>(reachableNodes.Count + 1)
            {
                currentNode
            };

            int maximumReachableDistance = 0;
            foreach (KeyValuePair<MapNode, int> pair in reachableNodes)
            {
                if (pair.Key == null || pair.Key == currentNode)
                {
                    continue;
                }

                visibleNodes.Add(pair.Key);
                maximumReachableDistance = Mathf.Max(maximumReachableDistance, pair.Value);
            }

            CalculateBoardLayout(visibleNodes, out Bounds boardBounds, out float drawingPlaneY);

            foreach (MapNode node in visibleNodes)
            {
                CreateTileOverlay(node, drawingPlaneY, stencilWriterMaterial, 0f, "TileMask");
            }

            if (maximumReachableDistance > 0)
            {
                Material reachableMaterial = GetReachableDimMaterial();
                foreach (KeyValuePair<MapNode, int> pair in reachableNodes)
                {
                    if (pair.Key == null || pair.Key == currentNode || pair.Value <= 0)
                    {
                        continue;
                    }

                    float dimAlpha = CalculateReachableDimAlpha(
                        pair.Value,
                        maximumReachableDistance);
                    if (dimAlpha > 0.001f)
                    {
                        CreateTileOverlay(
                            pair.Key,
                            drawingPlaneY,
                            reachableMaterial,
                            dimAlpha,
                            "ReachableGradient");
                    }
                }
            }

            // 마스크와 오버레이를 같은 평면에 두어 기울어진 카메라에서도
            // 스텐실 구멍과 실제 타일 위치가 어긋나지 않게 합니다.
            ConfigureGlobalOverlay(boardBounds, drawingPlaneY);
        }

        private void CreateTileOverlay(
            MapNode node,
            float drawingPlaneY,
            Material material,
            float colorAlpha,
            string objectName)
        {
            ResolveTileFootprint(node, out Vector3 tileCenter, out Vector2 tileSize, out _);

            GameObject overlay = GameObject.CreatePrimitive(PrimitiveType.Quad);
            overlay.name = $"{objectName}_{node.Position.x}_{node.Position.y}";
            Destroy(overlay.GetComponent<Collider>());

            MeshRenderer renderer = overlay.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            if (colorAlpha > 0f)
            {
                Color color = stencilReaderMaterial.HasProperty("_Color")
                    ? stencilReaderMaterial.GetColor("_Color")
                    : Color.black;
                color.a = colorAlpha;
                MaterialPropertyBlock properties = new MaterialPropertyBlock();
                properties.SetColor("_Color", color);
                renderer.SetPropertyBlock(properties);
            }

            overlay.transform.position = new Vector3(tileCenter.x, drawingPlaneY, tileCenter.z);
            overlay.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            overlay.transform.localScale = new Vector3(tileSize.x, tileSize.y, 1f);
            _activeTileMasks.Add(overlay);
        }

        private Material GetReachableDimMaterial()
        {
            if (_reachableDimMaterial != null)
            {
                return _reachableDimMaterial;
            }

            int baseRenderQueue = stencilReaderMaterial.renderQueue;
            if (baseRenderQueue < 0 && stencilReaderMaterial.shader != null)
            {
                baseRenderQueue = stencilReaderMaterial.shader.renderQueue;
            }

            _reachableDimMaterial = new Material(stencilReaderMaterial)
            {
                name = "MoveRangeReachableGradient",
                renderQueue = Mathf.Max(0, baseRenderQueue) + 1
            };
            _reachableDimMaterial.SetFloat("_StencilComp", (float)CompareFunction.Always);
            return _reachableDimMaterial;
        }

        private float CalculateReachableDimAlpha(int distance, int maximumDistance)
        {
            float unavailableAlpha = stencilReaderMaterial.HasProperty("_Color")
                ? stencilReaderMaterial.GetColor("_Color").a
                : 0.7f;
            float maximumReachableAlpha = Mathf.Min(
                farthestReachableDimAlpha,
                Mathf.Max(0f, unavailableAlpha - minimumUnavailableAlphaGap));
            float minimumReachableAlpha = Mathf.Min(
                nearestReachableDimAlpha,
                maximumReachableAlpha);

            if (maximumDistance <= 1)
            {
                return minimumReachableAlpha;
            }

            float gradient = Mathf.InverseLerp(1f, maximumDistance, distance);
            return Mathf.Lerp(minimumReachableAlpha, maximumReachableAlpha, gradient);
        }

        private void ResolveMapGenerator()
        {
            if (_mapGenerator == null)
            {
                _mapGenerator = FindFirstObjectByType<MapGenerator>();
            }
        }

        private void CalculateBoardLayout(
            IReadOnlyList<MapNode> reachableNodes,
            out Bounds boardBounds,
            out float drawingPlaneY)
        {
            bool hasBounds = false;
            boardBounds = default;
            float highestSurface = 0f;

            IEnumerable<MapNode> nodes = _mapGenerator != null && _mapGenerator.NodeDict.Count > 0
                ? _mapGenerator.NodeDict.Values
                : reachableNodes;

            foreach (MapNode node in nodes)
            {
                ResolveTileFootprint(node, out Vector3 center, out Vector2 size, out float surfaceY);
                Vector3 min = new Vector3(center.x - size.x * 0.5f, 0f, center.z - size.y * 0.5f);
                Vector3 max = new Vector3(center.x + size.x * 0.5f, 0f, center.z + size.y * 0.5f);

                if (!hasBounds)
                {
                    boardBounds = new Bounds();
                    boardBounds.SetMinMax(min, max);
                    highestSurface = surfaceY;
                    hasBounds = true;
                    continue;
                }

                boardBounds.Encapsulate(min);
                boardBounds.Encapsulate(max);
                highestSurface = Mathf.Max(highestSurface, surfaceY);
            }

            if (!hasBounds && _mapGenerator != null)
            {
                boardBounds = _mapGenerator.GeneratedWorldBounds;
                highestSurface = _mapGenerator.transform.position.y;
            }

            drawingPlaneY = highestSurface + surfaceOffset;
        }

        private void ResolveTileFootprint(
            MapNode node,
            out Vector3 center,
            out Vector2 size,
            out float surfaceY)
        {
            float spacing = _mapGenerator != null
                ? Mathf.Max(0.01f, Mathf.Abs(_mapGenerator.tileSpacing))
                : 2f;

            center = new Vector3(node.Position.x * spacing, 0f, node.Position.y * spacing);
            size = new Vector2(spacing, spacing);
            surfaceY = center.y;

            GameObject nodeView = _mapGenerator != null ? _mapGenerator.GetNodeView(node) : null;
            if (nodeView == null)
            {
                return;
            }

            center = nodeView.transform.position;

            if (!TryGetTileBounds(nodeView, out Bounds tileBounds))
            {
                return;
            }

            center = tileBounds.center;
            surfaceY = tileBounds.max.y;

            float width = Mathf.Abs(tileBounds.size.x);
            float depth = Mathf.Abs(tileBounds.size.z);
            size = new Vector2(
                width > 0.001f ? width : spacing,
                depth > 0.001f ? depth : spacing);
        }

        private static bool TryGetTileBounds(GameObject nodeView, out Bounds bounds)
        {
            Collider rootCollider = nodeView.GetComponent<Collider>();
            if (rootCollider != null && rootCollider.enabled)
            {
                bounds = rootCollider.bounds;
                return true;
            }

            Renderer rootRenderer = nodeView.GetComponent<Renderer>();
            if (rootRenderer != null && rootRenderer.enabled)
            {
                bounds = rootRenderer.bounds;
                return true;
            }

            Collider childCollider = nodeView.GetComponentInChildren<Collider>();
            if (childCollider != null && childCollider.enabled)
            {
                bounds = childCollider.bounds;
                return true;
            }

            Renderer childRenderer = nodeView.GetComponentInChildren<Renderer>();
            if (childRenderer != null && childRenderer.enabled)
            {
                bounds = childRenderer.bounds;
                return true;
            }

            bounds = default;
            return false;
        }

        private void ConfigureGlobalOverlay(Bounds boardBounds, float overlayY)
        {
            if (_globalDimOverlay == null)
            {
                CreateGlobalDimOverlay();
            }

            float spacing = _mapGenerator != null
                ? Mathf.Max(0.01f, Mathf.Abs(_mapGenerator.tileSpacing))
                : 1f;
            float padding = Mathf.Max(overlayPadding, spacing * 0.5f);

            _globalDimOverlay.transform.position = new Vector3(
                boardBounds.center.x,
                overlayY,
                boardBounds.center.z);
            _globalDimOverlay.transform.localScale = new Vector3(
                Mathf.Max(spacing, boardBounds.size.x + padding * 2f),
                Mathf.Max(spacing, boardBounds.size.z + padding * 2f),
                1f);
            _globalDimOverlay.SetActive(true);
        }

        /// <summary>
        /// 현재 표시 중인 이동 범위를 제거합니다.
        /// </summary>
        private void ClearMoveRange()
        {
            if (_globalDimOverlay != null)
            {
                _globalDimOverlay.SetActive(false);
            }

            foreach (GameObject mask in _activeTileMasks)
            {
                if (mask != null)
                {
                    Destroy(mask);
                }
            }

            _activeTileMasks.Clear();
        }

        private void OnDestroy()
        {
            if (_reachableDimMaterial != null)
            {
                Destroy(_reachableDimMaterial);
                _reachableDimMaterial = null;
            }

            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}
