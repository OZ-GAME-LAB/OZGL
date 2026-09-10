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

        [SerializeField] private string displayName;
        [SerializeField] private List<Tier> tiers = new List<Tier>();

        public string DisplayName => displayName;

        public bool ValidateConfiguration(UnityEngine.Object context = null)
        {
            bool valid = true;
            if (string.IsNullOrWhiteSpace(displayName))
            {
                Debug.LogError($"[SynergyDefinition] 표시 이름이 비어 있습니다: {name}", context);
                return false;
            }

            if (tiers == null || tiers.Count == 0)
            {
                Debug.LogError($"[SynergyDefinition] 시너지 단계가 비어 있습니다: {name}", context);
                return false;
            }

            int previousRequiredCount = 0;
            foreach (Tier tier in tiers)
            {
                if (tier.requiredCount <= previousRequiredCount || tier.hpMultiplier <= 0f || tier.attackMultiplier <= 0f)
                {
                    Debug.LogError($"[SynergyDefinition] 단계 설정이 올바르지 않습니다: {name}", context);
                    valid = false;
                }

                previousRequiredCount = tier.requiredCount;
            }

            return valid;
        }

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
