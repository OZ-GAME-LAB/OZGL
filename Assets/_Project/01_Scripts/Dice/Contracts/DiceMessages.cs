using OzGameLab01.Common.Messaging;

namespace OzGameLab01.Dice.Contracts
{
    public readonly struct DiceRollRequested : IRequest<DiceRollResult> { }
    public readonly struct DiceSnapshotRequested : IRequest<DiceSnapshot> { }
    public readonly struct DiceResetRequested : IRequest<bool> { }
    public readonly struct DiceSnapshot
    {
        public bool HasRolledThisTurn { get; }
        public DiceSnapshot(bool hasRolled) => HasRolledThisTurn = hasRolled;
    }
    public enum DiceRollFailure { None, BoardUnavailable, AlreadyRolled, MovementPending }
    public readonly struct DiceRollResult
    {
        public int Value { get; }
        public DiceRollFailure Failure { get; }
        public bool Success => Failure == DiceRollFailure.None;
        public DiceRollResult(int value, DiceRollFailure failure = DiceRollFailure.None)
        { Value = value; Failure = failure; }
    }
    public readonly struct DiceRolled
    {
        public int Value { get; }
        public DiceRolled(int value) => Value = value;
    }
}
