using System;

namespace OzGameLab01.Data
{
    /// <summary>Loads and publishes a complete content snapshot without owning Unity lifecycle.</summary>
    public sealed class RuntimeContentService : IDataNotificationSource, IDisposable
    {
        private readonly Func<ContentCatalog> _load;
        private readonly DataNotificationPublisher _notifications = new DataNotificationPublisher();
        private ContentCatalog _catalog;
        private bool _loading;
        private bool _disposed;
        public bool IsReady => !_disposed && _catalog != null;
        public long Revision { get; private set; }
        public Exception LastError { get; private set; }
        public ContentCatalog Catalog => IsReady ? _catalog : throw new InvalidOperationException("Content is not ready.");
        public event Action<DataNotification> Notification
        {
            add => _notifications.Notification += value;
            remove => _notifications.Notification -= value;
        }

        public RuntimeContentService(Func<ContentCatalog> load)
            => _load = load ?? throw new ArgumentNullException(nameof(load));

        public bool TryGetLatestNotification(string dataset, out DataNotification notification)
            => _notifications.TryGetLatestNotification(dataset, out notification);

        public bool Reload()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(RuntimeContentService));
            if (_loading) return false;
            _loading = true;
            try
            {
                ContentCatalog next;
                try { next = _load() ?? throw new InvalidOperationException("Loader returned no catalog."); }
                catch (Exception error)
                {
                    LastError = error;
                    _notifications.Publish(typeof(ContentCatalog).FullName, DataNotificationKind.SourceUnavailable, 0);
                    return false;
                }
                _catalog = next;
                Revision++;
                LastError = null;
                Record<UnitData>(next.UnitCount);
                Record<MonsterData>(next.EnemyCount);
                Record<SkillData>(next.Skills.Count);
                Record<SynergyData>(next.Synergies.Count);
                Record<RelicData>(next.Relics.Count);
                Record<EventContent>(next.EventCount);
                Record<EnemyGrowthRow>(next.EnemyGrowth.Count);
                _notifications.Record(typeof(ContentCatalog).FullName, DataNotificationKind.CacheReplaced, next.UnitCount);
                _notifications.DispatchPending();
                return true;
            }
            finally { _loading = false; }
        }

        private void Record<T>(int count)
            => _notifications.Record(typeof(T).FullName, DataNotificationKind.CacheReplaced, count);

        public void Dispose()
        {
            _disposed = true;
            _catalog = null;
            _notifications.ClearSubscribers();
        }
    }
}
