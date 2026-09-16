using System;
using System.Collections.Generic;
using OzGameLab01.Data;

namespace OzGameLab01.Effects.Models
{
    /// <summary>미보유 유물 우선 후보와 기존 가중치 추첨 규칙</summary>
    public static class RelicSelectionModel
    {
        public static RelicData Select(IReadOnlyDictionary<int, RelicData> allRelics,
            IEnumerable<int> ownedIds, Func<int, int> randomIndex, Func<float> randomValue)
        {
            if (allRelics.Count == 0) return null;
            HashSet<int> owned = new HashSet<int>(ownedIds);
            List<RelicData> pool = new List<RelicData>();
            foreach (RelicData relic in allRelics.Values)
            {
                if (!owned.Contains(relic.id)) pool.Add(relic);
            }
            if (pool.Count == 0) pool.AddRange(allRelics.Values);
            float totalWeight = 0f;
            foreach (RelicData relic in pool) totalWeight += Math.Max(0f, relic.dropWeight);
            if (totalWeight <= 0f) return pool[randomIndex(pool.Count)];
            float roll = randomValue() * totalWeight;
            float cumulative = 0f;
            foreach (RelicData relic in pool)
            {
                cumulative += Math.Max(0f, relic.dropWeight);
                if (roll <= cumulative) return relic;
            }
            return pool[pool.Count - 1];
        }
    }
}