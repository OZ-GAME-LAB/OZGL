using System;
using System.Collections.Generic;
using OzGameLab01.Combat;

namespace OzGameLab01.Effects.Models
{
    /// <summary>
    /// 플레이어 및 컨트롤러 참조 없이 효과 정렬과 상시 스탯 캐시를 계산합니다.
    /// </summary>
    public sealed class RuntimeEffectCache<TSource>
    {
        private readonly Func<TSource, EffectInstance> _definition;
        private readonly Func<TSource, int> _kind;
        private readonly Func<TSource, int> _sourceId;
        private readonly Func<TSource, int> _declarationIndex;

        public RuntimeEffectCache(Func<TSource, EffectInstance> definition, Func<TSource, int> kind, Func<TSource, int> sourceId, Func<TSource, int> declarationIndex)
        {
            _definition = definition ?? throw new ArgumentNullException(nameof(definition));
            _kind = kind ?? throw new ArgumentNullException(nameof(kind));
            _sourceId = sourceId ?? throw new ArgumentNullException(nameof(sourceId));
            _declarationIndex = declarationIndex ?? throw new ArgumentNullException(nameof(declarationIndex));
        }

        public void Rebuild(IEnumerable<TSource> sources)
        {
            _allEffects.Clear();
            _effectsByTrigger.Clear();
            _alwaysStatCache.Clear();
            IsCacheReady = false;
            if (sources != null)
            {
                _allEffects.AddRange(sources);
            }
            BuildCaches();
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

        public readonly struct EffectStatModifier
        {
            public readonly float Additive;
            public readonly float Multiplicative;
            public readonly int SourceCount;

            public EffectStatModifier(float additive, float multiplicative, int sourceCount)
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

        private readonly List<TSource> _allEffects = new List<TSource>();
        private readonly Dictionary<TriggerType, List<TSource>> _effectsByTrigger =
            new Dictionary<TriggerType, List<TSource>>();
        private readonly Dictionary<StatCacheKey, EffectStatModifier> _alwaysStatCache =
            new Dictionary<StatCacheKey, EffectStatModifier>();

        public bool IsCacheReady { get; private set; }
        public int CachedEffectCount => _allEffects.Count;

        /// <summary>
        /// 현재 플레이어 상태에서 수집된 효과를 실행 우선순위대로 정렬한 목록입니다.
        /// Always 효과와 스탯 계산이 먼저 오고, 같은 조건에서는 유닛 패시브가 유물보다 먼저 오며,
        /// 원본 데이터 선언 순서까지 tie-breaker로 사용합니다.
        /// </summary>
        public IReadOnlyList<TSource> AllEffects => _allEffects.AsReadOnly();

        public IReadOnlyList<TSource> OrderedEffects => _allEffects.AsReadOnly();

        public IReadOnlyList<TSource> GetEffects(TriggerType trigger)
        {
            if (_effectsByTrigger.TryGetValue(trigger, out List<TSource> effects))
            {
                return effects.AsReadOnly();
            }

            return Array.Empty<TSource>();
        }

        /// <summary>
        /// 상시 적용되는 스탯 효과의 사전 계산 결과를 반환합니다.
        /// statType이 지정되지 않은 기존 효과는 결과에 포함되지 않습니다.
        /// </summary>
        public bool TryGetAlwaysStatModifier(
            EffectStatType statType,
            EffectTarget target,
            out EffectStatModifier modifier)
        {
            return _alwaysStatCache.TryGetValue(
                new StatCacheKey(statType, target), out modifier);
        }

        private void BuildCaches()
        {
            _allEffects.Sort(CompareTSources);
            _effectsByTrigger.Clear();
            for (int i = 0; i < _allEffects.Count; i++)
            {
                TSource source = _allEffects[i];
                if (!_effectsByTrigger.TryGetValue(_definition(source).trigger, out List<TSource> triggerEffects))
                {
                    triggerEffects = new List<TSource>();
                    _effectsByTrigger.Add(_definition(source).trigger, triggerEffects);
                }

                triggerEffects.Add(source);
            }

            Dictionary<StatCacheKey, float> additive = new Dictionary<StatCacheKey, float>();
            Dictionary<StatCacheKey, float> multiplicative = new Dictionary<StatCacheKey, float>();
            Dictionary<StatCacheKey, int> sourceCounts = new Dictionary<StatCacheKey, int>();

            for (int i = 0; i < _allEffects.Count; i++)
            {
                EffectInstance effect = _definition(_allEffects[i]);
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
                _alwaysStatCache[pair.Key] = new EffectStatModifier(add, multiply, pair.Value);
            }

            IsCacheReady = true;
        }

        private int CompareTSources(TSource left, TSource right)
        {
            int compare = GetTriggerPriority(_definition(left).trigger)
                .CompareTo(GetTriggerPriority(_definition(right).trigger));
            if (compare != 0)
            {
                return compare;
            }

            compare = GetOperationPriority(_definition(left)).CompareTo(GetOperationPriority(_definition(right)));
            if (compare != 0)
            {
                return compare;
            }

            compare = _kind(left).CompareTo(_kind(right));
            if (compare != 0)
            {
                return compare;
            }

            compare = _sourceId(left).CompareTo(_sourceId(right));
            return compare != 0
                ? compare
                : _declarationIndex(left).CompareTo(_declarationIndex(right));
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
