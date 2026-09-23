using NUnit.Framework;
using OzGameLab01.Controllers;

namespace OzGameLab01.Tests.EditMode
{
    public sealed class TutorialSequenceStateTests
    {
        [Test]
        public void SequenceStateSkipsEmptySlotsAndActivatesInOrder()
        {
            object first = new object();
            object second = new object();
            var state = new TutorialSequenceState<object>(
                new[] { null, first, null, second });

            state.Start();

            Assert.That(state.PeekNext(), Is.SameAs(first));
            Assert.That(state.TryQueue(first), Is.True);
            Assert.That(state.ActivatePending(), Is.SameAs(first));
            Assert.That(state.PeekNext(), Is.Null);

            Assert.That(state.ClearActive(), Is.SameAs(first));
            Assert.That(state.PeekNext(), Is.SameAs(second));
            Assert.That(state.TryQueue(second), Is.True);
            Assert.That(state.ActivatePending(), Is.SameAs(second));
            state.ClearActive();

            Assert.That(state.HasRemainingSteps, Is.False);
        }

        [Test]
        public void StopClearsRuntimeStateWithoutChangingConfiguration()
        {
            object step = new object();
            var state = new TutorialSequenceState<object>(new[] { step });

            state.Start();
            state.TryQueue(step);
            state.Stop();

            Assert.That(state.IsPlaying, Is.False);
            Assert.That(state.ActiveStep, Is.Null);
            Assert.That(state.PendingStep, Is.Null);
            Assert.That(state.HasConfiguredSteps, Is.True);
        }
    }
}
