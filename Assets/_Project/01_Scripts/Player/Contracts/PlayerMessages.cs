using OzGameLab01.Data;

namespace OzGameLab01.Player.Contracts
{
    /// <summary>새 유닛이 인벤토리에 추가됐을 때 전역 버스로 발행되는 알림입니다.</summary>
    public readonly struct PlayerUnitAdded
    {
        public UnitData Unit { get; }
        public PlayerUnitAdded(UnitData unit) => Unit = unit;
    }
}
