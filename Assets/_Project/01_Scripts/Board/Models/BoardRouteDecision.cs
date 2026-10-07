using OzGameLab01.Map;
namespace OzGameLab01.Board.Models
{
    // 목표 선정 결과 상태
    public enum BoardRouteStatus { Ready, MissingStart, MissingBoss, MissingTarget }
    public readonly struct BoardRouteDecision
    {
        public BoardRouteStatus Status { get; }
        public MapNode Boss { get; }
        public MapNode Target { get; }
        public NodeType Type { get; }
        public BoardRouteDecision(BoardRouteStatus status, MapNode boss = null, MapNode target = null, NodeType type = NodeType.Normal)
        {
            Status = status; Boss = boss; Target = target; Type = type;
        }
    }
}
