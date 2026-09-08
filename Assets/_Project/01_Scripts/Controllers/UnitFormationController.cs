using System.Collections.Generic;
using OzGameLab01.Combat;
using OzGameLab01.Managers;
using OzGameLab01.UI;
using UnityEngine;
using UnityEngine.EventSystems;

namespace OzGameLab01.Controllers
{
    /// <summary>
    /// 유닛 데이터와 유닛 배치 화면을 연결하고,
    /// 전투 및 서브 유닛 편성을 관리합니다.
    /// </summary>
    public class UnitFormationController : MonoBehaviour
    {
        [System.Serializable]
        private struct UnitIconEntry
        {
            public int unitId;
            public Sprite icon;
        }

        /// <summary>
        /// 전투 슬롯 또는 서브 슬롯 한 그룹의 배치 상태(데이터/아이템/인원수)를 담습니다.
        /// 배틀/서포트 슬롯이 거의 동일한 배치·이동·교체 로직을 각자 복붙해서 갖고 있던 것을
        /// 이 타입 하나로 통합해 정리합니다.
        /// </summary>
        private sealed class SlotGroup
        {
            public readonly UnitSlotType SlotType;
            public readonly int MaxCount;
            public readonly UnitData[] Data;
            public readonly UnitItemView[] Items;
            public int Count;

            public SlotGroup(UnitSlotType slotType, int slotCount, int maxCount)
            {
                SlotType = slotType;
                MaxCount = maxCount;
                Data = new UnitData[slotCount];
                Items = new UnitItemView[slotCount];
            }
        }

        private const int BattleSlotCount = 9;
        private const int MaxBattleUnitCount = 4;
        private const int SupportSlotCount = 2;

        private static readonly Vector2 UnitListItemSize = new Vector2(80f, 80f);

        [Header("배치 화면")]
        [SerializeField]
        [Tooltip("유닛 배치 화면")]
        private UnitView unitView;

        [SerializeField]
        [Tooltip("보유 유닛 목록 생성에 사용할 원본 아이템")]
        private UnitItemView unitItemTemplate;

        [SerializeField]
        [Tooltip("보유 유닛 목록의 원본 데이터. CombatManager와 동일한 id 체계를 공유합니다.")]
        private UnitRosterData rosterData;

        [SerializeField]
        [Tooltip("보유 유닛 아이콘에 쓰이는 공용 스프라이트. 모든 아군이 같은 스프라이트를 색상만 다르게 사용합니다.")]
        private Sprite unitIconSprite;

        [SerializeField]
        private List<UnitIconEntry> unitIcons = new List<UnitIconEntry>();

        [Header("시너지 UI")]
        [SerializeField]
        [Tooltip("시너지 한 개를 표시하는 아이템 원본입니다. UnitView.SynergyContentRoot의 비활성 자식(템플릿)을 그대로 연결합니다.")]
        private SynergyItemView synergyItemTemplate;

        [SerializeField]
        [Tooltip("발동 중인 시너지 아이템 색상입니다.")]
        private Color synergyActiveColor = Color.white;

        [SerializeField]
        [Tooltip("보유 중이지만 아직 발동하지 않은 시너지 아이템 색상입니다.")]
        private Color synergyInactiveColor = new Color(1f, 1f, 1f, 0.4f);

        private readonly List<UnitData> testUnitDataList = new List<UnitData>();

        private readonly Dictionary<UnitItemView, UnitData> unitDataByItem = new Dictionary<UnitItemView, UnitData>();

        private readonly SlotGroup battleGroup = new SlotGroup(UnitSlotType.Battle, BattleSlotCount, MaxBattleUnitCount);

        private readonly SlotGroup supportGroup = new SlotGroup(UnitSlotType.Support, SupportSlotCount, SupportSlotCount);

        private UnitItemView draggingUnitItem;
        private Transform dragOriginParent;
        private int dragOriginSiblingIndex = -1;
        private bool dragDropHandled;

        private UnitFormationCombatLink formationCombatLink;

        private Dictionary<int, List<SynergyTrait>> unitTraitsById;

        /// <summary>
        /// 현재 배치 화면에 연결된 테스트 유닛 데이터를 반환합니다.
        /// </summary>
        public IReadOnlyList<UnitData> TestUnitDataList => testUnitDataList;

        /// <summary>
        /// 3×3 전투 슬롯에 배치된 유닛 데이터를 반환합니다.
        /// 인덱스는 전투 슬롯의 0~8 위치와 일치합니다.
        /// </summary>
        public IReadOnlyList<UnitData> BattleUnitData => battleGroup.Data;

        /// <summary>
        /// 서브 슬롯에 배치된 유닛 데이터를 반환합니다.
        /// 인덱스는 서브 슬롯의 0~1 위치와 일치합니다.
        /// </summary>
        public IReadOnlyList<UnitData> SupportUnitData => supportGroup.Data;

