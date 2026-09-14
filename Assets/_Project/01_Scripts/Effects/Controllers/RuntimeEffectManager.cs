using System;
using System.Collections.Generic;
using OzGameLab01.Combat;
using OzGameLab01.Data;
using Newtonsoft.Json;
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
    public sealed class RuntimeEffectManager : Singleton<RuntimeEffectManager>
    {
        public enum EffectSourceKind
        {
            UnitPassive,
            Relic
        }

        public readonly struct EffectSource
        {
            public readonly EffectSourceKind Kind;
            public readonly int SourceId;
            public readonly EffectInstance Definition;
            public readonly int DeclarationIndex;

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
            public readonly float Additive;
            public readonly float Multiplicative;
            public readonly int SourceCount;

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
        /// 임시 검증용 JSON 로더입니다. 실제 효과 캐시 입력과 분리되어 있으며,
        /// 전투 씬 진입 시 TempUnitData.json 역직렬화 결과를 Unity Console에 출력합니다.
        /// </summary>
        [ContextMenu("Debug/Load Temp Unit JSON")]
        public bool LoadTempUnitJsonAndLog()
        {
            const string resourcePath = "TempUnitData";
            TextAsset jsonFile = Resources.Load<TextAsset>(resourcePath);
            if (jsonFile == null)
            {
                Debug.LogError(
                    $"[RuntimeEffectManager][TempJson] Resources/{resourcePath}.json을 찾지 못했습니다.",
                    this);
                return false;
            }

            try
            {
                UnitDataList data = JsonConvert.DeserializeObject<UnitDataList>(jsonFile.text);
                if (data?.unitList == null || data.unitList.Count == 0)
                {
                    Debug.LogError(
                        "[RuntimeEffectManager][TempJson] 역직렬화는 되었지만 unitList가 비어 있습니다.",
                        this);
                    return false;
                }

                UnitData first = data.unitList[0];
                Debug.Log(
                    $"[RuntimeEffectManager][TempJson] Load OK: count={data.unitList.Count}, " +
                    $"first.id={first.id}, first.name={first.name}, " +
                    $"first.skillIds.Count={(first.skillIds?.Count ?? 0)}",
                    this);
                return true;
            }
            catch (JsonException exception)
            {
                Debug.LogError(
                    $"[RuntimeEffectManager][TempJson] JSON 역직렬화 실패: {exception.Message}",
                    this);
                return false;
            }
        }

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
                foreach (UnitData unit in inventory.OwnedUnits)
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
