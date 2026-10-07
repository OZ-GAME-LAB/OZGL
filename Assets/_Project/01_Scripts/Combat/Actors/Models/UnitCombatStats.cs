using System;
using System.Collections.Generic;
using OzGameLab01.Data;

namespace OzGameLab01.Combat
{
    /// <summary>Recalculates from base values; timed bonuses never permanently alter the base.</summary>
    public sealed class UnitCombatStats
    {
        private sealed class Modifier
        {
            public EffectStatType Stat;
            public EffectOperation Operation;
            public float Percent;
            public float Seconds;
            public bool UntilBattleEnd;
        }
        private readonly Dictionary<EffectStatType, float> _base = new Dictionary<EffectStatType, float>();
        private readonly List<Modifier> _modifiers = new List<Modifier>();
        public void SetBase(EffectStatType stat, float value) => _base[stat] = value;
        public float GetBase(EffectStatType stat) => _base.TryGetValue(stat, out float value) ? value : 0;
        public float GetMultiplier(EffectStatType stat)
        {
            float add = 0, multiply = 0;
            foreach (Modifier modifier in _modifiers)
            {
                if (modifier.Stat != stat) continue;
                if (modifier.Operation == EffectOperation.Add) add += modifier.Percent / 100f;
                else multiply += modifier.Percent / 100f;
            }
            return Math.Max(0, (1 + add) * (1 + multiply));
        }
        public float Get(EffectStatType stat) => GetBase(stat) * GetMultiplier(stat);
        public bool Add(EffectStatType stat, float percent, EffectOperation operation, float seconds, bool untilBattleEnd)
        {
            if (!_base.ContainsKey(stat) || float.IsNaN(percent) || float.IsInfinity(percent)) return false;
            if (!untilBattleEnd && (seconds <= 0 || float.IsNaN(seconds) || float.IsInfinity(seconds))) return false;
            _modifiers.Add(new Modifier { Stat = stat, Percent = percent, Operation = operation,
                Seconds = seconds, UntilBattleEnd = untilBattleEnd });
            return true;
        }
        public bool Tick(float seconds)
        {
            if (seconds < 0 || float.IsNaN(seconds) || float.IsInfinity(seconds)) throw new ArgumentOutOfRangeException(nameof(seconds));
            bool changed = false;
            for (int i = _modifiers.Count - 1; i >= 0; i--)
            {
                var modifier = _modifiers[i];
                if (modifier.UntilBattleEnd) continue;
                modifier.Seconds -= seconds;
                if (modifier.Seconds > 0) continue;
                _modifiers.RemoveAt(i);
                changed = true;
            }
            return changed;
        }
    }
}
