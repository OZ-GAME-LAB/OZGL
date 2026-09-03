using System.Collections.Generic;
using UnityEngine;

namespace OzGameLab01.Combat
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

        /// <summary>
        /// 다음 티어가 발동되는 데 필요한 보유 수를 반환합니다.
        /// 이미 최고 티어에 도달했다면 false를 반환합니다.
        /// </summary>
        public bool TryGetNextThreshold(int unitCount, out int nextThreshold)
        {
            bool found = false;
            nextThreshold = int.MaxValue;

            foreach (Tier tier in tiers)
            {
                if (tier.requiredCount > unitCount && tier.requiredCount < nextThreshold)
                {
                    nextThreshold = tier.requiredCount;
                    found = true;
                }
            }

            return found;
        }
    }
}
