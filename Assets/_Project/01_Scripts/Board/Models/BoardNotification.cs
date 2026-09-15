using OzGameLab01.Map;
using UnityEngine;
namespace OzGameLab01.Board.Models
{
    // 공용 이벤트 버스 연결 전 보드 알림 종류
    public enum BoardNotificationKind { PlayerArrived, TurnAdvanced, BattleRequested, SpecialTileConsumed, UnitGranted }

    // 씬 오브젝트 참조가 없는 발행 시점 값
    public readonly struct BoardNotification
    {
        public BoardNotificationKind Kind { get; }
        public Vector2Int Position { get; }
        public NodeType TileType { get; }
        public int TurnCount { get; }
        public int RemainingDice { get; }
        public int UnitId { get; }
        public int UnusedActionPoints { get; }
        public bool IsNight { get; }
        public BoardNotification(BoardNotificationKind kind, Vector2Int position, NodeType tileType, int turnCount, int remainingDice, int unitId = 0, int unusedActionPoints = 0, bool isNight = false)
        {
            Kind = kind; Position = position; TileType = tileType; TurnCount = turnCount; RemainingDice = remainingDice; UnitId = unitId; UnusedActionPoints = unusedActionPoints; IsNight = isNight;
        }
    }
}
