using System.Collections.Generic;
using UnityEngine;
using OzGameLab01.Managers;

namespace OzGameLab01.Combat
{
    public class CombatManager : MonoBehaviour
    {
        public static CombatManager Instance;


        public enum SlotRow { Front, Mid, Back }

        [System.Serializable]
        public struct SlotKey : System.IEquatable<SlotKey>
        {
            public int column;
            public SlotRow row;

            public bool Equals(SlotKey other) => column == other.column && row == other.row;
            public override bool Equals(object obj) => obj is SlotKey other && Equals(other);
            public override int GetHashCode() => (column, row).GetHashCode();
        }

        [System.Serializable]
        private struct SlotPlacement
        {
            public SlotKey slot;
            public int unitId;
        }

        [System.Serializable]
        private struct UnitPrefabEntry
        {
            public int id;
            public GameObject prefab;
        }

        [System.Serializable]
        private struct UnitTraitEntry
        {
            public int id;
            public List<SynergyTrait> traits;
        }

        private const int SlotColumns = 3;
        private const int SlotRows = 3;

        [SerializeField] private Transform unitsRoot;
        [SerializeField] private Vector3 gridOrigin = new Vector3(-4.5f, -1.2f, 0f);
        [SerializeField] private float columnSpacing = 1.2f;
        [SerializeField] private float rowSpacing = 1.2f;
        [SerializeField] private float slotMarkerSize = 0.9f;
        [SerializeField] private float slotMarkerLineWidth = 0.03f;
        [SerializeField] private Color slotMarkerColor = new Color(1f, 1f, 1f, 0.25f);
        [SerializeField] private Vector3 enemyPosition = new Vector3(4.5f, 0f, 0f);
        [SerializeField] private string enemyPrefabResourceName = "Characters/Enemy_Melee";
        [SerializeField] private float enemyScale = 3f;

        [Tooltip("맵 씬에서 미리 정한 아군 배치. 슬롯(열+행)을 키로, 그 슬롯에 들어갈 유닛 id(GameDB 기준)를 값으로 가짐.")]
        [SerializeField] private List<SlotPlacement> allyFormation = new List<SlotPlacement>();

        [Tooltip("유닛 id별 스폰 프리팹. GameDB의 UnitData.id와 매칭.")]
        [SerializeField] private List<UnitPrefabEntry> unitPrefabs = new List<UnitPrefabEntry>();

        [Tooltip("유닛 id별 시너지 트레이트. GameDB의 UnitData.id와 매칭.")]
        [SerializeField] private List<UnitTraitEntry> unitTraits = new List<UnitTraitEntry>();

        [Tooltip("아군 배치의 트레이트 조합으로 발동 가능한 시너지 목록.")]
        [SerializeField] private List<SynergyDefinition> synergyDefinitions = new List<SynergyDefinition>();

        private Dictionary<SlotKey, int> _allyFormation;
        private Dictionary<int, GameObject> _unitPrefabsById;
        private Dictionary<int, List<SynergyTrait>> _unitTraitsById;
        private readonly Unit[,] _slotUnits = new Unit[SlotColumns, SlotRows];
        private Unit _enemyUnit;

        public Unit EnemyUnit => _enemyUnit;

        private void Awake()
        {
            Instance = this;
            BuildAllyFormation();
            BuildUnitPrefabLookup();
            BuildUnitTraitLookup();
            SpawnSlotMarkers();
            SpawnAllies();
            ApplySynergies();
            SpawnEnemy();
        }

        private void BuildAllyFormation()
        {
            _allyFormation = new Dictionary<SlotKey, int>();
            foreach (SlotPlacement placement in allyFormation)
            {
                _allyFormation[placement.slot] = placement.unitId;
            }
        }

        private void BuildUnitPrefabLookup()
        {
            _unitPrefabsById = new Dictionary<int, GameObject>();
            foreach (UnitPrefabEntry entry in unitPrefabs)
            {
                _unitPrefabsById[entry.id] = entry.prefab;
            }
        }

