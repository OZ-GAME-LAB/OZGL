using UnityEngine;

namespace OzGameLab01.Combat
{
    [CreateAssetMenu(fileName = "SynergyTrait", menuName = "Combat/Synergy Trait")]
    public class SynergyTrait : ScriptableObject
    {
        [SerializeField] private string displayName;

        public string DisplayName => displayName;
    }
}
