using System;
using System.Collections.Generic;
using OzGameLab01.Data;
using OzGameLab01.Effects.Models;
using OzGameLab01.Common;

namespace OzGameLab01.Managers
{
    /// <summary>Serialized legacy component bridge. All content is owned by RuntimeContentService.</summary>
    public sealed class RuntimeDataManager : Singleton<RuntimeDataManager>, IDataNotificationSource
    {
        private RuntimeContentService _service;
        private RuntimeContentService Service => _service ?? (_service = RuntimeContent.Service);
        private readonly DataNotificationPublisher _notifications = new DataNotificationPublisher();
        public event Action<DataNotification> Notification
        {
            add => _notifications.Notification += value;
            remove => _notifications.Notification -= value;
        }
        public bool TryGetLatestNotification(string dataset, out DataNotification notification)
            => Service.TryGetLatestNotification(dataset, out notification);
        public IReadOnlyList<UnitData> Units => Service.Catalog.Units;
        public IReadOnlyList<MonsterData> Enemies => Service.Catalog.Enemies;
        public IReadOnlyDictionary<int, SynergyData> Synergies => Service.Catalog.Synergies;
        public IReadOnlyDictionary<int, RelicData> Relics => Service.Catalog.Relics;
        protected override void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            base.Awake();
            Service.Notification += Forward;
        }
        private void Forward(DataNotification notification)
            => _notifications.Publish(notification.Dataset, notification.Kind, notification.Count);
        private void OnDestroy()
        {
            if (_service != null) _service.Notification -= Forward;
            _notifications.ClearSubscribers();
        }
        public void LoadRosters() => Service.Reload();
        public void LoadSynergies() => Service.Reload();
        public void LoadRelics() => Service.Reload();
        public UnitData GetUnit(int id) => Service.Catalog.GetUnit(id);
        public MonsterData GetEnemy(int id) => Service.Catalog.GetEnemy(id);
        public SkillData GetSkill(int id) => Service.Catalog.GetSkill(id);
        public SynergyData GetSynergy(int id) => Service.Catalog.GetSynergy(id);
        public RelicData GetRelic(int id) => Service.Catalog.GetRelic(id);
        public IReadOnlyList<RuntimeEffectManager.EffectSource> GetEffects(TriggerType trigger)
            => SystemBus.Get<EffectsFacade>()?.GetEffects(trigger) ?? Array.Empty<RuntimeEffectManager.EffectSource>();
    }
}
