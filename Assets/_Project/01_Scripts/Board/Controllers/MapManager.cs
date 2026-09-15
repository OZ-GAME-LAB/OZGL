using System.Collections.Generic;
using UnityEngine;
using OzGameLab01.Board.Models;

namespace OzGameLab01.Map
{
    /// <summary>
    /// 기존 맵 조회 API를 유지하고 경로 계산을 독립 모델에 위임합니다.
    /// </summary>
    public class MapManager : MonoBehaviour
    {
        public static MapManager Instance { get; private set; }
        private Dictionary<Vector2Int, MapNode> _nodeDict = new Dictionary<Vector2Int, MapNode>();

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void InitializeMapData(Dictionary<Vector2Int, MapNode> generatedNodes)
        {
            _nodeDict = generatedNodes ?? new Dictionary<Vector2Int, MapNode>();
        }

        public MapNode GetNodeAt(Vector2Int position)
        {
            _nodeDict.TryGetValue(position, out MapNode node);
            return node;
        }

        public bool IsObstacle(NodeType type)
        {
            return BoardPathfinder.IsObstacle(type);
        }

        public List<MapNode> FindPath(MapNode startNode, MapNode targetNode, int maxDistance)
        {
            return BoardPathfinder.FindPath(startNode, targetNode, maxDistance);
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