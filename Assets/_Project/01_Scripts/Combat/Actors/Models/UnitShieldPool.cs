using System;
using System.Collections.Generic;

namespace OzGameLab01.Combat
{
    /// <summary>A public total backed by FIFO grants with independent remaining amounts and expiry.</summary>
    public sealed class UnitShieldPool
    {
        private sealed class Grant
        {
            public float Amount;
            public float Seconds;
            public bool UntilBattleEnd;
        }
        private readonly List<Grant> _grants = new List<Grant>();
        public float Total { get; private set; }

        public bool Add(float amount, float seconds, bool untilBattleEnd)
        {
            if (float.IsNaN(amount) || float.IsInfinity(amount) || amount <= 0) return false;
            if (!untilBattleEnd && (float.IsNaN(seconds) || float.IsInfinity(seconds) || seconds <= 0)) return false;
            bool acquired = Total == 0;
            _grants.Add(new Grant { Amount = amount, Seconds = seconds, UntilBattleEnd = untilBattleEnd });
            Total += amount;
            return acquired;
        }

        /// <returns>Damage remaining after shields absorb it.</returns>
        public float Absorb(float damage)
        {
            if (float.IsNaN(damage) || damage <= 0) return 0;
            while (damage > 0 && _grants.Count > 0)
            {
                Grant grant = _grants[0];
                float absorbed = Math.Min(grant.Amount, damage);
                grant.Amount -= absorbed;
                Total -= absorbed;
                damage -= absorbed;
                if (grant.Amount <= 0) _grants.RemoveAt(0);
            }
            if (_grants.Count == 0) Total = 0;
            return damage;
        }

        public void Tick(float deltaTime)
        {
            if (deltaTime < 0 || float.IsNaN(deltaTime) || float.IsInfinity(deltaTime)) throw new ArgumentOutOfRangeException(nameof(deltaTime));
            for (int i = _grants.Count - 1; i >= 0; i--)
            {
                Grant grant = _grants[i];
                if (grant.UntilBattleEnd) continue;
                grant.Seconds -= deltaTime;
                if (grant.Seconds > 0) continue;
                Total -= grant.Amount;
                _grants.RemoveAt(i);
            }
            if (_grants.Count == 0) Total = 0;
        }

        public void Clear() { _grants.Clear(); Total = 0; }
    }
}
