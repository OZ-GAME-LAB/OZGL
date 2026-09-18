using System;
using OzGameLab01.Board.Contracts;
using OzGameLab01.Common.Messaging;
using OzGameLab01.Dice.Contracts;

namespace OzGameLab01.Dice
{
    /// <summary>주사위 규칙과 보드 반영 순서의 소유자. Unity/View/Controller 참조 없음.</summary>
    public sealed class DiceModel : IDisposable
    {
        private readonly DiceState _state;
        private readonly MessageBus _local;
        private readonly MessageBus _global;
        private readonly Func<int> _roll;
        private readonly IDisposable _rollRegistration;
        private readonly IDisposable _snapshotRegistration;
        private readonly IDisposable _resetRegistration;

        public DiceModel(DiceState state, MessageBus local, MessageBus global, Func<int> roll)
        {
            _state = state;
            _local = local;
            _global = global;
            _roll = roll;
            _rollRegistration = local.Handle<DiceRollRequested, DiceRollResult>(HandleRoll);
            _snapshotRegistration =
                local.Handle<DiceSnapshotRequested, DiceSnapshot>(_ =>
                {
                    BoardDiceSnapshot board =
                        _global.Request<BoardDiceStateRequested, BoardDiceSnapshot>(default);
                    bool hasRolled = _state.HasRolledThisTurn || board.HasRolledThisTurn;
                    return new DiceSnapshot(hasRolled);
                });
            _resetRegistration = local.Handle<DiceResetRequested, bool>(_ => { _state.ResetTurn(); return true; });
        }

        private DiceRollResult HandleRoll(DiceRollRequested request)
        {
            BoardDiceSnapshot board =
                _global.Request<BoardDiceStateRequested, BoardDiceSnapshot>(default);

            if (!board.Available)
            {
                return new DiceRollResult(
                    0,
                    DiceRollFailure.BoardUnavailable);
            }

            if (_state.HasRolledThisTurn || board.HasRolledThisTurn)
            {
                return new DiceRollResult(
                    0,
                    DiceRollFailure.AlreadyRolled);
            }

            if (board.IsMoving || board.RemainingValue > 0)
            {
                return new DiceRollResult(
                    0,
                    DiceRollFailure.MovementPending);
            }

            int value = _roll();

            _state.MarkRolled();

            bool applied;
            try
            {
                applied = _global.Request<BoardDiceValueRequested, bool>(
                    new BoardDiceValueRequested(value));
            }
            catch
            {
                _state.ResetTurn();
                throw;
            }

            if (!applied)
            {
                _state.ResetTurn();

                return new DiceRollResult(
                    0,
                    DiceRollFailure.MovementPending);
            }

            _local.Publish(new DiceRolled(value));

            return new DiceRollResult(value);
        }

        public void Dispose()
        {
            _resetRegistration.Dispose();
            _snapshotRegistration.Dispose();
            _rollRegistration.Dispose();
        }
    }
}
