using OzGameLab01.Map;
namespace OzGameLab01.Board.Models
{
    // 타일 재진입 및 일회성 소비 규칙
    public static class BoardTileRules
    {
        public static bool IsSingleUse(NodeType type)
        {
            return type == NodeType.Battle || type == NodeType.Event || type == NodeType.Shop || type == NodeType.Elite || type == NodeType.Boss || type == NodeType.UnitAcquisition;
        }
        public static bool CanProcess(MapNode node, bool consumed)
        {
            return node != null && !consumed;
        }
    }
}
