using System;
using OzGameLab01.Common;
using UnityEngine;

namespace OzGameLab01.Data
{
    /// <summary>Composition entry for Bootstrap and direct-scene startup. Service itself is plain C#.</summary>
    public static class RuntimeContent
    {
        private static RuntimeContentService _owned;
        private static OzGameLab01.Events.EventDB _eventDatabase;
        public static bool UsesDataManager { get; private set; }
        public static void BindEvents(OzGameLab01.Events.EventDB database)
        {
            if (database == null || _eventDatabase == database) return;
            var previous = _eventDatabase;
            _eventDatabase = database;
            if (_owned != null && !_owned.Reload())
            {
                _eventDatabase = previous;
                throw new InvalidOperationException("Event content binding failed.", _owned.LastError);
            }
        }
        public static RuntimeContentService Service
        {
            get
            {
                RuntimeContentService registered = SystemBus.Get<RuntimeContentService>();
                if (registered != null) return registered;
                var service = new RuntimeContentService(() => ResourcesContentLoader.Load(_eventDatabase));
                if (!service.Reload())
                {
                    Exception error = service.LastError;
                    service.Dispose();
                    throw new InvalidOperationException("Runtime content initialization failed.", error);
                }
                SystemBus.Register(service);
                service.Notification += RefreshEffects;
                _owned = service;
                return service;
            }
        }
        public static ContentCatalog Catalog => Service.Catalog;

        public static void UseDataManager(OzGameLab01.Events.EventDB eventDatabase = null)
        {
            if (!OzGameLab01.Managers.DataManager.IsInitialized)
                throw new InvalidOperationException("DataManager must be initialized before it becomes the runtime source.");
            if (eventDatabase != null) _eventDatabase = eventDatabase;
            RuntimeContentService service = Service;
            if (!service.ReplaceLoader(() => DataManagerContentLoader.Load(_eventDatabase)))
                throw new InvalidOperationException("Addressables content activation failed.", service.LastError);
            UsesDataManager = true;
        }

        private static void RefreshEffects(DataNotification notification)
        {
            if (notification.Dataset == typeof(ContentCatalog).FullName && notification.Kind == DataNotificationKind.CacheReplaced)
                SystemBus.Get<OzGameLab01.Effects.Models.EffectsFacade>()?.RefreshFromPlayerState();
        }

        public static void Shutdown()
        {
            if (_owned == null) return;
            SystemBus.Unregister(_owned);
            _owned.Dispose();
            _owned = null;
            UsesDataManager = false;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSession()
        {
            Shutdown();
            _eventDatabase = null;
            UsesDataManager = false;
        }
    }
}
