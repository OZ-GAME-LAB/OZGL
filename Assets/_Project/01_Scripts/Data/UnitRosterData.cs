using System.Collections.Generic;
using UnityEngine;

namespace OzGameLab01.Combat
{
    /// <summary>
    /// 유닛 id별 프리팹/시너지 트레이트와 시너지 발동 정의를 담은 공유 데이터입니다.
    ///
    /// CombatManager(전투 씬)와 로스터 준비 화면이 같은 데이터를 참조하도록
    /// 별도 ScriptableObject로 분리했습니다. 씬마다 따로 채우던 방식은
    /// 한쪽만 갱신했을 때 어긋나는 문제가 있어 이 asset 하나로 통일합니다.
    /// </summary>
    [CreateAssetMenu(fileName = "UnitRosterData", menuName = "Combat/Unit Roster Data")]
    public class UnitRosterData : ScriptableObject
    {
        [System.Serializable]
        public struct UnitPrefabEntry
        {
            public int id;
            public GameObject prefab;
        }

        [System.Serializable]
        public struct UnitTraitEntry
        {
            public int id;
            public List<SynergyTrait> traits;
        }

        [Tooltip("유닛 id별 스폰 프리팹. GameDB의 UnitData.id와 매칭.")]
        [SerializeField] private List<UnitPrefabEntry> unitPrefabs = new List<UnitPrefabEntry>();

        [Tooltip("유닛 id별 시너지 트레이트. GameDB의 UnitData.id와 매칭.")]
        [SerializeField] private List<UnitTraitEntry> unitTraits = new List<UnitTraitEntry>();

        [Tooltip("트레이트 조합으로 발동 가능한 시너지 목록.")]
        [SerializeField] private List<SynergyDefinition> synergyDefinitions = new List<SynergyDefinition>();

        public IReadOnlyList<UnitPrefabEntry> UnitPrefabs => unitPrefabs;
        public IReadOnlyList<UnitTraitEntry> UnitTraits => unitTraits;
        public IReadOnlyList<SynergyDefinition> SynergyDefinitions => synergyDefinitions;
    }
}
