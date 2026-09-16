using System;
using NUnit.Framework;
using OzGameLab01.Common.Messaging;
using OzGameLab01.Dice;
using OzGameLab01.Dice.Contracts;
using OzGameLab01.Board.Contracts;

namespace OzGameLab01.Tests.EditMode
{
    public class DiceMessageFlowTests
    {
        private MessageBus _global;
        private MessageBus _local;
        private DiceModel _model;
        private DiceFacade _facade;
        private BoardDiceSnapshot _board;
        private int _applied;
        private bool _accept;

        [SetUp]
        public void SetUp()
        {
            _global = new MessageBus();
            _local = new MessageBus();
            _board = new BoardDiceSnapshot(true, false, 0);
            _applied = 0;
            _accept = true;
            _global.Handle<BoardDiceStateRequested, BoardDiceSnapshot>(_ => _board);
            _global.Handle<BoardDiceValueRequested, bool>(request => { if (_accept) _applied = request.Value; return _accept; });
            _model = new DiceModel(new DiceState(), _local, _global, () => 4);
            _facade = new DiceFacade(_local, _global);
        }
        [TearDown]
        public void TearDown() { _facade.Dispose(); _model.Dispose(); _local.Dispose(); _global.Dispose(); }

        [Test]
        public void RollAppliesBoardStateBeforeNotificationAndRejectsSecondRoll()
        {
            int notifications = 0;
            _global.Subscribe<DiceRolled>(message =>
            {
                notifications++;
                Assert.That(_applied, Is.EqualTo(message.Value));
                Assert.That(_global.Request<DiceSnapshotRequested, DiceSnapshot>(default).HasRolledThisTurn, Is.True);
            });
            Assert.That(_global.Request<DiceRollRequested, DiceRollResult>(default).Value, Is.EqualTo(4));
            Assert.That(_global.Request<DiceRollRequested, DiceRollResult>(default).Failure, Is.EqualTo(DiceRollFailure.AlreadyRolled));
            Assert.That(notifications, Is.EqualTo(1));
        }

        [TestCase(false, false, 0, DiceRollFailure.BoardUnavailable)]
        [TestCase(true, true, 0, DiceRollFailure.MovementPending)]
        [TestCase(true, false, 3, DiceRollFailure.MovementPending)]
        public void InvalidBoardStateDoesNotConsumeRoll(bool available, bool moving, int remaining, DiceRollFailure reason)
        {
            _board = new BoardDiceSnapshot(available, moving, remaining);
            Assert.That(_global.Request<DiceRollRequested, DiceRollResult>(default).Failure, Is.EqualTo(reason));
            Assert.That(_facade.HasRolledThisTurn, Is.False);
            Assert.That(_applied, Is.Zero);
        }

        [Test]
        public void RejectedBoardWriteRollsBackDiceState()
        {
            _accept = false;
            Assert.That(_global.Request<DiceRollRequested, DiceRollResult>(default).Success, Is.False);
            Assert.That(_facade.HasRolledThisTurn, Is.False);
        }

        [Test]
        public void RunResetPreservesExistingViewSubscription()
        {
            int received = 0;
            _facade.OnDiceRolled += _ => received++;
            _facade.RollDice();
            _facade.ResetRunState();
            _facade.RollDice();
            Assert.That(received, Is.EqualTo(2));
        }

        [Test]
        public void DisposingFacadeRemovesExternalRoutes()
        {
            _facade.Dispose();
            Assert.Throws<InvalidOperationException>(() => _global.Request<DiceSnapshotRequested, DiceSnapshot>(default));
        }
    }
}
