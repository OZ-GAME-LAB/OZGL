using OzGameLab01.Common.Messaging;

namespace OzGameLab01.Board.Contracts
{
    public readonly struct BoardDiceStateRequested : IRequest<BoardDiceSnapshot> { }
    public readonly struct BoardDiceSnapshot
    {
        public bool Available { get; }
        public bool IsMoving { get; }
        public int RemainingValue { get; }
        public bool HasRolledThisTurn { get; }
        public int RolledDiceValue { get; }

        public BoardDiceSnapshot(
            bool available,
            bool moving,
            int remaining,
            bool hasRolledThisTurn = false,
            int rolledDiceValue = 0)
        {
            Available = available;
            IsMoving = moving;
            RemainingValue = remaining;
            HasRolledThisTurn = hasRolledThisTurn;
            RolledDiceValue = rolledDiceValue;
        }
    }

    public readonly struct BoardDiceValueRequested : IRequest<bool>
    {
        public int Value { get; }
        public BoardDiceValueRequested(int value) => Value = value;
    }
}