        /// <summary>
        /// 현재 전투 슬롯에 배치된 유닛 수를 반환합니다.
        /// </summary>
        public int BattleUnitCount => battleGroup.Count;

        /// <summary>
        /// 현재 서브 슬롯에 배치된 유닛 수를 반환합니다.
        /// </summary>
        public int SupportUnitCount => supportGroup.Count;

        // [수정]비활성 UnitView가 아직 초기화되지 않았어도 저장된 전투 편성으로 진입을 허용합니다.
        public bool CanStartBattle => battleGroup.Count >= 1 || UnitFormationCombatLink.HasSavedBattleUnit;

        private void Awake()
        {
            formationCombatLink = GetComponent<UnitFormationCombatLink>();
        }

        private void OnEnable()
        {
            SubscribeViewEvents();
        }

        private void Start()
        {
            LoadRosterUnitData();
            BuildUnitTraitLookup();
            CreateUnitItems();
            RestorePersistedFormation();
            UpdateUnitCount();

            if (Managers.PlayerInventoryManager.Instance != null)
            {
                Managers.PlayerInventoryManager.Instance.OnUnitAdded += AddNewUnitItem;
            }
        }

        private void OnDisable()
        {
            UnsubscribeViewEvents();
            ClearDragState();
        }

        private void OnDestroy()
        {
            if (Managers.PlayerInventoryManager.Instance != null)
            {
                Managers.PlayerInventoryManager.Instance.OnUnitAdded -= AddNewUnitItem;
            }
        }

        /// <summary>
        /// 유닛 배치 화면의 입력 이벤트를 구독합니다.
        /// </summary>
        private void SubscribeViewEvents()
        {
            if (unitView == null)
            {
                return;
            }

            unitView.UnitClicked += HandleUnitClicked;
            unitView.UnitBeginDragged += HandleUnitBeginDragged;
            unitView.UnitDragged += HandleUnitDragged;
            unitView.UnitEndDragged += HandleUnitEndDragged;
            unitView.SlotDropped += HandleSlotDropped;
        }

        /// <summary>
        /// 유닛 배치 화면의 입력 이벤트 구독을 해제합니다.
        /// </summary>
        private void UnsubscribeViewEvents()
        {
            if (unitView == null)
            {
                return;
            }

            unitView.UnitClicked -= HandleUnitClicked;
            unitView.UnitBeginDragged -= HandleUnitBeginDragged;
            unitView.UnitDragged -= HandleUnitDragged;
            unitView.UnitEndDragged -= HandleUnitEndDragged;
            unitView.SlotDropped -= HandleSlotDropped;
        }

        /// <summary>
        /// UnitRosterData(CombatManager와 공유하는 id 체계)를 기준으로 보유 유닛 데이터를 생성합니다.
        /// </summary>
        private void LoadRosterUnitData()
        {
            testUnitDataList.Clear();
            // [수정됨] 이제 씬 전환 시에도 파괴되지 않는 전역 인벤토리에서 유닛 목록을 가져옵니다!
            if (Managers.PlayerInventoryManager.Instance != null)
            {
                foreach (UnitData source in Managers.PlayerInventoryManager.Instance.OwnedUnits)
                {
                    if (source == null)
                    {
                        continue;
                    }

                    // 누락된 필드까지 포함한 로스터 원본 데이터를 id 기준으로 복제합니다.
                    testUnitDataList.Add(CloneCanonicalUnitData(source));
                }
            }
            else
            {
                Debug.LogWarning("[UnitFormationController] PlayerInventoryManager를 씬에서 찾을 수 없습니다. (매니저 오브젝트를 생성해주세요!)", this);
            }
        }

        /// <summary>
        /// CombatManager와 공유하는 유닛 id별 시너지 트레이트를 읽어 둡니다.
        /// </summary>
        private void BuildUnitTraitLookup()
        {
            unitTraitsById = new Dictionary<int, List<SynergyTrait>>();

            if (rosterData == null)
            {
                return;
            }

            UnitRosterData.RegisterActive(rosterData, this);

            foreach (UnitRosterData.UnitTraitEntry entry in rosterData.UnitTraits)
            {
                unitTraitsById[entry.id] = entry.traits;
            }
        }

