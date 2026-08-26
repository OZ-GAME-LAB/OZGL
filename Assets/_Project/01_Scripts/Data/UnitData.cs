using System.Collections.Generic;
using UnityEngine;

namespace Combat
{
    [CreateAssetMenu(fileName = "UnitData", menuName = "Combat/Unit Data")]
    public class UnitData : ScriptableObject
    {
        [SerializeField] private GameObject unitPrefab;
        [SerializeField] private List<SynergyTrait> traits = new List<SynergyTrait>();

        public GameObject UnitPrefab => unitPrefab;
        public IReadOnlyList<SynergyTrait> Traits => traits;
    }
}
