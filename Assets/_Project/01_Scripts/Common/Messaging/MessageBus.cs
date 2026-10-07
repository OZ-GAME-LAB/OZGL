using System;
using System.Collections.Generic;

namespace OzGameLab01.Common.Messaging
{
    public interface IRequest<TResult> { }

    /// <summary>소유 스레드에서 동기 전달. 등록 토큰은 수명 소유자가 해제한다.</summary>
    public sealed class MessageBus : IDisposable
    {
        private sealed class Registration : IDisposable
        {
            private Action _remove;
            public readonly Delegate Handler;
            public bool Active => _remove != null;
            public Registration(Delegate handler, Action remove) { Handler = handler; _remove = remove; }
            public void Dispose() { Action remove = _remove; _remove = null; remove?.Invoke(); }
        }
        private readonly Dictionary<Type, Registration> _requests = new();
        private readonly Dictionary<Type, List<Registration>> _subscribers = new();
        private readonly HashSet<Type> _activeRequests = new();
        private readonly Queue<Action> _notifications = new();
        private readonly Action<Exception> _reportException;
        private bool _dispatching;
        private bool _disposed;
        public MessageBus(Action<Exception> reportException = null) => _reportException = reportException;

        public IDisposable Handle<TRequest, TResult>(Func<TRequest, TResult> handler) where TRequest : IRequest<TResult>
        {
            ThrowIfDisposed();
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            Type type = typeof(TRequest);
            if (_requests.ContainsKey(type)) throw new InvalidOperationException($"Duplicate request handler: {type.FullName}");
            Registration registration = null;
            registration = new Registration(handler, () =>
            {
                if (_requests.TryGetValue(type, out Registration current) && ReferenceEquals(current, registration))
                    _requests.Remove(type);
            });
            _requests.Add(type, registration);
            return registration;
        }

        public TResult Request<TRequest, TResult>(TRequest request) where TRequest : IRequest<TResult>
        {
            ThrowIfDisposed();
            Type type = typeof(TRequest);
            if (!_requests.TryGetValue(type, out Registration registration))
                throw new InvalidOperationException($"Missing request handler: {type.FullName}");
            if (!_activeRequests.Add(type)) throw new InvalidOperationException($"Cyclic request: {type.FullName}");
            try { return ((Func<TRequest, TResult>)registration.Handler)(request); }
            finally { _activeRequests.Remove(type); }
        }

        public IDisposable Subscribe<T>(Action<T> handler)
        {
            ThrowIfDisposed();
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            Type type = typeof(T);
            if (!_subscribers.TryGetValue(type, out List<Registration> entries))
                _subscribers.Add(type, entries = new List<Registration>());
            Registration registration = null;
            registration = new Registration(handler, () =>
            {
                entries.Remove(registration);
                if (entries.Count == 0) _subscribers.Remove(type);
            });
            entries.Add(registration);
            return registration;
        }

        public void Publish<T>(T notification)
        {
            ThrowIfDisposed();
            if (!_subscribers.TryGetValue(typeof(T), out List<Registration> entries)) return;
            Registration[] snapshot = entries.ToArray();
            _notifications.Enqueue(() =>
            {
                var failures = new List<Exception>();
                foreach (Registration entry in snapshot)
                {
                    if (!entry.Active || _disposed) continue;
                    try { ((Action<T>)entry.Handler)(notification); }
                    catch (Exception exception) { failures.Add(exception); }
                }
                if (failures.Count > 0) throw new AggregateException(failures);
            });
            if (_dispatching) return;
            _dispatching = true;
            var errors = new List<Exception>();
            try
            {
                while (_notifications.Count > 0)
                {
                    try { _notifications.Dequeue()(); }
                    catch (Exception exception) { errors.Add(exception); }
                }
            }
            finally { _dispatching = false; }
            if (errors.Count == 0) return;
            var failure = new AggregateException(errors);
            if (_reportException != null) _reportException(failure);
            else throw failure;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _requests.Clear();
            _subscribers.Clear();
            _notifications.Clear();
        }
        private void ThrowIfDisposed()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(MessageBus));
        }
    }
}