        /// <summary>
        /// 보유 유닛 데이터에 대응하는 보유 유닛 아이템을 생성합니다.
        /// </summary>
        private void CreateUnitItems()
        {
            if (unitView == null)
            {
                //Debug.LogError("[UnitFormationController] UnitView가 연결되지 않았습니다.", this);
                return;
            }

            if (unitItemTemplate == null)
            {
                //Debug.LogError("[UnitFormationController] 유닛 아이템 원본이 연결되지 않았습니다.", this);
                return;
            }

            unitDataByItem.Clear();
            unitItemTemplate.gameObject.SetActive(false);

            for (int i = 0; i < testUnitDataList.Count; i++)
            {
                UnitItemView unitItem = Instantiate(unitItemTemplate, unitView.UnitContentRoot);

                unitItem.name = $"Unit_Item_{i + 1:00}";

                // UnitData.id에 연결된 PrivateAssets 아이콘을 우선 사용
                unitItem.SetIcon(GetUnitIcon(testUnitDataList[i]));
                unitItem.SetIconColor(testUnitDataList[i].color);
                unitItem.SetSelected(false);
                unitItem.gameObject.SetActive(true);

                unitDataByItem.Add(unitItem, testUnitDataList[i]);

                unitView.RegisterUnitItem(unitItem);
            }
        }

        /// <summary>
        /// 저장된 인벤토리 객체가 일부 필드를 잃었더라도 id 기준 로스터 원본으로 UI/전투 데이터를 복원합니다.
        /// </summary>
        private UnitData CloneCanonicalUnitData(UnitData ownedUnit)
        {
            if (ownedUnit == null)
            {
                return null;
            }

            if (rosterData != null)
            {
                foreach (UnitData rosterUnit in rosterData.UnitStats)
                {
                    if (rosterUnit != null && rosterUnit.id == ownedUnit.id)
                    {
                        return PlayerInventoryManager.CloneUnitData(rosterUnit);
                    }
                }
            }

            // 로스터에 없는 런타임 유닛은 기존 인벤토리 데이터를 복사해 유지
            return PlayerInventoryManager.CloneUnitData(ownedUnit);
        }

        /// <summary>
        /// 씬이 교체되어 UnitFormationController가 다시 생성되어도 정적 전달 데이터에서 UI 배치를 복원합니다.
        /// </summary>
        private void RestorePersistedFormation()
        {
            if (unitView == null)
            {
                return;
            }

            bool restoredAnyUnit = false;
            IReadOnlyList<UnitFormationCombatLink.TransferredUnit> savedBattleUnits =
                UnitFormationCombatLink.BattleUnits;

            for (int slotIndex = 0; slotIndex < BattleSlotCount; slotIndex++)
            {
                // 저장된 슬롯 ID를 우선 사용하고 구버전 전달 데이터는 폴백으로 유지
                int savedUnitId = UnitFormationCombatLink.HasSavedFormation
                    ? UnitFormationCombatLink.SavedBattleUnitIds[slotIndex]
                    : savedBattleUnits[slotIndex]?.Data?.id ?? -1;
                UnitData savedData = savedBattleUnits[slotIndex]?.Data;
                if (savedData == null && SceneTransitioner.AllyFormationData != null &&
                    slotIndex < SceneTransitioner.AllyFormationData.Length)
                {
                    savedData = SceneTransitioner.AllyFormationData[slotIndex];
                }

                UnitItemView unitItem = savedUnitId >= 0
                    ? FindUnplacedUnitItemById(savedUnitId)
                    : FindUnplacedUnitItem(savedData);
                UnitSlotItemView slot = FindSlot(UnitSlotType.Battle, slotIndex);
                if (unitItem != null && slot != null && PlaceUnitInEmptySlot(battleGroup, unitItem, unitDataByItem[unitItem], slot))
                {
                    restoredAnyUnit = true;
                }
            }

            IReadOnlyList<UnitFormationCombatLink.TransferredUnit> savedSupportUnits =
                UnitFormationCombatLink.SupportUnits;

            for (int slotIndex = 0; slotIndex < SupportSlotCount; slotIndex++)
            {
                UnitData savedData = savedSupportUnits[slotIndex]?.Data;
                int savedUnitId = UnitFormationCombatLink.HasSavedFormation
                    ? UnitFormationCombatLink.SavedSupportUnitIds[slotIndex]
                    : savedData?.id ?? -1;
                UnitItemView unitItem = savedUnitId >= 0
                    ? FindUnplacedUnitItemById(savedUnitId)
                    : FindUnplacedUnitItem(savedData);
                UnitSlotItemView slot = FindSlot(UnitSlotType.Support, slotIndex);
                if (unitItem != null && slot != null && PlaceUnitInEmptySlot(supportGroup, unitItem, unitDataByItem[unitItem], slot))
                {
                    restoredAnyUnit = true;
                }
            }

            if (restoredAnyUnit)
            {
                // 새 씬에서 생성된 UnitItemView와 아이콘 참조까지 최신 전달 데이터로 다시 저장
                SaveFormation();
                Debug.Log("[UnitFormationController] 이전 전투 및 서브 유닛 배치를 복원했습니다.", this);
            }
        }

