using System;
using System.Collections.Generic;
using OzGameLab01.Combat;
using OzGameLab01.Data;
using UnityEngine;

namespace OzGameLab01.Managers
{
    /// <summary>
    /// 플레이어가 현재 보유한 유닛 패시브와 유물 효과를 한 곳에 모아
    /// 트리거별로 인덱싱하고, 상시 스탯 효과를 미리 계산해 캐싱합니다.
    ///
    /// 이 클래스는 정적 Content(UnitData/RelicData)을 수정하지 않습니다.
    /// 효과가 추가·제거될 때만 RefreshFromPlayerState()를 호출해 캐시를 재구성합니다.
    /// 실제 전투 유닛의 초 단위 디버프(UnitStatusEffects)는 별도 시스템으로 유지합니다.
    /// </summary>
    public sealed class RuntimeEffectManager : Singleton<RuntimeEffectManager>, OzGameLab01.Effects.Contracts.IEffectsNotificationSource
    {
        private void OnDestroy() => _notifications.ClearSubscribers();

        private readonly OzGameLab01.Effects.Controllers.EffectsNotificationPublisher _notifications = new OzGameLab01.Effects.Controllers.EffectsNotificationPublisher();
        public event System.Action<OzGameLab01.Effects.Models.EffectsNotification> Notification
        {
            add => _notifications.Notification += value;
            remove => _notifications.Notification -= value;
        }

        public enum EffectSourceKind
        {
            UnitPassive,
            Relic
        }

        public readonly struct EffectSource
        {
            public EffectSourceKind Kind { get; }
            public int SourceId { get; }
            public EffectInstance Definition { get; }
            public int DeclarationIndex { get; }

            public EffectSource(
                EffectSourceKind kind,
                int sourceId,
                EffectInstance definition,
                int declarationIndex)
            {
                Kind = kind;
                SourceId = sourceId;
                Definition = definition;
                DeclarationIndex = declarationIndex;
            }
        }

        public readonly struct StatModifierCache
        {
            public float Additive { get; }
            public float Multiplicative { get; }
            public int SourceCount { get; }

            public StatModifierCache(float additive, float multiplicative, int sourceCount)
            {
                Additive = additive;
                Multiplicative = multiplicative;
                SourceCount = sourceCount;
            }

            public float Apply(float baseValue)
            {
                return (baseValue + Additive) * Multiplicative;
            }
        }

        private readonly OzGameLab01.Effects.Models.RuntimeEffectCache<EffectSource> _cache =
            new OzGameLab01.Effects.Models.RuntimeEffectCache<EffectSource>(source => source.Definition, source => (int)source.Kind, source => source.SourceId, source => source.DeclarationIndex);

        public bool IsCacheReady => _cache.IsCacheReady;
        public int CachedEffectCount => _cache.CachedEffectCount;
        public IReadOnlyList<EffectSource> AllEffects => _cache.AllEffects;
        public IReadOnlyList<EffectSource> OrderedEffects => _cache.OrderedEffects;
        /// <summary>
        /// PlayerInventoryManager와 RelicManager의 현재 상태를 읽어 효과 캐시를 재생성합니다.
        /// 보유 목록이 변경되는 획득·복원·초기화 시점에만 호출합니다.
        /// </summary>
        public void RefreshFromPlayerState()
        {
            List<EffectSource> sources = new List<EffectSource>();

            PlayerInventoryManager inventory = FindAnyObjectByType<PlayerInventoryManager>();
            if (inventory != null)
            {
                foreach (UnitData unit in inventory.Facade.OwnedUnits)
                {
                    AddSourceEffects(sources,
                        EffectSourceKind.UnitPassive,
                        unit != null ? unit.id : 0,
                        unit != null ? unit.passiveEffects : null);
                }
            }

            RelicManager relicManager = FindAnyObjectByType<RelicManager>();
            if (relicManager != null)
            {
                foreach (RelicRuntimeInstance relic in relicManager.OwnedRelics)
                {
                    if (relic?.Data == null)
                    {
                        continue;
                    }

                    AddSourceEffects(sources,
                        EffectSourceKind.Relic,
                        relic.Data.id,
                        relic.Data.effects);
                }
            }

            _cache.Rebuild(sources);
            _notifications.Publish(OzGameLab01.Effects.Models.EffectsNotificationKind.CacheRebuilt, 0, CachedEffectCount);
        }

        /// <summary>
        /// 특정 트리거에 연결된 효과만 반환합니다. 반환 리스트는 내부 캐시이므로 수정하지 않습니다.
        /// </summary>
        public IReadOnlyList<EffectSource> GetEffects(TriggerType trigger)
        {
            return _cache.GetEffects(trigger);
        }

        public bool TryGetAlwaysStatModifier(EffectStatType statType, EffectTarget target, out StatModifierCache modifier)
        {
            bool found = _cache.TryGetAlwaysStatModifier(statType, target, out var cached);
            modifier = new StatModifierCache(cached.Additive, cached.Multiplicative, cached.SourceCount);
            return found;
        }
        private static void AddSourceEffects(
            List<EffectSource> sources,
            EffectSourceKind sourceKind,
            int sourceId,
            IReadOnlyList<EffectInstance> definitions)
        {
            if (definitions == null)
            {
                return;
            }

            for (int i = 0; i < definitions.Count; i++)
            {
                EffectInstance definition = definitions[i];
                EffectSource source = new EffectSource(sourceKind, sourceId, definition, i);
                sources.Add(source);
            }
        }

    }
}
