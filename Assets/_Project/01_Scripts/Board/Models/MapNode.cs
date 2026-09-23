using System.Collections.Generic;
using UnityEngine;

namespace OzGameLab01.Map
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
        public bool IsMandatoryStop;
        public List<MapNode> ConnectedNodes = new List<MapNode>();
        // 화면 오브젝트 대응 관계는 MapGenerator에서 관리
    }
}