        /// <summary>
        /// 동일 id 유닛이 여러 개여도 아직 어느 슬롯에도 놓이지 않은 UI 아이템을 하나씩 찾습니다.
        /// </summary>
        private UnitItemView FindUnplacedUnitItem(UnitData savedData)
        {
            if (savedData == null)
            {
                return null;
            }

            foreach (KeyValuePair<UnitItemView, UnitData> pair in unitDataByItem)
            {
                if (pair.Key == null || pair.Value == null || pair.Value.id != savedData.id)
                {
                    continue;
                }

                if (FindSlotIndex(battleGroup, pair.Key) < 0 && FindSlotIndex(supportGroup, pair.Key) < 0)
                {
                    return pair.Key;
                }
            }

            return null;
        }

        /// <summary>
        /// 저장된 UnitData 참조 대신 불변 ID로 현재 인벤토리의 미배치 아이템을 찾습니다.
        /// </summary>
        private UnitItemView FindUnplacedUnitItemById(int unitId)
        {
            foreach (KeyValuePair<UnitItemView, UnitData> pair in unitDataByItem)
            {
                if (pair.Key != null && pair.Value != null && pair.Value.id == unitId &&
                    FindSlotIndex(battleGroup, pair.Key) < 0 && FindSlotIndex(supportGroup, pair.Key) < 0)
                {
                    return pair.Key;
                }
            }

            return null;
        }

        /// <summary>
        /// 유닛을 우클릭하면 배치하거나 보유 목록으로 되돌립니다.
        /// </summary>
        private void HandleUnitClicked(UnitItemView unitItem, PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Right)
            {
                return;
            }

            int battleSlotIndex = FindSlotIndex(battleGroup, unitItem);

            if (battleSlotIndex >= 0)
            {
                if (RemoveUnit(battleGroup, battleSlotIndex))
                {
                    SaveFormation();
                }

                return;
            }

            int supportSlotIndex = FindSlotIndex(supportGroup, unitItem);

            if (supportSlotIndex >= 0)
            {
                if (RemoveUnit(supportGroup, supportSlotIndex))
                {
                    SaveFormation();
                }

                return;
            }

            if (PlaceUnitInFirstEmptySlot(battleGroup, unitItem))
            {
                SaveFormation();
            }
        }

        /// <summary>
        /// 유닛 아이템 드래그를 시작합니다.
        /// </summary>
        private void HandleUnitBeginDragged(UnitItemView unitItem, PointerEventData eventData)
        {
            if (unitItem == null)
            {
                return;
            }

            draggingUnitItem = unitItem;
            dragOriginParent = unitItem.transform.parent;
            dragOriginSiblingIndex = unitItem.transform.GetSiblingIndex();

            dragDropHandled = false;

            RectTransform unitRect = unitItem.RectTransform;

            unitRect.SetParent(unitView.transform, true);

            unitRect.SetAsLastSibling();
            unitRect.position = eventData.position;
        }

        /// <summary>
        /// 드래그 중인 유닛 아이템을 마우스 위치로 이동합니다.
        /// </summary>
        private void HandleUnitDragged(UnitItemView unitItem, PointerEventData eventData)
        {
            if (unitItem == null || unitItem != draggingUnitItem)
            {
                return;
            }

            unitItem.RectTransform.position = eventData.position;
        }

        /// <summary>
        /// 드롭 결과에 따라 유닛 아이템 위치를 확정하거나 복구합니다.
        /// </summary>
        private void HandleUnitEndDragged(UnitItemView unitItem, PointerEventData eventData)
        {
            if (unitItem == null || unitItem != draggingUnitItem)
            {
                return;
            }

            if (!dragDropHandled)
            {
                ReturnDraggedUnitToOrigin();
            }

            ClearDragState();
        }

        /// <summary>
        /// 드래그한 유닛을 슬롯 종류에 맞게 배치합니다.
        /// </summary>
        private void HandleSlotDropped(UnitSlotItemView targetSlot, PointerEventData eventData)
        {
            if (draggingUnitItem == null || targetSlot == null)
            {
                return;
            }

            if (targetSlot.IsBattleSlot)
            {
                dragDropHandled = TryDropOnSlot(draggingUnitItem, targetSlot, battleGroup, supportGroup);
            }
            else if (targetSlot.IsSupportSlot)
            {
                dragDropHandled = TryDropOnSlot(draggingUnitItem, targetSlot, supportGroup, battleGroup);
            }

            if (dragDropHandled)
            {
                SaveFormation();
            }
        }

