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
        // 0이면 NodeType/낮밤 규칙으로 적을 결정하고, 양수면 해당 MonsterData ID를 사용합니다.
        public int EncounterMonsterId;
        public bool IsMandatoryStop;
        public List<MapNode> ConnectedNodes = new List<MapNode>();
        // 화면 오브젝트 대응 관계는 MapGenerator에서 관리
    }
}
