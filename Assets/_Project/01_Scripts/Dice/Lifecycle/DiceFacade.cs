using System;
using System.Collections.Generic;
using OzGameLab01.Common.Messaging;
using OzGameLab01.Dice.Contracts;

namespace OzGameLab01.Dice
{
    /// <summary>외부 요청을 시스템 내부 버스로 연결. 공개 메서드는 기존 호출 호환용.</summary>
    public sealed class DiceFacade : IDisposable
    {
        private readonly MessageBus _local;
        private readonly List<IDisposable> _registrations = new();
        public bool HasRolledThisTurn => _local.Request<DiceSnapshotRequested, DiceSnapshot>(default).HasRolledThisTurn;
        public event Action<int> OnDiceRolled;

        public DiceFacade(MessageBus local, MessageBus global)
        {
            _local = local;
            try
            {
                _registrations.Add(global.Handle<DiceRollRequested, DiceRollResult>(request => local.Request<DiceRollRequested, DiceRollResult>(request)));
                _registrations.Add(global.Handle<DiceSnapshotRequested, DiceSnapshot>(request => local.Request<DiceSnapshotRequested, DiceSnapshot>(request)));
                _registrations.Add(global.Handle<DiceResetRequested, bool>(request => local.Request<DiceResetRequested, bool>(request)));
                _registrations.Add(local.Subscribe<DiceRolled>(global.Publish));
                _registrations.Add(local.Subscribe<DiceRolled>(message => OnDiceRolled?.Invoke(message.Value)));
            }
            catch { Dispose(); throw; }
        }
        public void RollDice() => _local.Request<DiceRollRequested, DiceRollResult>(default);
        public void ResetTurnRoll() => _local.Request<DiceResetRequested, bool>(default);
        public void ResetRunState() => ResetTurnRoll();
        public void Dispose()
        {
            for (int i = _registrations.Count - 1; i >= 0; i--) _registrations[i].Dispose();
            _registrations.Clear();
            OnDiceRolled = null;
        }
    }
}