        /// <summary>
        /// 드래그한 유닛을 지정한 슬롯 그룹(전투 또는 서브)에 배치합니다.
        /// otherGroup은 반대쪽 그룹으로, 이미 그쪽에 배치된 유닛은 여기로 옮길 수 없도록 막는 데 씁니다.
        /// </summary>
        private bool TryDropOnSlot(UnitItemView unitItem, UnitSlotItemView targetSlot, SlotGroup ownGroup, SlotGroup otherGroup)
        {
            int targetIndex = targetSlot.SlotIndex;

            if (!IsValidSlot(ownGroup, targetIndex))
            {
                return false;
            }

            if (FindSlotIndex(otherGroup, unitItem) >= 0)
            {
                return false;
            }

            if (!unitDataByItem.TryGetValue(unitItem, out UnitData draggedUnitData))
            {
                return false;
            }

            int sourceIndex = FindSlotIndex(ownGroup, unitItem);

            UnitItemView targetUnitItem = ownGroup.Items[targetIndex];

            UnitData targetUnitData = ownGroup.Data[targetIndex];

            if (sourceIndex == targetIndex)
            {
                MoveUnitItemToSlot(unitItem, targetSlot);

                return true;
            }

            if (sourceIndex < 0 && targetUnitItem == null)
            {
                return PlaceUnitInEmptySlot(ownGroup, unitItem, draggedUnitData, targetSlot);
            }

            if (sourceIndex >= 0 && targetUnitItem == null)
            {
                return MoveUnitToEmptySlot(ownGroup, unitItem, draggedUnitData, sourceIndex, targetSlot);
            }

            if (sourceIndex < 0 && targetUnitItem != null)
            {
                return ReplaceUnit(ownGroup, unitItem, draggedUnitData, targetUnitItem, targetIndex, targetSlot);
            }

            if (sourceIndex >= 0 && targetUnitItem != null)
            {
                return SwapUnits(ownGroup, unitItem, draggedUnitData, targetUnitItem, targetUnitData, sourceIndex, targetSlot);
            }

            return false;
        }

        /// <summary>
        /// 보유 유닛을 지정한 그룹의 비어 있는 슬롯에 배치합니다.
        /// </summary>
        private bool PlaceUnitInEmptySlot(SlotGroup group, UnitItemView unitItem, UnitData unitData, UnitSlotItemView targetSlot)
        {
            if (group.Count >= group.MaxCount)
            {
                Debug.LogWarning($"[UnitFormationController] {GroupLabel(group)} 유닛은 최대 {group.MaxCount}명까지 배치할 수 있습니다.", this);

                return false;
            }

            int targetIndex = targetSlot.SlotIndex;

            group.Items[targetIndex] = unitItem;
            group.Data[targetIndex] = unitData;
            group.Count++;

            targetSlot.SetOccupied(true);
            MoveUnitItemToSlot(unitItem, targetSlot);
            UpdateUnitCount();

            return true;
        }

        /// <summary>
        /// 같은 그룹 안에서 유닛을 비어 있는 다른 슬롯으로 이동합니다.
        /// </summary>
        private bool MoveUnitToEmptySlot(SlotGroup group, UnitItemView unitItem, UnitData unitData, int sourceIndex, UnitSlotItemView targetSlot)
        {
            UnitSlotItemView sourceSlot = FindSlot(group.SlotType, sourceIndex);

            int targetIndex = targetSlot.SlotIndex;

            group.Items[sourceIndex] = null;
            group.Data[sourceIndex] = null;

            group.Items[targetIndex] = unitItem;
            group.Data[targetIndex] = unitData;

            if (sourceSlot != null)
            {
                sourceSlot.SetOccupied(false);
            }

            targetSlot.SetOccupied(true);
            MoveUnitItemToSlot(unitItem, targetSlot);

            return true;
        }

        /// <summary>
        /// 그룹 안의 기존 슬롯 유닛을 보유 목록 유닛으로 교체합니다.
        /// </summary>
        private bool ReplaceUnit(SlotGroup group, UnitItemView unitItem, UnitData unitData, UnitItemView targetUnitItem, int targetIndex, UnitSlotItemView targetSlot)
        {
            MoveUnitItemToList(targetUnitItem);

            group.Items[targetIndex] = unitItem;
            group.Data[targetIndex] = unitData;

            targetSlot.SetOccupied(true);
            MoveUnitItemToSlot(unitItem, targetSlot);
            UpdateUnitCount();

            return true;
        }

