using OzGameLab01.Common.Messaging;

namespace OzGameLab01.Board.Contracts
{
    public readonly struct BoardDiceStateRequested : IRequest<BoardDiceSnapshot> { }
    public readonly struct BoardDiceSnapshot
    {
        public bool Available { get; }
        public bool IsMoving { get; }
        public int RemainingValue { get; }
        public BoardDiceSnapshot(bool available, bool moving, int remaining)
        { Available = available; IsMoving = moving; RemainingValue = remaining; }
    }
    public readonly struct BoardDiceValueRequested : IRequest<bool>
    {
        public int Value { get; }
        public BoardDiceValueRequested(int value) => Value = value;
    }
}
