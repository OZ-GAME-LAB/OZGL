using System.Collections.Generic;
using UnityEngine;

namespace Combat
{
    [CreateAssetMenu(fileName = "SynergyDefinition", menuName = "Combat/Synergy Definition")]
    public class SynergyDefinition : ScriptableObject
    {
        [System.Serializable]
        public struct Tier
        {
            public int requiredCount;
            public float hpMultiplier;
            public float attackMultiplier;
        }

        [SerializeField] private SynergyTrait trait;
        [SerializeField] private List<Tier> tiers = new List<Tier>();

        public SynergyTrait Trait => trait;

        public bool TryGetActiveTier(int unitCount, out Tier activeTier)
        {
            bool found = false;
            activeTier = default;

            foreach (Tier tier in tiers)
            {
                if (unitCount >= tier.requiredCount && (!found || tier.requiredCount > activeTier.requiredCount))
                {
                    activeTier = tier;
                    found = true;
                }
            }

            return found;
        }
    }
}