        /// <summary>
        /// 같은 그룹 안의 두 슬롯에 배치된 유닛의 위치를 교체합니다.
        /// </summary>
        private bool SwapUnits(SlotGroup group, UnitItemView unitItem, UnitData unitData, UnitItemView targetUnitItem, UnitData targetUnitData, int sourceIndex, UnitSlotItemView targetSlot)
        {
            UnitSlotItemView sourceSlot = FindSlot(group.SlotType, sourceIndex);

            if (sourceSlot == null)
            {
                return false;
            }

            int targetIndex = targetSlot.SlotIndex;

            group.Items[sourceIndex] = targetUnitItem;
            group.Data[sourceIndex] = targetUnitData;

            group.Items[targetIndex] = unitItem;
            group.Data[targetIndex] = unitData;

            MoveUnitItemToSlot(targetUnitItem, sourceSlot);
            MoveUnitItemToSlot(unitItem, targetSlot);

            sourceSlot.SetOccupied(true);
            targetSlot.SetOccupied(true);

            return true;
        }

        /// <summary>
        /// 지정한 그룹에서 비어 있는 슬롯을 앞에서부터 찾아 유닛을 배치합니다.
        /// </summary>
        private bool PlaceUnitInFirstEmptySlot(SlotGroup group, UnitItemView unitItem)
        {
            if (unitItem == null)
            {
                return false;
            }

            if (group.Count >= group.MaxCount)
            {
                Debug.LogWarning($"[UnitFormationController] {GroupLabel(group)} 유닛은 최대 {group.MaxCount}명까지 배치할 수 있습니다.", this);

                return false;
            }

            if (!unitDataByItem.TryGetValue(unitItem, out UnitData unitData))
            {
                return false;
            }

            for (int slotIndex = 0; slotIndex < group.Data.Length; slotIndex++)
            {
                if (group.Data[slotIndex] != null)
                {
                    continue;
                }

                UnitSlotItemView slotItem = FindSlot(group.SlotType, slotIndex);

                if (slotItem == null)
                {
                    Debug.LogWarning($"[UnitFormationController] {GroupLabel(group)} 슬롯 {slotIndex}번을 찾을 수 없습니다.", this);

                    return false;
                }

                return PlaceUnitInEmptySlot(group, unitItem, unitData, slotItem);
            }

            return false;
        }

        /// <summary>
        /// 지정한 그룹의 슬롯에 배치된 유닛을 보유 목록으로 되돌립니다.
        /// </summary>
        private bool RemoveUnit(SlotGroup group, int slotIndex)
        {
            if (!IsValidSlot(group, slotIndex))
            {
                return false;
            }

            UnitItemView unitItem = group.Items[slotIndex];

            if (unitItem == null)
            {
                return false;
            }

            UnitSlotItemView slotItem = FindSlot(group.SlotType, slotIndex);

            group.Items[slotIndex] = null;
            group.Data[slotIndex] = null;
            group.Count--;

            MoveUnitItemToList(unitItem);

            if (slotItem != null)
            {
                slotItem.SetOccupied(false);
            }

            UpdateUnitCount();
            return true;
        }

        /// <summary>
        /// 유닛 아이템을 지정한 슬롯 중앙으로 이동합니다.
        /// </summary>
        private void MoveUnitItemToSlot(UnitItemView unitItem, UnitSlotItemView slotItem)
        {
            if (unitItem == null || slotItem == null)
            {
                return;
            }

            // Slot_00 원본 프리팹의 참조가 비어 있어도 각 오브젝트 자신의 RectTransform을 사용
            RectTransform unitRect = unitItem.RectTransform != null
                ? unitItem.RectTransform
                : unitItem.transform as RectTransform;

            RectTransform slotRect = slotItem.RectTransform != null
                ? slotItem.RectTransform
                : slotItem.transform as RectTransform;

            if (unitRect == null || slotRect == null)
            {
                return;
            }

            unitRect.SetParent(slotRect, false);
            // 슬롯 크기를 복사하지 않고 부모 슬롯 전체에 stretch하여 레이아웃 계산 이후에도 자동으로 맞춤
            unitRect.anchorMin = Vector2.zero;
            unitRect.anchorMax = Vector2.one;
            unitRect.pivot = new Vector2(0.5f, 0.5f);
            unitRect.anchoredPosition = Vector2.zero;
            unitRect.offsetMin = Vector2.zero;
            unitRect.offsetMax = Vector2.zero;
            unitRect.localScale = Vector3.one;
        }

        /// <summary>
        /// 유닛 아이템을 보유 유닛 목록으로 이동합니다.
        /// </summary>
        private void MoveUnitItemToList(UnitItemView unitItem)
        {
            if (unitItem == null || unitView == null)
            {
                return;
            }

            // Inspector 참조가 비어 있는 UnitItem도 자신의 RectTransform으로 목록에 복귀
            RectTransform unitRect = unitItem.RectTransform != null
                ? unitItem.RectTransform
                : unitItem.transform as RectTransform;

            if (unitRect == null)
            {
                return;
            }

            unitRect.SetParent(unitView.UnitContentRoot, false);

            unitRect.anchorMin = new Vector2(0f, 1f);
            unitRect.anchorMax = new Vector2(0f, 1f);
            unitRect.pivot = new Vector2(0.5f, 0.5f);
            unitRect.sizeDelta = UnitListItemSize;
            unitRect.localScale = Vector3.one;
            unitRect.SetAsLastSibling();
        }