        private void BuildUnitTraitLookup()
        {
            _unitTraitsById = new Dictionary<int, List<SynergyTrait>>();
            foreach (UnitTraitEntry entry in unitTraits)
            {
                _unitTraitsById[entry.id] = entry.traits;
            }
        }

        public Unit ResolveAllyTarget()
        {
            List<Unit> exposed = new List<Unit>();

            for (int column = 0; column < SlotColumns; column++)
            {
                Unit front = _slotUnits[column, (int)SlotRow.Front];
                Unit mid = _slotUnits[column, (int)SlotRow.Mid];
                Unit back = _slotUnits[column, (int)SlotRow.Back];

                if (front != null && !front.IsDead)
                {
                    exposed.Add(front);
                }
                else if (mid != null && !mid.IsDead)
                {
                    exposed.Add(mid);
                }
                else if (back != null && !back.IsDead)
                {
                    exposed.Add(back);
                }
            }

            if (exposed.Count == 0)
            {
                return null;
            }

            return exposed[Random.Range(0, exposed.Count)];
        }

        public List<Unit> GetParticipatingAllyUnits()
        {
            List<Unit> units = new List<Unit>();
            foreach (Unit unit in _slotUnits)
            {
                if (unit != null)
                {
                    units.Add(unit);
                }
            }

            return units;
        }

        private void SpawnSlotMarkers()
        {
            for (int column = 0; column < SlotColumns; column++)
            {
                CreateSlotMarker(GetSlotPosition(column, SlotRow.Front));
                CreateSlotMarker(GetSlotPosition(column, SlotRow.Mid));
                CreateSlotMarker(GetSlotPosition(column, SlotRow.Back));
            }
        }

        private void CreateSlotMarker(Vector3 position)
        {
            GameObject marker = new GameObject("SlotMarker");
            marker.transform.SetParent(unitsRoot);
            marker.transform.position = position;

            LineRenderer lineRenderer = marker.AddComponent<LineRenderer>();
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            lineRenderer.useWorldSpace = false;
            lineRenderer.loop = true;
            lineRenderer.positionCount = 4;
            lineRenderer.widthMultiplier = slotMarkerLineWidth;
            lineRenderer.startColor = slotMarkerColor;
            lineRenderer.endColor = slotMarkerColor;
            lineRenderer.sortingOrder = -1;

            float half = slotMarkerSize * 0.5f;
            lineRenderer.SetPosition(0, new Vector3(-half, -half, 0f));
            lineRenderer.SetPosition(1, new Vector3(-half, half, 0f));
            lineRenderer.SetPosition(2, new Vector3(half, half, 0f));
            lineRenderer.SetPosition(3, new Vector3(half, -half, 0f));
        }

        private void SpawnAllies()
        {
            Unit[] placedUnits = SceneTransitioner.AllyFormationSlots;
            bool hasPlacementData = false;
            if (placedUnits != null)
            {
                foreach (Unit placedUnit in placedUnits)
                {
                    if (placedUnit != null)
                    {
                        hasPlacementData = true;
                        break;
                    }
                }
            }

            if (hasPlacementData)
            {
                SpawnAlliesFromPlacement(placedUnits);
                return;
            }

            foreach (KeyValuePair<SlotKey, int> kvp in _allyFormation)
            {
                if (!_unitPrefabsById.TryGetValue(kvp.Value, out GameObject prefab) || prefab == null)
                {
                    continue;
                }

                SlotKey slot = kvp.Key;
                GameObject instance = Instantiate(prefab, GetSlotPosition(slot.column, slot.row), Quaternion.identity, unitsRoot);
                _slotUnits[slot.column, (int)slot.row] = instance.GetComponent<Unit>();
            }
        }

        private void SpawnAlliesFromPlacement(Unit[] placedUnits)
        {
            for (int placementIndex = 0; placementIndex < placedUnits.Length; placementIndex++)
            {
                Unit placedUnit = placedUnits[placementIndex];
                if (placedUnit == null)
                {
                    continue;
                }

                SlotKey slot = PlacementIndexToSlotKey(placementIndex);
                GameObject instance = Instantiate(placedUnit.gameObject, GetSlotPosition(slot.column, slot.row), Quaternion.identity, unitsRoot);
                _slotUnits[slot.column, (int)slot.row] = instance.GetComponent<Unit>();
            }
        }

