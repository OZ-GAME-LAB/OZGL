using System.Collections.Generic;
using UnityEngine;

namespace OZGL.Map
{
    public enum NodeType
    {
        Normal,
        Start,
        Battle,
        Event,
        Shop,
        Elite,
        Boss,
        Tree,
        Rock,
        WaterPuddle,
        WaterStart,
        WaterBody,
        WaterEnd,
        UnitAcquisition // [추가됨] 유닛 획득 타일
    }


    public class MapNode
    {
        public Vector2Int Position;
        public NodeType Type;
        public List<MapNode> ConnectedNodes = new List<MapNode>();
        public GameObject NodeView;
    }
}