        /// <summary>
        /// 드래그한 유닛 아이템을 원래 위치로 되돌립니다.
        /// </summary>
        private void ReturnDraggedUnitToOrigin()
        {
            if (draggingUnitItem == null || dragOriginParent == null)
            {
                return;
            }

            UnitSlotItemView originSlot = dragOriginParent.GetComponent<UnitSlotItemView>();

            if (originSlot != null)
            {
                MoveUnitItemToSlot(draggingUnitItem, originSlot);

                return;
            }

            RectTransform unitRect = draggingUnitItem.RectTransform;

            unitRect.SetParent(dragOriginParent, false);

            unitRect.anchorMin = new Vector2(0f, 1f);
            unitRect.anchorMax = new Vector2(0f, 1f);
            unitRect.pivot = new Vector2(0.5f, 0.5f);
            unitRect.sizeDelta = UnitListItemSize;
            unitRect.localScale = Vector3.one;

            unitRect.SetSiblingIndex(dragOriginSiblingIndex);
        }

        /// <summary>
        /// 유닛 아이템이 지정한 그룹의 어느 슬롯에 배치되어 있는지 찾습니다.
        /// </summary>
        private static int FindSlotIndex(SlotGroup group, UnitItemView unitItem)
        {
            for (int i = 0; i < group.Items.Length; i++)
            {
                if (group.Items[i] == unitItem)
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>
        /// 종류와 번호가 일치하는 슬롯을 반환합니다.
        /// </summary>
        private UnitSlotItemView FindSlot(UnitSlotType slotType, int slotIndex)
        {
            if (unitView == null)
            {
                return null;
            }

            foreach (UnitSlotItemView slotItem in unitView.SlotItems)
            {
                if (slotItem == null)
                {
                    continue;
                }

                if (slotItem.SlotType == slotType && slotItem.SlotIndex == slotIndex)
                {
                    return slotItem;
                }
            }

            return null;
        }

        /// <summary>
        /// 현재 배치 인원 표시를 갱신합니다.
        /// </summary>
        private void UpdateUnitCount()
        {
            if (unitView == null)
            {
                return;
            }

            unitView.SetUnitCount(battleGroup.Count, battleGroup.MaxCount);

            unitView.SetSupportUnitCount(supportGroup.Count, supportGroup.MaxCount);

            RefreshSynergyPanel();
        }

        /// <summary>
        /// 현재 전투 슬롯에 배치된 유닛 기준으로 시너지 보유 현황을 다시 계산해 표시합니다.
        /// 발동 수가 높은 시너지가 먼저 오도록 정렬합니다.
        /// </summary>
        private void RefreshSynergyPanel()
        {
            if (rosterData == null || unitView == null || unitView.SynergyContentRoot == null || synergyItemTemplate == null)
            {
                return;
            }

            Dictionary<SynergyTrait, int> traitCounts = BuildTraitCounts();

            Transform panelRoot = unitView.SynergyContentRoot;
            for (int i = panelRoot.childCount - 1; i >= 0; i--)
            {
                Transform child = panelRoot.GetChild(i);

                if (child == synergyItemTemplate.transform)
                {
                    continue;
                }

                Destroy(child.gameObject);
            }

            List<SynergyPanelUtility.DisplayItem> displayItems =
                SynergyPanelUtility.BuildDisplayItems(rosterData.SynergyDefinitions, traitCounts);

            foreach (SynergyPanelUtility.DisplayItem displayItem in displayItems)
            {
                SynergyItemView item = Instantiate(synergyItemTemplate, panelRoot);
                item.gameObject.SetActive(true);
                item.SetTitle(displayItem.Definition.Trait.DisplayName);
                item.SetStackText(displayItem.StackText);
                item.SetBackgroundColor(displayItem.IsActive ? synergyActiveColor : synergyInactiveColor);
            }
        }

        /// <summary>
        /// 현재 전투 슬롯에 배치된 유닛들의 트레이트 보유 수를 센다.
        /// (서브 슬롯 유닛은 CombatManager와 마찬가지로 시너지 계산에서 제외한다.)
        /// </summary>
        private Dictionary<SynergyTrait, int> BuildTraitCounts()
        {
            List<int> battleUnitIds = new List<int>();
            foreach (UnitData data in battleGroup.Data)
            {
                if (data != null)
                {
                    battleUnitIds.Add(data.id);
                }
            }

            return SynergyPanelUtility.CountTraits(battleUnitIds, unitTraitsById);
        }

        /// <summary>
        /// 현재 드래그 상태를 초기화합니다.
        /// </summary>
        private void ClearDragState()
        {
            draggingUnitItem = null;
            dragOriginParent = null;
            dragOriginSiblingIndex = -1;
            dragDropHandled = false;
        }

        /// <summary>
        /// 현재 전투 및 서브 유닛 편성을 전투 씬 전달 데이터에 저장합니다.
        /// </summary>
        private void SaveFormation()
        {
            if (formationCombatLink != null)
            {
                formationCombatLink.SaveFormation();
            }
        }

        private static bool IsValidSlot(SlotGroup group, int slotIndex)
        {
            return slotIndex >= 0 && slotIndex < group.Data.Length;
        }

        private static string GroupLabel(SlotGroup group)
        {
            return group.SlotType == UnitSlotType.Battle ? "전투" : "서브";
        }

        /// <summary>
        /// 보유 유닛 아이템에 연결된 유닛 데이터를 반환합니다.
        /// 연결된 데이터가 없다면 null을 반환합니다.
        /// </summary>
        public UnitData GetUnitData(UnitItemView unitItem)
        {
            if (unitItem == null)
            {
                return null;
            }

            if (unitDataByItem.TryGetValue(unitItem, out UnitData unitData))
            {
                return unitData;
            }

            return null;
        }

        /// <summary>
        /// 지정한 전투 슬롯 위치의 유닛 데이터를 반환합니다.
        /// 유닛이 없거나 유효하지 않은 위치라면 null을 반환합니다.
        /// </summary>
        public UnitData GetBattleUnitData(int slotIndex)
        {
            return IsValidSlot(battleGroup, slotIndex) ? battleGroup.Data[slotIndex] : null;
        }

        /// <summary>
        /// 지정한 서브 슬롯 위치의 유닛 데이터를 반환합니다.
        /// 유닛이 없거나 유효하지 않은 위치라면 null을 반환합니다.
        /// </summary>
        public UnitData GetSupportUnitData(int slotIndex)
        {
            return IsValidSlot(supportGroup, slotIndex) ? supportGroup.Data[slotIndex] : null;
        }

        /// <summary>
        /// 전투 슬롯에 배치된 유닛 아이템을 반환합니다.
        /// </summary>
        public UnitItemView GetBattleUnitItem(int slotIndex)
        {
            return IsValidSlot(battleGroup, slotIndex) ? battleGroup.Items[slotIndex] : null;
        }

        /// <summary>
        /// 서브 슬롯에 배치된 유닛 아이템을 반환합니다.
        /// </summary>
        public UnitItemView GetSupportUnitItem(int slotIndex)
        {
            return IsValidSlot(supportGroup, slotIndex) ? supportGroup.Items[slotIndex] : null;
        }

        // [추가됨] 새 유닛을 얻었을 때 편성창(하단 목록)에 아이템을 즉시 1개 추가해주는 함수
        private void AddNewUnitItem(UnitData source)
        {
            if (source == null || unitItemTemplate == null || unitView == null) return;
            // 1. 편성창 전용 독립 데이터로 복사하여 내부 리스트에 추가
            // (필드를 직접 나열하지 않고 PlayerInventoryManager.CloneUnitData를 재사용해
            //  UnitData에 필드가 추가되어도 이 복사가 누락되지 않도록 한다.)
            UnitData newData = PlayerInventoryManager.CloneUnitData(source);
            testUnitDataList.Add(newData);
            // 2. UI 아이템(프리팹) 1개 새로 생성 후 셋팅
            UnitItemView unitItem = Instantiate(unitItemTemplate, unitView.UnitContentRoot);
            unitItem.name = $"Unit_Item_{testUnitDataList.Count:00}";

            unitItem.SetIcon(GetUnitIcon(newData));
            unitItem.SetIconColor(newData.color);
            unitItem.SetSelected(false);
            unitItem.gameObject.SetActive(true);
            // 3. 컨트롤러가 관리하는 딕셔너리와 View에 등록
            unitDataByItem.Add(unitItem, newData);
            unitView.RegisterUnitItem(unitItem);
            // 4. 시너지나 카운트 갱신
            UpdateUnitCount();
        }

        /// <summary>
        /// 유닛별 아이콘을 id로 찾고, 등록되지 않은 유닛은 기존 공용 아이콘으로 폴백합니다.
        /// </summary>
        private Sprite GetUnitIcon(UnitData unitData)
        {
            if (unitData != null)
            {
                for (int index = 0; index < unitIcons.Count; index++)
                {
                    UnitIconEntry entry = unitIcons[index];
                    if (entry.unitId == unitData.id && entry.icon != null)
                    {
                        return entry.icon;
                    }
                }
            }

            return unitIconSprite;
        }
    }
}
