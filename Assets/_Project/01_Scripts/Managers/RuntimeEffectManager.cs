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

        private readonly struct StatCacheKey : IEquatable<StatCacheKey>
        {
            public readonly EffectStatType StatType;
            public readonly EffectTarget Target;

            public StatCacheKey(EffectStatType statType, EffectTarget target)
            {
                StatType = statType;
                Target = target;
            }

            public bool Equals(StatCacheKey other)
            {
                return StatType == other.StatType && Target == other.Target;
            }

            public override bool Equals(object obj)
            {
                return obj is StatCacheKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return ((int)StatType * 397) ^ (int)Target;
                }
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

        private readonly List<EffectSource> _allEffects = new List<EffectSource>();
        private readonly Dictionary<TriggerType, List<EffectSource>> _effectsByTrigger =
            new Dictionary<TriggerType, List<EffectSource>>();
        private readonly Dictionary<StatCacheKey, StatModifierCache> _alwaysStatCache =
            new Dictionary<StatCacheKey, StatModifierCache>();

        public bool IsCacheReady { get; private set; }
        public int CachedEffectCount => _allEffects.Count;

        /// <summary>
        /// 현재 플레이어 상태에서 수집된 효과를 실행 우선순위대로 정렬한 목록입니다.
        /// Always 효과와 스탯 계산이 먼저 오고, 같은 조건에서는 유닛 패시브가 유물보다 먼저 오며,
        /// 원본 데이터 선언 순서까지 tie-breaker로 사용합니다.
        /// </summary>
        public IReadOnlyList<EffectSource> AllEffects => _allEffects;

        public IReadOnlyList<EffectSource> OrderedEffects => _allEffects;

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
            _allEffects.Clear();
            _effectsByTrigger.Clear();
            _alwaysStatCache.Clear();
            IsCacheReady = false;

            PlayerInventoryManager inventory = FindAnyObjectByType<PlayerInventoryManager>();
            if (inventory != null)
            {
                foreach (UnitData unit in inventory.OwnedUnits)
                {
                    AddSourceEffects(
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

                    AddSourceEffects(
                        EffectSourceKind.Relic,
                        relic.Data.id,
                        relic.Data.effects);
                }
            }

            BuildCaches();
        }

        /// <summary>
        /// 특정 트리거에 연결된 효과만 반환합니다. 반환 리스트는 내부 캐시이므로 수정하지 않습니다.
        /// </summary>
        public IReadOnlyList<EffectSource> GetEffects(TriggerType trigger)
        {
            if (_effectsByTrigger.TryGetValue(trigger, out List<EffectSource> effects))
            {
                return effects;
            }

            return Array.Empty<EffectSource>();
        }

        /// <summary>
        /// 상시 적용되는 스탯 효과의 사전 계산 결과를 반환합니다.
        /// statType이 지정되지 않은 기존 효과는 결과에 포함되지 않습니다.
        /// </summary>
        public bool TryGetAlwaysStatModifier(
            EffectStatType statType,
            EffectTarget target,
            out StatModifierCache modifier)
        {
            return _alwaysStatCache.TryGetValue(
                new StatCacheKey(statType, target), out modifier);
        }

        private void AddSourceEffects(
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
                _allEffects.Add(source);
            }
        }

        private void BuildCaches()
        {
            _allEffects.Sort(CompareEffectSources);
            _effectsByTrigger.Clear();
            for (int i = 0; i < _allEffects.Count; i++)
            {
                EffectSource source = _allEffects[i];
                if (!_effectsByTrigger.TryGetValue(source.Definition.trigger, out List<EffectSource> triggerEffects))
                {
                    triggerEffects = new List<EffectSource>();
                    _effectsByTrigger.Add(source.Definition.trigger, triggerEffects);
                }

                triggerEffects.Add(source);
            }

            Dictionary<StatCacheKey, float> additive = new Dictionary<StatCacheKey, float>();
            Dictionary<StatCacheKey, float> multiplicative = new Dictionary<StatCacheKey, float>();
            Dictionary<StatCacheKey, int> sourceCounts = new Dictionary<StatCacheKey, int>();

            for (int i = 0; i < _allEffects.Count; i++)
            {
                EffectInstance effect = _allEffects[i].Definition;
                if (effect.trigger != TriggerType.Always ||
                    effect.effect != EffectType.StatModifier ||
                    effect.statType == EffectStatType.Unknown)
                {
                    continue;
                }

                StatCacheKey key = new StatCacheKey(effect.statType, effect.target);
                if (!sourceCounts.ContainsKey(key))
                {
                    sourceCounts[key] = 0;
                }

                sourceCounts[key]++;

                if (effect.operation == EffectOperation.Multiply)
                {
                    if (!multiplicative.ContainsKey(key))
                    {
                        multiplicative[key] = 1f;
                    }

                    multiplicative[key] *= effect.effectParam;
                }
                else
                {
                    if (!additive.ContainsKey(key))
                    {
                        additive[key] = 0f;
                    }

                    additive[key] += effect.effectParam;
                }
            }

            foreach (KeyValuePair<StatCacheKey, int> pair in sourceCounts)
            {
                float add = additive.TryGetValue(pair.Key, out float additiveValue) ? additiveValue : 0f;
                float multiply = multiplicative.TryGetValue(pair.Key, out float multiplicativeValue)
                    ? multiplicativeValue
                    : 1f;
                _alwaysStatCache[pair.Key] = new StatModifierCache(add, multiply, pair.Value);
            }

            IsCacheReady = true;
        }

        private static int CompareEffectSources(EffectSource left, EffectSource right)
        {
            int compare = GetTriggerPriority(left.Definition.trigger)
                .CompareTo(GetTriggerPriority(right.Definition.trigger));
            if (compare != 0)
            {
                return compare;
            }

            compare = GetOperationPriority(left.Definition).CompareTo(GetOperationPriority(right.Definition));
            if (compare != 0)
            {
                return compare;
            }

            compare = left.Kind.CompareTo(right.Kind);
            if (compare != 0)
            {
                return compare;
            }

            compare = left.SourceId.CompareTo(right.SourceId);
            return compare != 0
                ? compare
                : left.DeclarationIndex.CompareTo(right.DeclarationIndex);
        }

        private static int GetOperationPriority(EffectInstance effect)
        {
            if (effect.trigger == TriggerType.Always && effect.effect == EffectType.StatModifier)
            {
                return effect.operation == EffectOperation.Add ? 0 : 1;
            }

            return 0;
        }

        private static int GetTriggerPriority(TriggerType trigger)
        {
            switch (trigger)
            {
                case TriggerType.Always: return 0;
                case TriggerType.OnBattleStart: return 10;
                case TriggerType.OnNightStart: return 20;
                case TriggerType.OnSelfDeath: return 30;
                case TriggerType.OnFirstAllyDeath: return 31;
                case TriggerType.OnAllyDeath: return 32;
                case TriggerType.OnHpBelowThreshold: return 40;
                case TriggerType.OnAttackCount: return 50;
                case TriggerType.OnAllySkillUsed: return 60;
                case TriggerType.OnEnemySkillUsed: return 70;
                case TriggerType.OnAllyHealed: return 80;
                case TriggerType.OnAllyBuffed: return 90;
                case TriggerType.OnAllyShielded: return 100;
                case TriggerType.OnBattleEnd: return 110;
                default: return 1000;
            }
        }
    }
}
