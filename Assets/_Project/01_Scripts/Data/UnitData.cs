using UnityEngine;

namespace Combat
{
    [CreateAssetMenu(fileName = "UnitData", menuName = "Combat/Unit Data")]
    public class UnitData : ScriptableObject
    {
        [SerializeField] private GameObject unitPrefab;

        public GameObject UnitPrefab => unitPrefab;
    }
}
