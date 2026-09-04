using System.Collections.Generic;
using UnityEngine;
using OzGameLab01.Managers;
using OzGameLab01.UI;
using OzGameLab01.UI.Battle;

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

        private const int SlotColumns = 3;
        private const int SlotRows = 3;

        [SerializeField] private Transform unitsRoot;
        [SerializeField] private Vector3 gridOrigin = new Vector3(-4.5f, -1.2f, 0f);
        [SerializeField] private float columnSpacing = 1.2f;
        [SerializeField] private float rowSpacing = 1.2f;
        [SerializeField] private Vector3 enemyPosition = new Vector3(4.5f, 0f, 0f);
        [SerializeField] private string enemyPrefabResourceName = "Characters/Enemy_Melee";
        [SerializeField] private float enemyScale = 3f;

        [Tooltip("모든 아군이 공유하는 프리팹입니다. Instantiate 후 UnitData로 Configure()하여 실제 유닛으로 만듭니다. 프리팹 루트는 비활성 상태여야 합니다(Configure가 Awake보다 먼저 실행되어야 하므로).")]
        [SerializeField] private GameObject allyTemplatePrefab;

        [Tooltip("맵 씬에서 미리 정한 아군 배치. 슬롯(열+행)을 키로, 그 슬롯에 들어갈 유닛 id(GameDB 기준)를 값으로 가짐.")]
        [SerializeField] private List<SlotPlacement> allyFormation = new List<SlotPlacement>();

        [Tooltip("유닛 id별 프리팹/트레이트, 시너지 발동 정의. 로스터 준비 화면과 공유하는 데이터입니다.")]
        [SerializeField] private UnitRosterData rosterData;

        [Header("시너지 UI")]
        [Tooltip("시너지 표시 아이템이 배치될 부모입니다.")]
        [SerializeField] private Transform synergyPanelRoot;
        [Tooltip("시너지 한 개를 표시하는 아이템 원본입니다.")]
        [SerializeField] private SynergyItemView synergyItemTemplate;
        [Tooltip("발동 중인 시너지 아이템 색상입니다.")]
        [SerializeField] private Color synergyActiveColor = Color.white;
        [Tooltip("보유 중이지만 아직 발동하지 않은 시너지 아이템 색상입니다.")]
        [SerializeField] private Color synergyInactiveColor = new Color(1f, 1f, 1f, 0.4f);

        [Header("유닛 정보 UI")]
        [Tooltip("화면 하단에 현재 전투 중인 아군을 표시하는 패널입니다.")]
        [SerializeField] private BattleUnitInfoView battleUnitInfoView;

        [Tooltip("전투에 직접 참여하지 않는 서브 유닛 id 목록. 전투 그리드에는 스폰되지 않고 하단 UI에만 표시됩니다.")]
        [SerializeField] private List<int> supportFormation = new List<int>();

        private Dictionary<SlotKey, int> _allyFormation;
        private Dictionary<SlotKey, int> _spawnedFormation;
        private Dictionary<int, UnitData> _unitDataById;
        private Dictionary<int, List<SynergyTrait>> _unitTraitsById;
        private Dictionary<SynergyTrait, int> _traitCounts;
        private readonly Unit[,] _slotUnits = new Unit[SlotColumns, SlotRows];
        private Unit _enemyUnit;

        public Unit EnemyUnit => _enemyUnit;

        private void Awake()
        {
            Instance = this;
            BuildAllyFormation();
            BuildUnitStatLookup();
            BuildUnitTraitLookup();
            bool spawnedFromPlacement = SpawnAllies();
            ApplySynergies();
            PopulateSynergyPanel();

            // 배치 데이터로 스폰했다면 BattleFormationInfoController가 유닛 정보 패널을
            // 실제 편성 기준으로 채운다. 인스펙터 폴백 편성일 때만 여기서 직접 채운다.
            if (!spawnedFromPlacement)
            {
                PopulateUnitInfoPanel();
            }

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

        private void BuildUnitStatLookup()
        {
            _unitDataById = new Dictionary<int, UnitData>();
            if (rosterData == null)
            {
                return;
            }

            foreach (UnitData data in rosterData.UnitStats)
            {
                _unitDataById[data.id] = data;
            }
        }

        private void BuildUnitTraitLookup()
        {
            _unitTraitsById = new Dictionary<int, List<SynergyTrait>>();
            if (rosterData == null)
            {
                return;
            }

            foreach (UnitRosterData.UnitTraitEntry entry in rosterData.UnitTraits)
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

        /// <summary>
        /// 아군을 스폰합니다. 유닛 편성 화면에서 넘어온 배치 데이터(AllyFormationData)가
        /// 있으면 그것을 사용하고, 없으면 인스펙터에 지정된 allyFormation으로 대체합니다.
        /// 배치 데이터를 사용했는지 여부를 반환합니다(하단 유닛 정보 패널을
        /// BattleFormationInfoController가 채울지, 여기서 채울지 판단하는 데 씁니다).
        /// </summary>
        private bool SpawnAllies()
        {
            _spawnedFormation = new Dictionary<SlotKey, int>();

            UnitData[] placedUnits = SceneTransitioner.AllyFormationData;
            bool hasPlacementData = false;
            if (placedUnits != null)
            {
                foreach (UnitData placedUnit in placedUnits)
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
                return true;
            }

            foreach (KeyValuePair<SlotKey, int> kvp in _allyFormation)
            {
                if (!_unitDataById.TryGetValue(kvp.Value, out UnitData data) || data == null)
                {
                    continue;
                }

                SlotKey slot = kvp.Key;
                _slotUnits[slot.column, (int)slot.row] = SpawnAllyUnit(data, GetSlotPosition(slot.column, slot.row));
                _spawnedFormation[slot] = kvp.Value;
            }

            return false;
        }

        private void SpawnAlliesFromPlacement(UnitData[] placedUnits)
        {
            for (int placementIndex = 0; placementIndex < placedUnits.Length; placementIndex++)
            {
                UnitData data = placedUnits[placementIndex];
                if (data == null)
                {
                    continue;
                }

                SlotKey slot = PlacementIndexToSlotKey(placementIndex);
                _slotUnits[slot.column, (int)slot.row] = SpawnAllyUnit(data, GetSlotPosition(slot.column, slot.row));
                _spawnedFormation[slot] = data.id;
            }
        }

        /// <summary>
        /// 공용 아군 프리팹을 비활성 상태로 Instantiate하여 UnitData로 Configure()한 뒤 활성화합니다.
        /// (Awake()가 Configure()에서 설정한 값을 읽어 초기화하므로 순서가 중요합니다.)
        /// </summary>
        private Unit SpawnAllyUnit(UnitData data, Vector3 position)
        {
            if (allyTemplatePrefab == null)
            {
                Debug.LogError("[CombatManager] allyTemplatePrefab이 연결되지 않았습니다.", this);
                return null;
            }

            GameObject instance = Instantiate(allyTemplatePrefab, position, Quaternion.identity, unitsRoot);
            Unit unit = instance.GetComponent<Unit>();
            unit.Configure(data);
            instance.SetActive(true);
            unit.SetVisualsVisible(false);

            return unit;
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
            // 인스펙터 폴백 편성이 아니라 실제로 스폰된 편성(_spawnedFormation)을 기준으로 삼아야
            // 배치 화면에서 넘어온 편성에도 시너지가 정상 반영된다.
            _traitCounts = new Dictionary<SynergyTrait, int>();
            foreach (int unitId in _spawnedFormation.Values)
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

                    _traitCounts.TryGetValue(trait, out int count);
                    _traitCounts[trait] = count + 1;
                }
            }

            Dictionary<SynergyTrait, SynergyDefinition> definitionByTrait = new Dictionary<SynergyTrait, SynergyDefinition>();
            if (rosterData != null)
            {
                foreach (SynergyDefinition definition in rosterData.SynergyDefinitions)
                {
                    if (definition != null && definition.Trait != null)
                    {
                        definitionByTrait[definition.Trait] = definition;
                    }
                }
            }

            // 발동된 시너지의 보너스는 해당 트레이트를 실제로 보유한 유닛에게만 적용한다.
            foreach (KeyValuePair<SlotKey, int> kvp in _spawnedFormation)
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

                    _traitCounts.TryGetValue(trait, out int count);
                    if (definition.TryGetActiveTier(count, out SynergyDefinition.Tier tier))
                    {
                        unit.ApplySynergyBonus(tier.hpMultiplier, tier.attackMultiplier);
                    }
                }
            }
        }

        /// <summary>
        /// 보유 중인(카운트 1 이상) 시너지를 패널에 표시합니다.
        /// 발동 중인 시너지와 아직 발동하지 않은 시너지를 색상으로 구분합니다.
        /// </summary>
        private void PopulateSynergyPanel()
        {
            if (rosterData == null || synergyPanelRoot == null || synergyItemTemplate == null)
            {
                return;
            }

            for (int i = synergyPanelRoot.childCount - 1; i >= 0; i--)
            {
                Destroy(synergyPanelRoot.GetChild(i).gameObject);
            }

            // 발동 수가 높은 시너지가 먼저 오도록 정렬한다.
            List<SynergyDefinition> sortedDefinitions = new List<SynergyDefinition>(rosterData.SynergyDefinitions);
            sortedDefinitions.Sort((a, b) => GetTraitCount(b).CompareTo(GetTraitCount(a)));

            foreach (SynergyDefinition definition in sortedDefinitions)
            {
                if (definition == null || definition.Trait == null)
                {
                    continue;
                }

                int count = GetTraitCount(definition);
                if (count <= 0)
                {
                    continue;
                }

                bool isActive = definition.TryGetActiveTier(count, out _);
                string stackText = definition.TryGetNextThreshold(count, out int nextThreshold)
                    ? $"{count}/{nextThreshold}"
                    : count.ToString();

                SynergyItemView item = Instantiate(synergyItemTemplate, synergyPanelRoot);
                item.gameObject.SetActive(true);
                item.SetTitle(definition.Trait.DisplayName);
                item.SetStackText(stackText);
                item.SetBackgroundColor(isActive ? synergyActiveColor : synergyInactiveColor);
            }
        }

        private int GetTraitCount(SynergyDefinition definition)
        {
            if (definition == null || definition.Trait == null)
            {
                return 0;
            }

            _traitCounts.TryGetValue(definition.Trait, out int count);
            return count;
        }

        /// <summary>
        /// 화면 하단 유닛 정보 패널에 현재 전투 중인 아군을 표시합니다.
        /// 초상화는 스폰된 유닛의 SpriteRenderer에서, 이름은 UnitData에서 가져옵니다.
        /// </summary>
        private void PopulateUnitInfoPanel()
        {
            if (battleUnitInfoView == null)
            {
                return;
            }

            battleUnitInfoView.ClearUnitInfoItems();

            foreach (KeyValuePair<SlotKey, int> kvp in _spawnedFormation)
            {
                Unit unit = _slotUnits[kvp.Key.column, (int)kvp.Key.row];
                if (unit == null)
                {
                    continue;
                }

                BattleUnitInfoItemView item = battleUnitInfoView.CreateBattleUnitInfoItem();
                if (item == null)
                {
                    continue;
                }

                SpriteRenderer spriteRenderer = unit.GetComponentInChildren<SpriteRenderer>();
                if (spriteRenderer != null)
                {
                    item.SetPortrait(spriteRenderer.sprite);
                }

                if (_unitDataById.TryGetValue(kvp.Value, out UnitData data) && data != null)
                {
                    item.SetUnitName(data.name);
                }
            }

            Sprite allyIconSprite = allyTemplatePrefab != null
                ? allyTemplatePrefab.GetComponentInChildren<SpriteRenderer>(true)?.sprite
                : null;

            foreach (int unitId in supportFormation)
            {
                if (!_unitDataById.TryGetValue(unitId, out UnitData data) || data == null)
                {
                    continue;
                }

                SupportUnitInfoItemView item = battleUnitInfoView.CreateSupportUnitInfoItem();
                if (item == null)
                {
                    continue;
                }

                item.SetPortrait(allyIconSprite);
                if (item.PortraitImage != null)
                {
                    item.PortraitImage.color = data.color;
                }

                item.SetUnitName(data.name);
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
