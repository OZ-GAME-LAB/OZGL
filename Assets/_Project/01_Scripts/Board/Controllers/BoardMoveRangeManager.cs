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

        private GameObject _globalDimOverlay;
        private readonly List<GameObject> _activeTileMasks = new List<GameObject>();

        private MapGenerator _mapGenerator;

        private void Awake()
        {
            Instance = this;

            // 현재 씬의 MapGenerator 참조
            _mapGenerator = FindFirstObjectByType<MapGenerator>();

            CreateGlobalDimOverlay();
        }

        /// <summary>
        /// 주사위 UI가 닫히면 이동 가능한 범위를 표시합니다.
        /// </summary>
        private void OnEnable()
        {
            BoardUIController.OnRollViewClosed += TryDrawRange;
        }

        /// <summary>
        /// 이벤트 구독을 해제합니다.
        /// </summary>
        private void OnDisable()
        {
            BoardUIController.OnRollViewClosed -= TryDrawRange;

            ClearMoveRange();
        }

        private void CreateGlobalDimOverlay()
        {
            _globalDimOverlay = GameObject.CreatePrimitive(PrimitiveType.Quad);
            _globalDimOverlay.name = "GlobalDimOverlay";

            Destroy(_globalDimOverlay.GetComponent<Collider>());

            _globalDimOverlay.GetComponent<MeshRenderer>().material =
                stencilReaderMaterial;

            // 바닥보다 살짝 위
            _globalDimOverlay.transform.position =
                new Vector3(10f, 0.51f, 10f);

            _globalDimOverlay.transform.rotation =
                Quaternion.Euler(90f, 0f, 0f);

            _globalDimOverlay.transform.localScale =
                new Vector3(100f, 100f, 1f);

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

            // MapGenerator의 실제 타일 간격을 사용합니다.
            // MapGenerator가 없다면 기본값 2f를 사용합니다.
            float tileSpacing =
                _mapGenerator != null
                    ? _mapGenerator.tileSpacing
                    : 2f;

            foreach (MapNode node in reachableNodes)
            {
                if (node == null)
                {
                    continue;
                }

                GameObject mask =
                    GameObject.CreatePrimitive(PrimitiveType.Quad);

                mask.name = "TileMask";

                Destroy(mask.GetComponent<Collider>());

                mask.GetComponent<MeshRenderer>().material =
                    stencilWriterMaterial;

                mask.transform.position = new Vector3(
                    node.Position.x * tileSpacing,
                    0.51f,
                    node.Position.y * tileSpacing
                );

                mask.transform.rotation =
                    Quaternion.Euler(90f, 0f, 0f);

                mask.transform.localScale =
                    new Vector3(tileSpacing, tileSpacing, 1f);

                _activeTileMasks.Add(mask);
            }

            if (_globalDimOverlay != null)
            {
                _globalDimOverlay.SetActive(true);
            }
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
