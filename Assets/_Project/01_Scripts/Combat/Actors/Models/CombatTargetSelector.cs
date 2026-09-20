using System;
using System.Collections.Generic;

namespace OzGameLab01.Combat
{
    /// <summary>
    /// Resolves skill targets from an already filtered set of participating allies.
    /// Keeping selection separate from Unit makes target-count and ordering rules testable.
    /// </summary>
    public static class CombatTargetSelector
    {
        public static List<Unit> SelectRandom(IReadOnlyList<Unit> candidates, int targetCount, IRandomProvider random)
        {
            if (candidates == null) throw new ArgumentNullException(nameof(candidates));
            if (random == null) throw new ArgumentNullException(nameof(random));

            var remaining = new List<Unit>(candidates.Count);
            for (int i = 0; i < candidates.Count; i++)
            {
                Unit candidate = candidates[i];
                if (candidate != null && !candidate.IsDead) remaining.Add(candidate);
            }

            int count = Math.Max(1, targetCount);
            var result = new List<Unit>(Math.Min(count, remaining.Count));
            for (int i = 0; i < count && remaining.Count > 0; i++)
            {
                int selected = random.Next(remaining.Count);
                result.Add(remaining[selected]);
                remaining.RemoveAt(selected);
            }

            return result;
        }

        public static List<Unit> SelectWorstHp(IReadOnlyList<Unit> candidates, int targetCount)
        {
            if (candidates == null) throw new ArgumentNullException(nameof(candidates));

            var remaining = new List<Unit>(candidates.Count);
            for (int i = 0; i < candidates.Count; i++)
            {
                Unit candidate = candidates[i];
                if (candidate != null && !candidate.IsDead) remaining.Add(candidate);
            }

            int count = Math.Max(1, targetCount);
            var result = new List<Unit>(Math.Min(count, remaining.Count));
            for (int i = 0; i < count && remaining.Count > 0; i++)
            {
                int selected = 0;
                float selectedRatio = float.MaxValue;
                for (int j = 0; j < remaining.Count; j++)
                {
                    Unit candidate = remaining[j];
                    float ratio = candidate.MaxHp > 0f ? candidate.CurrentHp / candidate.MaxHp : 0f;
                    if (ratio < selectedRatio)
                    {
                        selectedRatio = ratio;
                        selected = j;
                    }
                }

                result.Add(remaining[selected]);
                remaining.RemoveAt(selected);
            }

            return result;
        }
    }
}
