using System;
using System.Collections.Generic;
using OzGameLab01.Data;
using OzGameLab01.Effects.Contracts;
using OzGameLab01.Effects.Controllers;
using OzGameLab01.Managers;
using OzGameLab01.Player;
using OzGameLab01.Common;

namespace OzGameLab01.Effects.Models
{
    /// <summary>
    /// RuntimeEffectManager가 노출하는 유일한 진입점입니다. 보유 유닛 패시브/유물 효과를
    /// 트리거별로 인덱싱하고 상시 스탯 효과를 캐싱하는 실제 로직을 담당합니다. 캐시 재구성
    /// 알림은 이 Facade의 Notification으로 발행합니다(아직 외부 구독자가 없어 전역 버스
    /// 연결은 실제 필요 시점에 진행 — Docs/REFACTOR_ARCHITECTURE.md 참고).
    /// </summary>
    public sealed class EffectsFacade : IEffectsNotificationSource
    {
        private readonly EffectsNotificationPublisher _notifications = new EffectsNotificationPublisher();
        public event Action<EffectsNotification> Notification
        {
            add => _notifications.Notification += value;
            remove => _notifications.Notification -= value;
        }

        public OzGameLab01.Combat.CombatEffectCatalog CombatCatalog { get; } = new OzGameLab01.Combat.CombatEffectCatalog();
        private OzGameLab01.Combat.CombatEffectCatalog _cache => CombatCatalog;

        public bool IsCacheReady => _cache.IsCacheReady;
        public int CachedEffectCount => _cache.CachedEffectCount;
        public IReadOnlyList<RuntimeEffectManager.EffectSource> AllEffects => _cache.AllEffects;
        public IReadOnlyList<RuntimeEffectManager.EffectSource> OrderedEffects => _cache.OrderedEffects;

        /// <summary>
        /// PlayerFacade와 RelicManager의 현재 상태를 읽어 효과 캐시를 재생성합니다.
        /// 보유 목록이 변경되는 획득·복원·초기화 시점에만 호출합니다.
        /// </summary>
        public void RefreshFromPlayerState()
        {
            _cache.Rebuild(RuntimeContent.Catalog, SystemBus.Get<PlayerFacade>()?.OwnedUnits,
                RelicManager.Instance.Facade.OwnedRelics);
            _notifications.Publish(EffectsNotificationKind.CacheRebuilt, 0, CachedEffectCount);
        }

        /// <summary>
        /// 특정 트리거에 연결된 효과만 반환합니다. 반환 리스트는 내부 캐시이므로 수정하지 않습니다.
        /// </summary>
        public IReadOnlyList<RuntimeEffectManager.EffectSource> GetEffects(TriggerType trigger)
        {
            return _cache.GetEffects(trigger);
        }

        public bool TryGetAlwaysStatModifier(EffectStatType statType, EffectTarget target, out RuntimeEffectManager.StatModifierCache modifier)
        {
            bool found = _cache.TryGetAlwaysStatModifier(statType, target, out var cached);
            modifier = new RuntimeEffectManager.StatModifierCache(cached.Additive, cached.Multiplicative, cached.SourceCount);
            return found;
        }

        /// <summary>매니저 종료 시 대기 중인 알림과 구독자를 정리합니다.</summary>
        public void ClearSubscriptions() => _notifications.ClearSubscribers();

    }
}