        // UnitPlaceScene 배치 그리드(인덱스 0-8, row-major: row=idx/3 위→아래, col=idx%3 왼쪽→오른쪽)를
        // CombatManager 슬롯으로 옮긴다. 오른쪽 열=Front, 가운데=Mid, 왼쪽=Back로 취급하고,
        // 배치 UI의 위쪽 행이 CombatManager의 높은 column 값이 되도록 상하 시각 순서를 그대로 보존한다.
        private static SlotKey PlacementIndexToSlotKey(int placementIndex)
        {
            int placeRow = placementIndex / SlotColumns;
            int placeCol = placementIndex % SlotColumns;

            return new SlotKey
            {
                column = (SlotColumns - 1) - placeRow,
                row = (SlotRow)((SlotColumns - 1) - placeCol)
            };
        }

        private void ApplySynergies()
        {
            // 팀 전체에서 각 트레이트를 보유한 유닛 수를 센다 (시너지 발동 여부 판정용).
            Dictionary<SynergyTrait, int> traitCounts = new Dictionary<SynergyTrait, int>();
            foreach (int unitId in _allyFormation.Values)
            {
                if (!_unitTraitsById.TryGetValue(unitId, out List<SynergyTrait> traits) || traits == null)
                {
                    continue;
                }

                foreach (SynergyTrait trait in traits)
                {
                    if (trait == null)
                    {
                        continue;
                    }

                    traitCounts.TryGetValue(trait, out int count);
                    traitCounts[trait] = count + 1;
                }
            }

            Dictionary<SynergyTrait, SynergyDefinition> definitionByTrait = new Dictionary<SynergyTrait, SynergyDefinition>();
            foreach (SynergyDefinition definition in synergyDefinitions)
            {
                if (definition != null && definition.Trait != null)
                {
                    definitionByTrait[definition.Trait] = definition;
                }
            }

            // 발동된 시너지의 보너스는 해당 트레이트를 실제로 보유한 유닛에게만 적용한다.
            foreach (KeyValuePair<SlotKey, int> kvp in _allyFormation)
            {
                Unit unit = _slotUnits[kvp.Key.column, (int)kvp.Key.row];
                if (unit == null || !_unitTraitsById.TryGetValue(kvp.Value, out List<SynergyTrait> traits) || traits == null)
                {
                    continue;
                }

                foreach (SynergyTrait trait in traits)
                {
                    if (trait == null || !definitionByTrait.TryGetValue(trait, out SynergyDefinition definition))
                    {
                        continue;
                    }

                    traitCounts.TryGetValue(trait, out int count);
                    if (definition.TryGetActiveTier(count, out SynergyDefinition.Tier tier))
                    {
                        unit.ApplySynergyBonus(tier.hpMultiplier, tier.attackMultiplier);
                    }
                }
            }
        }

        private void SpawnEnemy()
        {
            GameObject prefab = Resources.Load<GameObject>(enemyPrefabResourceName);
            if (prefab == null)
            {
                return;
            }

            GameObject enemySlot = new GameObject("EnemySlot");
            enemySlot.transform.SetParent(unitsRoot);
            enemySlot.transform.position = enemyPosition;
            enemySlot.transform.localScale = Vector3.one * enemyScale;

            GameObject enemyInstance = Instantiate(prefab, enemySlot.transform);
            enemyInstance.transform.localPosition = Vector3.zero;
            enemyInstance.transform.localRotation = Quaternion.identity;
            _enemyUnit = enemyInstance.GetComponent<Unit>();
        }

        private Vector3 GetSlotPosition(int column, SlotRow row)
        {
            // Front (closest to the enemy on the right) sits at the highest local X; Back sits at gridOrigin.
            float localX = (SlotRows - 1 - (int)row) * rowSpacing;
            float localY = column * columnSpacing;
            return gridOrigin + new Vector3(localX, localY, 0f);
        }
    }
}
