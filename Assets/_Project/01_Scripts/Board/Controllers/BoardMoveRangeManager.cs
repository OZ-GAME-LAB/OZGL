using UnityEngine;
using System;
using System.Collections.Generic;
using OzGameLab01.Map;
using OzGameLab01.Controllers;

public class BoardMoveRangeManager : MonoBehaviour
{
    public static BoardMoveRangeManager Instance { get; private set; }

    [Header("Materials")]
    public Material stencilWriterMaterial;
    public Material stencilReaderMaterial;

    private GameObject _globalDimOverlay;
    private List<GameObject> _activeTileMasks = new List<GameObject>();

    private void Awake()
    {
        Instance = this;
        CreateGlobalDimOverlay();
    }

    // 💡 방송 구독 시작 (OnEnable)
    private void OnEnable()
    {
        BoardUIController.OnRollViewClosed += TryDrawRange;
        BoardPlayerController.OnPlayerFinishedMoving += TryDrawRange;

        BoardPlayerController.OnPlayerStartedMoving += ClearMoveRange;
    }

    // 💡 방송 구독 해제 (OnDisable - 메모리 누수 방지용 필수)
    private void OnDisable()
    {
        BoardUIController.OnRollViewClosed -= TryDrawRange;
        BoardPlayerController.OnPlayerFinishedMoving -= TryDrawRange;

        BoardPlayerController.OnPlayerStartedMoving -= ClearMoveRange;
    }

    private void CreateGlobalDimOverlay()
    {
        _globalDimOverlay = GameObject.CreatePrimitive(PrimitiveType.Quad);
        _globalDimOverlay.name = "GlobalDimOverlay";
        Destroy(_globalDimOverlay.GetComponent<Collider>());

        _globalDimOverlay.GetComponent<MeshRenderer>().material = stencilReaderMaterial;
        _globalDimOverlay.transform.position = new Vector3(10f, 0.51f, 10f); // 바닥보다 살짝 위
        _globalDimOverlay.transform.rotation = Quaternion.Euler(90, 0, 0);
        _globalDimOverlay.transform.localScale = new Vector3(100f, 100f, 1f);

        _globalDimOverlay.SetActive(false);
    }

    private void TryDrawRange()
    {
        var player = BoardPlayerController.Instance;
        // 플레이어가 없거나, 주사위(행동력)가 0이거나, 걷고 있다면 그리지 않음
        if (player == null || player.CurrentDiceValue <= 0 || player.IsMoving)
        {
            ClearMoveRange();
            return;
        }

        // 맵 매니저에서 닿을 수 있는 노드를 가져옴
        var reachable = MapManager.Instance.GetReachableNodes(player.CurrentNode, player.CurrentDiceValue);
        DrawMoveRange(reachable);
    }

    private void DrawMoveRange(HashSet<MapNode> reachableNodes)
    {
        ClearMoveRange();

        foreach (var node in reachableNodes)
        {
            GameObject mask = GameObject.CreatePrimitive(PrimitiveType.Quad);
            mask.name = "TileMask";
            Destroy(mask.GetComponent<Collider>());
            mask.GetComponent<MeshRenderer>().material = stencilWriterMaterial;

            // 좌표를 2배수로 잡은 이유는 MapGenerator의 tileSpacing이 보통 2f이기 때문입니다. 
            // 프로젝트에 맞게 간격을 수정해주세요.
            mask.transform.position = new Vector3(node.Position.x * 2f, 0.51f, node.Position.y * 2f);
            mask.transform.rotation = Quaternion.Euler(90, 0, 0);
            mask.transform.localScale = new Vector3(2f, 2f, 1f);

            _activeTileMasks.Add(mask);
        }

        _globalDimOverlay.SetActive(true);
    }

    private void ClearMoveRange()
    {
        _globalDimOverlay.SetActive(false);

        foreach (var mask in _activeTileMasks)
        {
            Destroy(mask);
        }
        _activeTileMasks.Clear();
    }
}