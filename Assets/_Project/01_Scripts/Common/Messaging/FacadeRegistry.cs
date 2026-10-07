using System;
using System.Collections.Generic;

namespace OzGameLab01.Common.Messaging
{
    /// <summary>기존 Facade 조회 경로의 호환 레지스트리. 신규 통신은 MessageBus를 사용한다.</summary>
    public sealed class FacadeRegistry
    {
        private readonly Dictionary<Type, object> _services = new();
        public void Register<T>(T service) where T : class
        {
            if (service == null) throw new ArgumentNullException(nameof(service));
            if (_services.TryGetValue(typeof(T), out object existing) && !ReferenceEquals(existing, service))
                throw new InvalidOperationException($"Duplicate facade: {typeof(T).FullName}");
            _services[typeof(T)] = service;
        }
        public T Get<T>() where T : class => _services.TryGetValue(typeof(T), out object value) ? (T)value : null;
        public void Unregister<T>(T owner) where T : class
        {
            if (ReferenceEquals(Get<T>(), owner)) _services.Remove(typeof(T));
        }
        public void Unregister<T>() where T : class => _services.Remove(typeof(T));
        public void Clear() => _services.Clear();
    }
}
