using System.Collections.Generic;
using OzGameLab01.Controllers;
using OzGameLab01.Map;
using UnityEngine;

namespace OzGameLab01.Managers
{
    public class BoardMoveRangeManager : MonoBehaviour
    {
        public static BoardMoveRangeManager Instance { get; private set; }

        [Header("Materials")]
        public Material stencilWriterMaterial;
        public Material stencilReaderMaterial;

        [Header("Layout")]
        [SerializeField, Min(0.001f)] private float surfaceOffset = 0.01f;
        [SerializeField, Min(0f)] private float overlayPadding = 0.5f;

        private GameObject _globalDimOverlay;
        private readonly List<GameObject> _activeTileMasks = new List<GameObject>();

        private MapGenerator _mapGenerator;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
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
        }

        /// <summary>
        /// 이벤트 구독을 해제합니다.
        /// </summary>
        private void OnDisable()
        {
            BoardUIController.OnRollViewClosed -= TryDrawRange;
            BoardPlayerController.OnPlayerFinishedMoving -= TryDrawRange;
            BoardPlayerController.OnPlayerStartedMoving -= ClearMoveRange;

            ClearMoveRange();
        }

        private void CreateGlobalDimOverlay()
        {
            _globalDimOverlay = GameObject.CreatePrimitive(PrimitiveType.Quad);
            _globalDimOverlay.name = "GlobalDimOverlay";

            Destroy(_globalDimOverlay.GetComponent<Collider>());

            _globalDimOverlay.GetComponent<MeshRenderer>().material = stencilReaderMaterial;
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
            IReadOnlyList<MapNode> reachableNodes =
                player.GetReachableNodes();

            if (reachableNodes == null || reachableNodes.Count == 0)
            {
                ClearMoveRange();
                return;
            }

            DrawMoveRange(reachableNodes);
        }

        /// <summary>
        /// 이동 가능한 노드 위치에 마스크를 생성합니다.
        /// </summary>
        private void DrawMoveRange(IReadOnlyList<MapNode> reachableNodes)
        {
            ClearMoveRange();

            if (reachableNodes == null || reachableNodes.Count == 0 ||
                stencilWriterMaterial == null || stencilReaderMaterial == null)
            {
                return;
            }

            ResolveMapGenerator();
            CalculateBoardLayout(reachableNodes, out Bounds boardBounds, out float drawingPlaneY);

            foreach (MapNode node in reachableNodes)
            {
                if (node == null)
                {
                    continue;
                }

                ResolveTileFootprint(node, out Vector3 tileCenter, out Vector2 tileSize, out _);

                GameObject mask = GameObject.CreatePrimitive(PrimitiveType.Quad);
                mask.name = $"TileMask_{node.Position.x}_{node.Position.y}";

                Destroy(mask.GetComponent<Collider>());
                mask.GetComponent<MeshRenderer>().material = stencilWriterMaterial;

                mask.transform.position = new Vector3(tileCenter.x, drawingPlaneY, tileCenter.z);
                mask.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                mask.transform.localScale = new Vector3(tileSize.x, tileSize.y, 1f);

                _activeTileMasks.Add(mask);
            }

            ConfigureGlobalOverlay(boardBounds, drawingPlaneY + surfaceOffset);
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
                : 1f;

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
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}
