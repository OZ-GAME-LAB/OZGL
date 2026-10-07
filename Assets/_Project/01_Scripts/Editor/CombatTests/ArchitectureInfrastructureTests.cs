using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using OzGameLab01.Common.Messaging;
using OzGameLab01.Common.Operations;

namespace OzGameLab01.Tests.EditMode
{
    public class ArchitectureInfrastructureTests
    {
        private readonly struct ReadValue : IRequest<int> { }

        [Test]
        public void OperationListIsAValueSnapshot()
        {
            var registry = new OperationRegistry();
            registry.TryBegin("reward:1", OperationPolicy.OnceUntilForgotten, out var lease);
            var snapshot = registry.GetSnapshot();
            lease.Complete();
            Assert.That(snapshot[0].Status, Is.EqualTo(OperationStatus.Running));
            Assert.That(registry.GetSnapshot()[0].Status, Is.EqualTo(OperationStatus.Completed));
            Assert.That(snapshot[0].Key, Is.EqualTo("reward:1"));
        }

        [Test]
        public void TransitionModelsShareWorkExclusionAndReleaseOnCompletionOrCancellation()
        {
            var registry = new OperationRegistry();
            var first = new OzGameLab01.GameFlow.Models.SceneTransitionModel(registry);
            var second = new OzGameLab01.GameFlow.Models.SceneTransitionModel(registry);
            Assert.That(first.TryBegin("Title", "Board"), Is.True);
            Assert.That(second.TryBegin("Title", "Combat"), Is.False);
            first.Finish();
            Assert.That(second.TryBegin("Title", "Combat"), Is.True);
            second.Cancel();
            Assert.That(first.TryBegin("Combat", "Board"), Is.True);
            first.Cancel();
        }

        [Test]
        public void RequestsRejectMissingAndDuplicateOwnersAndAllowReplacementAfterDisposal()
        {
            using var bus = new MessageBus();
            Assert.Throws<InvalidOperationException>(() => bus.Request<ReadValue, int>(default));
            IDisposable first = bus.Handle<ReadValue, int>(_ => 7);
            Assert.Throws<InvalidOperationException>(() => bus.Handle<ReadValue, int>(_ => 8));
            Assert.That(bus.Request<ReadValue, int>(default), Is.EqualTo(7));
            first.Dispose();
            using var second = bus.Handle<ReadValue, int>(_ => 8);
            first.Dispose();
            Assert.That(bus.Request<ReadValue, int>(default), Is.EqualTo(8));
        }

        [Test]
        public void CyclicRequestFailsAndDoesNotPoisonLaterRequests()
        {
            using var bus = new MessageBus();
            bool recurse = true;
            using var registration = bus.Handle<ReadValue, int>(_ => recurse ? bus.Request<ReadValue, int>(default) : 3);
            Assert.Throws<InvalidOperationException>(() => bus.Request<ReadValue, int>(default));
            recurse = false;
            Assert.That(bus.Request<ReadValue, int>(default), Is.EqualTo(3));
        }

        [Test]
        public void NotificationsPreserveNestedOrderAndContinueAfterSubscriberFailure()
        {
            var errors = new List<Exception>();
            using var bus = new MessageBus(errors.Add);
            var order = new List<int>();
            using var a = bus.Subscribe<int>(value => { order.Add(value); if (value == 1) bus.Publish(2); });
            using var b = bus.Subscribe<int>(_ => throw new InvalidOperationException("test"));
            using var c = bus.Subscribe<int>(value => order.Add(value * 10));
            bus.Publish(1);
            Assert.That(order, Is.EqualTo(new[] { 1, 10, 2, 20 }));
            Assert.That(errors.Count, Is.EqualTo(1));
        }

        [Test]
        public void DisposedSubscriberDoesNotReceivePendingNotification()
        {
            using var bus = new MessageBus();
            int count = 0;
            IDisposable pending = null;
            using var first = bus.Subscribe<int>(_ => pending.Dispose());
            pending = bus.Subscribe<int>(_ => count++);
            bus.Publish(1);
            Assert.That(count, Is.Zero);
        }

        [Test]
        public void DisposedBusRejectsWork()
        {
            var bus = new MessageBus();
            bus.Dispose();
            Assert.Throws<ObjectDisposedException>(() => bus.Publish(1));
            Assert.Throws<ObjectDisposedException>(() => bus.Subscribe<int>(_ => { }));
        }

        [Test]
        public void FacadeRegistryRejectsReplacementAndOnlyOwnerCanUnregister()
        {
            var registry = new FacadeRegistry();
            var owner = new object();
            registry.Register(owner);
            registry.Register(owner);
            Assert.Throws<InvalidOperationException>(() => registry.Register(new object()));
            registry.Unregister(new object());
            Assert.That(registry.Get<object>(), Is.SameAs(owner));
            registry.Unregister(owner);
            Assert.That(registry.Get<object>(), Is.Null);
        }

        [Test]
        public void OperationStartIsAtomicAcrossConcurrentCallers()
        {
            var registry = new OperationRegistry();
            int winners = 0;
            Parallel.For(0, 32, _ =>
            {
                if (registry.TryBegin("reward:123", OperationPolicy.OnceUntilForgotten, out var lease))
                { Interlocked.Increment(ref winners); lease.Complete(); }
            });
            Assert.That(winners, Is.EqualTo(1));
            Assert.That(registry.TryGetStatus("reward:123", out var status), Is.True);
            Assert.That(status, Is.EqualTo(OperationStatus.Completed));
        }

        [Test]
        public void CompletedOperationRequiresExplicitForgetButRunningCannotBeForgotten()
        {
            var registry = new OperationRegistry();
            Assert.That(registry.TryBegin("reward:1", OperationPolicy.OnceUntilForgotten, out var first), Is.True);
            Assert.That(registry.ForgetCompleted("reward:1"), Is.False);
            first.Complete();
            first.Dispose();
            Assert.That(registry.TryBegin("reward:1", OperationPolicy.OnceUntilForgotten, out _), Is.False);
            Assert.That(registry.ForgetCompleted("reward:1"), Is.True);
            Assert.That(registry.TryBegin("reward:1", OperationPolicy.OnceUntilForgotten, out var next), Is.True);
            next.Dispose();
        }

        [Test]
        public void FailedOrCancelledOperationCanRetryAndOldLeaseCannotEndNewWork()
        {
            var registry = new OperationRegistry();
            registry.TryBegin("save", OperationPolicy.ConcurrentOnly, out var first);
            first.Fail();
            Assert.That(registry.TryBegin("save", OperationPolicy.ConcurrentOnly, out var next), Is.True);
            first.Dispose();
            Assert.That(registry.TryBegin("save", OperationPolicy.ConcurrentOnly, out _), Is.False);
            next.Dispose();
            Assert.That(registry.TryBegin("save", OperationPolicy.ConcurrentOnly, out var last), Is.True);
            last.Complete();
            Assert.That(registry.TryGetStatus("save", out _), Is.False);
        }
    }
}
