using System;
using System.Collections.Generic;

namespace OzGameLab01.Common.Operations
{
    public enum OperationPolicy { ConcurrentOnly, OnceUntilForgotten }
    public enum OperationStatus { Running, Completed, Failed, Cancelled }

    public readonly struct OperationInfo
    {
        public string Key { get; }
        public OperationPolicy Policy { get; }
        public OperationStatus Status { get; }
        public OperationInfo(string key, OperationPolicy policy, OperationStatus status)
        { Key = key; Policy = policy; Status = status; }
    }

    /// <summary>메모리 내 중복 방지. 저장/복구와 실제 실행은 담당 Model의 책임.</summary>
    public sealed class OperationRegistry
    {
        private readonly object _gate = new();
        private readonly Dictionary<string, OperationLease> _entries = new(StringComparer.Ordinal);

        public IReadOnlyList<OperationInfo> GetSnapshot()
        {
            lock (_gate)
            {
                var result = new List<OperationInfo>(_entries.Count);
                foreach (OperationLease entry in _entries.Values)
                    result.Add(new OperationInfo(entry.Key, entry.Policy, entry.Status));
                return result.AsReadOnly();
            }
        }
        public bool TryBegin(string key, OperationPolicy policy, out OperationLease lease)
        {
            if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("A stable operation key is required.", nameof(key));
            if (policy != OperationPolicy.ConcurrentOnly && policy != OperationPolicy.OnceUntilForgotten)
                throw new ArgumentOutOfRangeException(nameof(policy));
            lock (_gate)
            {
                if (_entries.ContainsKey(key)) { lease = null; return false; }
                lease = new OperationLease(this, key, policy);
                _entries.Add(key, lease);
                return true;
            }
        }
        public bool TryGetStatus(string key, out OperationStatus status)
        {
            lock (_gate)
            {
                if (_entries.TryGetValue(key, out OperationLease entry)) { status = entry.Status; return true; }
                status = default;
                return false;
            }
        }
        public bool ForgetCompleted(string key)
        {
            lock (_gate)
                return _entries.TryGetValue(key, out OperationLease entry) &&
                    entry.Status == OperationStatus.Completed && _entries.Remove(key);
        }
        private void Finish(OperationLease lease, OperationStatus status)
        {
            lock (_gate)
            {
                if (lease.Status != OperationStatus.Running) return;
                lease.Status = status;
                if (!_entries.TryGetValue(lease.Key, out OperationLease current) || !ReferenceEquals(current, lease)) return;
                if (status != OperationStatus.Completed || lease.Policy == OperationPolicy.ConcurrentOnly)
                    _entries.Remove(lease.Key);
            }
        }
        public sealed class OperationLease : IDisposable
        {
            private readonly OperationRegistry _registry;
            public string Key { get; }
            public OperationPolicy Policy { get; }
            public OperationStatus Status { get; internal set; } = OperationStatus.Running;
            internal OperationLease(OperationRegistry registry, string key, OperationPolicy policy)
            { _registry = registry; Key = key; Policy = policy; }
            public void Complete() => _registry.Finish(this, OperationStatus.Completed);
            public void Fail() => _registry.Finish(this, OperationStatus.Failed);
            public void Dispose() => _registry.Finish(this, OperationStatus.Cancelled);
        }
    }
}
