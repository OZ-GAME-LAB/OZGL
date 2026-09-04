using System.Collections.Generic;
using OzGameLab01.Combat;
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

        private readonly List<UnitData> testUnitDataList = new List<UnitData>();

        private readonly Dictionary<UnitItemView, UnitData> unitDataByItem = new Dictionary<UnitItemView, UnitData>();

        private readonly UnitData[] battleUnitData = new UnitData[BattleSlotCount];

        private readonly UnitItemView[] battleUnitItems = new UnitItemView[BattleSlotCount];

        private readonly UnitData[] supportUnitData = new UnitData[SupportSlotCount];

        private readonly UnitItemView[] supportUnitItems = new UnitItemView[SupportSlotCount];

        private int battleUnitCount;
        private int supportUnitCount;

        private UnitItemView draggingUnitItem;
        private Transform dragOriginParent;
        private int dragOriginSiblingIndex = -1;
        private bool dragDropHandled;

        private UnitFormationCombatLink formationCombatLink;

        /// <summary>
        /// 현재 배치 화면에 연결된 테스트 유닛 데이터를 반환합니다.
        /// </summary>
        public IReadOnlyList<UnitData> TestUnitDataList => testUnitDataList;

        /// <summary>
        /// 3×3 전투 슬롯에 배치된 유닛 데이터를 반환합니다.
        /// 인덱스는 전투 슬롯의 0~8 위치와 일치합니다.
        /// </summary>
        public IReadOnlyList<UnitData> BattleUnitData => battleUnitData;

        /// <summary>
        /// 서브 슬롯에 배치된 유닛 데이터를 반환합니다.
        /// 인덱스는 서브 슬롯의 0~1 위치와 일치합니다.
        /// </summary>
        public IReadOnlyList<UnitData> SupportUnitData => supportUnitData;

        /// <summary>
        /// 현재 전투 슬롯에 배치된 유닛 수를 반환합니다.
        /// </summary>
        public int BattleUnitCount => battleUnitCount;

        /// <summary>
        /// 현재 서브 슬롯에 배치된 유닛 수를 반환합니다.
        /// </summary>
        public int SupportUnitCount => supportUnitCount;

        /// <summary>
        /// 전투 유닛이 한 명 이상 배치되었는지 반환합니다.
        /// </summary>
        public bool CanStartBattle => battleUnitCount >= 1;

        /// <summary>
        /// 보유 유닛 목록의 원본 데이터입니다. UnitFormationCombatLink가 프리팹 조회에 공유합니다.
        /// </summary>
        public UnitRosterData RosterData => rosterData;

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
            CreateUnitItems();
            UpdateUnitCount();
        }

        private void OnDisable()
        {
            UnsubscribeViewEvents();
            ClearDragState();
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
        /// 표시용 이름은 프리팹 이름을 사용합니다(실제 캐릭터 이름 데이터가 아직 없어 그레이박스로 대체).
        /// </summary>
        private void LoadRosterUnitData()
        {
            testUnitDataList.Clear();

            if (rosterData == null)
            {
                //Debug.LogError("[UnitFormationController] UnitRosterData가 연결되지 않았습니다.", this);
                return;
            }

            foreach (UnitRosterData.UnitPrefabEntry entry in rosterData.UnitPrefabs)
            {
                if (entry.prefab == null)
                {
                    continue;
                }

                testUnitDataList.Add(new UnitData
                {
                    id = entry.id,
                    name = entry.prefab.name
                });
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

                SpriteRenderer unitSpriteRenderer = FindUnitSpriteRenderer(testUnitDataList[i].id);
                unitItem.SetIcon(unitSpriteRenderer != null ? unitSpriteRenderer.sprite : null);
                unitItem.SetIconColor(unitSpriteRenderer != null ? unitSpriteRenderer.color : Color.white);
                unitItem.SetSelected(false);
                unitItem.gameObject.SetActive(true);

                unitDataByItem.Add(unitItem, testUnitDataList[i]);

                unitView.RegisterUnitItem(unitItem);
            }
        }

        /// <summary>
        /// 유닛 id에 대응하는 전투 프리팹의 SpriteRenderer를 반환합니다.
        /// 아이콘의 스프라이트와 트레이트 색상(빨강/파랑/노랑 틴트)을 함께 가져오는 데 씁니다.
        /// </summary>
        private SpriteRenderer FindUnitSpriteRenderer(int unitId)
        {
            if (rosterData == null)
            {
                return null;
            }

            foreach (UnitRosterData.UnitPrefabEntry entry in rosterData.UnitPrefabs)
            {
                if (entry.id != unitId || entry.prefab == null)
                {
                    continue;
                }

                return entry.prefab.GetComponentInChildren<SpriteRenderer>();
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

            int battleSlotIndex = FindBattleSlotIndex(unitItem);

            if (battleSlotIndex >= 0)
            {
                if (RemoveBattleUnit(battleSlotIndex))
                {
                    SaveFormation();
                }

                return;
            }

            int supportSlotIndex = FindSupportSlotIndex(unitItem);

            if (supportSlotIndex >= 0)
            {
                if (RemoveSupportUnit(supportSlotIndex))
                {
                    SaveFormation();
                }

                return;
            }

            if (PlaceUnitInFirstEmptyBattleSlot(unitItem))
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
                dragDropHandled = TryDropOnBattleSlot(draggingUnitItem, targetSlot);
            }
            else if (targetSlot.IsSupportSlot)
            {
                dragDropHandled = TryDropOnSupportSlot(draggingUnitItem, targetSlot);
            }

            if (dragDropHandled)
            {
                SaveFormation();
            }
        }

        /// <summary>
        /// 드래그한 유닛을 지정한 전투 슬롯에 배치합니다.
        /// </summary>
        private bool TryDropOnBattleSlot(UnitItemView unitItem, UnitSlotItemView targetSlot)
        {
            int targetIndex = targetSlot.SlotIndex;

            if (!IsValidBattleSlot(targetIndex))
            {
                return false;
            }

            if (FindSupportSlotIndex(unitItem) >= 0)
            {
                return false;
            }

            if (!unitDataByItem.TryGetValue(unitItem, out UnitData draggedUnitData))
            {
                return false;
            }

            int sourceIndex = FindBattleSlotIndex(unitItem);

            UnitItemView targetUnitItem = battleUnitItems[targetIndex];

            UnitData targetUnitData = battleUnitData[targetIndex];

            if (sourceIndex == targetIndex)
            {
                MoveUnitItemToSlot(unitItem, targetSlot);

                return true;
            }

            if (sourceIndex < 0 && targetUnitItem == null)
            {
                return PlaceUnitInEmptyBattleSlot(unitItem, draggedUnitData, targetSlot);
            }

            if (sourceIndex >= 0 && targetUnitItem == null)
            {
                return MoveBattleUnitToEmptySlot(
                    unitItem, draggedUnitData, sourceIndex, targetSlot);
            }

            if (sourceIndex < 0 &&
                targetUnitItem != null)
            {
                return ReplaceBattleUnit(unitItem, draggedUnitData, targetUnitItem, targetIndex, targetSlot);
            }

            if (sourceIndex >= 0 && targetUnitItem != null)
            {
                return SwapBattleUnits(unitItem, draggedUnitData, targetUnitItem, targetUnitData, sourceIndex, targetSlot);
            }

            return false;
        }

        /// <summary>
        /// 보유 유닛을 비어 있는 전투 슬롯에 배치합니다.
        /// </summary>
        private bool PlaceUnitInEmptyBattleSlot(UnitItemView unitItem, UnitData unitData, UnitSlotItemView targetSlot)
        {
            if (battleUnitCount >= MaxBattleUnitCount)
            {
                Debug.LogWarning("[UnitFormationController] 전투 유닛은 최대 4명까지 배치할 수 있습니다.", this);

                return false;
            }

            int targetIndex = targetSlot.SlotIndex;

            battleUnitItems[targetIndex] = unitItem;
            battleUnitData[targetIndex] = unitData;
            battleUnitCount++;

            targetSlot.SetOccupied(true);
            MoveUnitItemToSlot(unitItem, targetSlot);
            UpdateUnitCount();

            return true;
        }

        /// <summary>
        /// 전투 유닛을 비어 있는 다른 전투 슬롯으로 이동합니다.
        /// </summary>
        private bool MoveBattleUnitToEmptySlot(UnitItemView unitItem, UnitData unitData,
            int sourceIndex, UnitSlotItemView targetSlot)
        {
            UnitSlotItemView sourceSlot = FindSlot(UnitSlotType.Battle, sourceIndex);

            int targetIndex = targetSlot.SlotIndex;

            battleUnitItems[sourceIndex] = null;
            battleUnitData[sourceIndex] = null;

            battleUnitItems[targetIndex] = unitItem;
            battleUnitData[targetIndex] = unitData;

            if (sourceSlot != null)
            {
                sourceSlot.SetOccupied(false);
            }

            targetSlot.SetOccupied(true);
            MoveUnitItemToSlot(unitItem, targetSlot);

            return true;
        }

        /// <summary>
        /// 전투 슬롯의 기존 유닛을 보유 목록 유닛으로 교체합니다.
        /// </summary>
        private bool ReplaceBattleUnit(
            UnitItemView unitItem,
            UnitData unitData,
            UnitItemView targetUnitItem,
            int targetIndex,
            UnitSlotItemView targetSlot)
        {
            MoveUnitItemToList(targetUnitItem);

            battleUnitItems[targetIndex] = unitItem;
            battleUnitData[targetIndex] = unitData;

            targetSlot.SetOccupied(true);
            MoveUnitItemToSlot(unitItem, targetSlot);
            UpdateUnitCount();

            return true;
        }

        /// <summary>
        /// 두 전투 슬롯에 배치된 유닛의 위치를 교체합니다.
        /// </summary>
        private bool SwapBattleUnits(
            UnitItemView unitItem,
            UnitData unitData,
            UnitItemView targetUnitItem,
            UnitData targetUnitData,
            int sourceIndex,
            UnitSlotItemView targetSlot)
        {
            UnitSlotItemView sourceSlot = FindSlot(UnitSlotType.Battle, sourceIndex);

            if (sourceSlot == null)
            {
                return false;
            }

            int targetIndex = targetSlot.SlotIndex;

            battleUnitItems[sourceIndex] = targetUnitItem;

            battleUnitData[sourceIndex] = targetUnitData;

            battleUnitItems[targetIndex] = unitItem;

            battleUnitData[targetIndex] = unitData;

            MoveUnitItemToSlot(targetUnitItem, sourceSlot);

            MoveUnitItemToSlot(unitItem, targetSlot);

            sourceSlot.SetOccupied(true);
            targetSlot.SetOccupied(true);

            return true;
        }

        /// <summary>
        /// 드래그한 유닛을 지정한 서브 슬롯에 배치합니다.
        /// </summary>
        private bool TryDropOnSupportSlot(UnitItemView unitItem, UnitSlotItemView targetSlot)
        {
            int targetIndex = targetSlot.SlotIndex;

            if (!IsValidSupportSlot(targetIndex))
            {
                return false;
            }

            if (FindBattleSlotIndex(unitItem) >= 0)
            {
                return false;
            }

            if (!unitDataByItem.TryGetValue(unitItem, out UnitData draggedUnitData))
            {
                return false;
            }

            int sourceIndex = FindSupportSlotIndex(unitItem);

            UnitItemView targetUnitItem = supportUnitItems[targetIndex];

            UnitData targetUnitData = supportUnitData[targetIndex];

            if (sourceIndex == targetIndex)
            {
                MoveUnitItemToSlot(unitItem, targetSlot);

                return true;
            }

            if (sourceIndex < 0 && targetUnitItem == null)
            {
                return PlaceUnitInEmptySupportSlot(unitItem, draggedUnitData, targetSlot);
            }

            if (sourceIndex >= 0 && targetUnitItem == null)
            {
                return MoveSupportUnitToEmptySlot(
                    unitItem,
                    draggedUnitData,
                    sourceIndex,
                    targetSlot);
            }

            if (sourceIndex < 0 && targetUnitItem != null)
            {
                return ReplaceSupportUnit(
                    unitItem,
                    draggedUnitData,
                    targetUnitItem,
                    targetIndex,
                    targetSlot);
            }

            if (sourceIndex >= 0 && targetUnitItem != null)
            {
                return SwapSupportUnits(
                    unitItem,
                    draggedUnitData,
                    targetUnitItem,
                    targetUnitData,
                    sourceIndex,
                    targetSlot);
            }

            return false;
        }

        /// <summary>
        /// 보유 유닛을 비어 있는 서브 슬롯에 배치합니다.
        /// </summary>
        private bool PlaceUnitInEmptySupportSlot(UnitItemView unitItem, UnitData unitData, UnitSlotItemView targetSlot)
        {
            if (supportUnitCount >= SupportSlotCount)
            {
                Debug.LogWarning("[UnitFormationController] 서브 유닛은 최대 2명까지 배치할 수 있습니다.", this);

                return false;
            }

            int targetIndex = targetSlot.SlotIndex;

            supportUnitItems[targetIndex] = unitItem;
            supportUnitData[targetIndex] = unitData;
            supportUnitCount++;

            targetSlot.SetOccupied(true);
            MoveUnitItemToSlot(unitItem, targetSlot);
            UpdateUnitCount();

            return true;
        }

        /// <summary>
        /// 서브 유닛을 비어 있는 다른 서브 슬롯으로 이동합니다.
        /// </summary>
        private bool MoveSupportUnitToEmptySlot(
            UnitItemView unitItem,
            UnitData unitData,
            int sourceIndex,
            UnitSlotItemView targetSlot)
        {
            UnitSlotItemView sourceSlot = FindSlot(UnitSlotType.Support, sourceIndex);

            int targetIndex = targetSlot.SlotIndex;

            supportUnitItems[sourceIndex] = null;
            supportUnitData[sourceIndex] = null;

            supportUnitItems[targetIndex] = unitItem;
            supportUnitData[targetIndex] = unitData;

            if (sourceSlot != null)
            {
                sourceSlot.SetOccupied(false);
            }

            targetSlot.SetOccupied(true);
            MoveUnitItemToSlot(unitItem, targetSlot);

            return true;
        }

        /// <summary>
        /// 서브 슬롯의 기존 유닛을 보유 목록 유닛으로 교체합니다.
        /// </summary>
        private bool ReplaceSupportUnit(
            UnitItemView unitItem,
            UnitData unitData,
            UnitItemView targetUnitItem,
            int targetIndex,
            UnitSlotItemView targetSlot)
        {
            MoveUnitItemToList(targetUnitItem);

            supportUnitItems[targetIndex] = unitItem;
            supportUnitData[targetIndex] = unitData;

            targetSlot.SetOccupied(true);
            MoveUnitItemToSlot(unitItem, targetSlot);
            UpdateUnitCount();

            return true;
        }

        /// <summary>
        /// 두 서브 슬롯에 배치된 유닛의 위치를 교체합니다.
        /// </summary>
        private bool SwapSupportUnits(
            UnitItemView unitItem,
            UnitData unitData,
            UnitItemView targetUnitItem,
            UnitData targetUnitData,
            int sourceIndex,
            UnitSlotItemView targetSlot)
        {
            UnitSlotItemView sourceSlot = FindSlot(UnitSlotType.Support, sourceIndex);

            if (sourceSlot == null)
            {
                return false;
            }

            int targetIndex = targetSlot.SlotIndex;

            supportUnitItems[sourceIndex] = targetUnitItem;

            supportUnitData[sourceIndex] = targetUnitData;

            supportUnitItems[targetIndex] = unitItem;

            supportUnitData[targetIndex] = unitData;

            MoveUnitItemToSlot(targetUnitItem, sourceSlot);

            MoveUnitItemToSlot(unitItem, targetSlot);

            sourceSlot.SetOccupied(true);
            targetSlot.SetOccupied(true);

            return true;
        }

        /// <summary>
        /// 비어 있는 전투 슬롯을 앞에서부터 찾아 유닛을 배치합니다.
        /// </summary>
        private bool PlaceUnitInFirstEmptyBattleSlot(UnitItemView unitItem)
        {
            if (unitItem == null)
            {
                return false;
            }

            if (battleUnitCount >= MaxBattleUnitCount)
            {
                Debug.LogWarning("[UnitFormationController] 전투 유닛은 최대 4명까지 배치할 수 있습니다.", this);

                return false;
            }

            if (!unitDataByItem.TryGetValue(unitItem, out UnitData unitData))
            {
                return false;
            }

            for (int slotIndex = 0; slotIndex < BattleSlotCount; slotIndex++)
            {
                if (battleUnitData[slotIndex] != null)
                {
                    continue;
                }

                UnitSlotItemView slotItem = FindSlot(UnitSlotType.Battle, slotIndex);

                if (slotItem == null)
                {
                    Debug.LogWarning($"[UnitFormationController] 전투 슬롯 {slotIndex}번을 찾을 수 없습니다.", this);

                    return false;
                }

                return PlaceUnitInEmptyBattleSlot(unitItem, unitData, slotItem);
            }

            return false;
        }

        /// <summary>
        /// 지정한 전투 슬롯의 유닛을 보유 목록으로 되돌립니다.
        /// </summary>
        private bool RemoveBattleUnit(int slotIndex)
        {
            if (!IsValidBattleSlot(slotIndex))
            {
                return false;
            }

            UnitItemView unitItem = battleUnitItems[slotIndex];

            if (unitItem == null)
            {
                return false;
            }

            UnitSlotItemView slotItem = FindSlot(UnitSlotType.Battle, slotIndex);

            battleUnitItems[slotIndex] = null;
            battleUnitData[slotIndex] = null;
            battleUnitCount--;

            MoveUnitItemToList(unitItem);

            if (slotItem != null)
            {
                slotItem.SetOccupied(false);
            }

            UpdateUnitCount();
            return true;
        }

        /// <summary>
        /// 지정한 서브 슬롯의 유닛을 보유 목록으로 되돌립니다.
        /// </summary>
        private bool RemoveSupportUnit(int slotIndex)
        {
            if (!IsValidSupportSlot(slotIndex))
            {
                return false;
            }

            UnitItemView unitItem = supportUnitItems[slotIndex];

            if (unitItem == null)
            {
                return false;
            }

            UnitSlotItemView slotItem = FindSlot(UnitSlotType.Support, slotIndex);

            supportUnitItems[slotIndex] = null;
            supportUnitData[slotIndex] = null;
            supportUnitCount--;

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

            RectTransform unitRect = unitItem.RectTransform;

            RectTransform slotRect = slotItem.RectTransform;

            if (unitRect == null || slotRect == null)
            {
                return;
            }

            unitRect.SetParent(slotRect, false);
            unitRect.anchorMin = new Vector2(0.5f, 0.5f);
            unitRect.anchorMax = new Vector2(0.5f, 0.5f);
            unitRect.pivot = new Vector2(0.5f, 0.5f);
            unitRect.anchoredPosition = Vector2.zero;
            unitRect.localScale = Vector3.one;
            unitRect.sizeDelta = slotRect.rect.size;
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

            RectTransform unitRect = unitItem.RectTransform;

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
        /// 유닛 아이템이 배치된 전투 슬롯 번호를 반환합니다.
        /// </summary>
        private int FindBattleSlotIndex(UnitItemView unitItem)
        {
            for (int i = 0; i < battleUnitItems.Length; i++)
            {
                if (battleUnitItems[i] == unitItem)
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>
        /// 유닛 아이템이 배치된 서브 슬롯 번호를 반환합니다.
        /// </summary>
        private int FindSupportSlotIndex(UnitItemView unitItem)
        {
            for (int i = 0; i < supportUnitItems.Length; i++)
            {
                if (supportUnitItems[i] == unitItem)
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

            unitView.SetUnitCount(battleUnitCount, MaxBattleUnitCount);

            unitView.SetSupportUnitCount(supportUnitCount, SupportSlotCount);
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

        private bool IsValidBattleSlot(int slotIndex)
        {
            return slotIndex >= 0 && slotIndex < BattleSlotCount;
        }

        private bool IsValidSupportSlot(int slotIndex)
        {
            return slotIndex >= 0 && slotIndex < SupportSlotCount;
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
            if (!IsValidBattleSlot(slotIndex))
            {
                return null;
            }

            return battleUnitData[slotIndex];
        }

        /// <summary>
        /// 지정한 서브 슬롯 위치의 유닛 데이터를 반환합니다.
        /// 유닛이 없거나 유효하지 않은 위치라면 null을 반환합니다.
        /// </summary>
        public UnitData GetSupportUnitData(int slotIndex)
        {
            if (!IsValidSupportSlot(slotIndex))
            {
                return null;
            }

            return supportUnitData[slotIndex];
        }

        /// <summary>
        /// 전투 슬롯에 배치된 유닛 아이템을 반환합니다.
        /// </summary>
        public UnitItemView GetBattleUnitItem(int slotIndex)
        {
            if (!IsValidBattleSlot(slotIndex))
            {
                return null;
            }

            return battleUnitItems[slotIndex];
        }

        /// <summary>
        /// 서브 슬롯에 배치된 유닛 아이템을 반환합니다.
        /// </summary>
        public UnitItemView GetSupportUnitItem(int slotIndex)
        {
            if (!IsValidSupportSlot(slotIndex))
            {
                return null;
            }

            return supportUnitItems[slotIndex];
        }
    }


}
