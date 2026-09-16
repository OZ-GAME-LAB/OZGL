using System;
using System.Collections.Generic;
using OzGameLab01.Managers;
using OzGameLab01.UI;
using OzGameLab01.Data;
using OzGameLab01.Save;
using UnityEngine;

namespace OzGameLab01.Controllers
{
    /// <summary>
    /// 유닛 배치 결과와 화면 정보를 전투 씬에 전달합니다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class UnitFormationCombatLink : MonoBehaviour
    {
        public sealed class TransferredUnit
        {
            public UnitData Data { get; }
            public Sprite Sprite { get; }
            public Color Color { get; }

            public TransferredUnit(
                UnitData data,
                Sprite sprite,
                Color color)
            {
                Data = data;
                Sprite = sprite;
                Color = color;
            }
        }

        private const int BATTLE_SLOT_COUNT = 9;

        private const int SUPPORT_SLOT_COUNT = 2;

        private static readonly TransferredUnit[] BattleUnits = new TransferredUnit[BATTLE_SLOT_COUNT];

        private static readonly TransferredUnit[] SupportUnits = new TransferredUnit[SUPPORT_SLOT_COUNT];
        private static readonly int[] SavedBattleUnitIds = new int[BATTLE_SLOT_COUNT];
        private static readonly int[] SavedSupportUnitIds = new int[SUPPORT_SLOT_COUNT];
        private static bool HasSavedFormation;

        [Header("유닛 배치")]
        [SerializeField]
        [Tooltip("유닛 배치 정보를 관리하는 컨트롤러")]
        private UnitFormationController formationController;

        public static IReadOnlyList<TransferredUnit> BattleUnitList => BattleUnits;
        public static IReadOnlyList<TransferredUnit> SupportUnitList => SupportUnits;
        public static IReadOnlyList<int> SavedBattleUnitIdList => SavedBattleUnitIds;
        public static IReadOnlyList<int> SavedSupportUnitIdList => SavedSupportUnitIds;
        public static bool Has_Saved_Formation => HasSavedFormation;

        /// <summary>
        /// 저장된 전투 슬롯에 유닛이 한 명 이상 있는지 반환합니다.
        /// </summary>
        public static bool HasSavedBattleUnit
        {
            get
            {
                if (!HasSavedFormation)
                {
                    return false;
                }

                for (int slotIndex = 0; slotIndex < SavedBattleUnitIds.Length; slotIndex++)
                {
                    if (SavedBattleUnitIds[slotIndex] >= 0)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        private void Awake()
        {
            if (formationController == null)
            {
                formationController = GetComponent<UnitFormationController>();
            }
        }

        /// <summary>
        /// 현재 전투 및 서브 편성을 전투 씬 전달 데이터에 저장합니다.
        /// </summary>
        public void SaveFormation()
        {
            if (formationController == null)
            {
                Debug.LogError("[UnitFormationCombatLink] UnitFormationController가 연결되지 않았습니다.", this);

                return;
            }

            SaveTransferredUnits();

            SceneTransitioner.AllyFormationData = CreateCombatFormation();

            SaveFormationIds();

            Debug.Log("[UnitFormationCombatLink] 최신 유닛 편성을 저장했습니다.", this);
        }

        private void SaveTransferredUnits()
        {
            ClearTransferredUnits();

            for (int slotIndex = 0; slotIndex < BATTLE_SLOT_COUNT; slotIndex++)
            {
                UnitData unitData = formationController.GetBattleUnitData(slotIndex);

                UnitItemView unitItem = formationController.GetBattleUnitItem(slotIndex);

                BattleUnits[slotIndex] = CreateTransferredUnit(unitData, unitItem);
            }

            for (int slotIndex = 0; slotIndex < SUPPORT_SLOT_COUNT; slotIndex++)
            {
                UnitData unitData = formationController.GetSupportUnitData(slotIndex);

                UnitItemView unitItem = formationController.GetSupportUnitItem(slotIndex);

                SupportUnits[slotIndex] = CreateTransferredUnit(unitData, unitItem);
            }
        }

        private TransferredUnit CreateTransferredUnit(UnitData unitData, UnitItemView unitItem)
        {
            if (unitData == null)
            {
                return null;
            }

            Sprite sprite = null;
            Color color = Color.white;

            if (unitItem != null && unitItem.UnitIcon != null)
            {
                sprite = unitItem.UnitIcon.sprite;
                color = unitItem.UnitIcon.color;
            }

            return new TransferredUnit(
                unitData,
                sprite,
                color);
        }

        /// <summary>
        /// 전투 슬롯에 배치된 유닛의 UnitData를 그대로 전달합니다.
        /// 아군은 공용 프리팹 하나를 이 데이터로 Configure()하여 스폰하므로,
        /// id별 프리팹을 따로 찾을 필요가 없습니다.
        /// </summary>
        private UnitData[] CreateCombatFormation()
        {
            UnitData[] combatFormation = new UnitData[BATTLE_SLOT_COUNT];

            for (int slotIndex = 0; slotIndex < BATTLE_SLOT_COUNT; slotIndex++)
            {
                TransferredUnit transferredUnit = BattleUnits[slotIndex];

                combatFormation[slotIndex] = transferredUnit?.Data;
            }

            return combatFormation;
        }

        /// <summary>
        /// 각 슬롯의 유닛 ID를 복귀용 스냅샷으로 저장합니다. 빈 슬롯은 -1입니다.
        /// </summary>
        private void SaveFormationIds()
        {
            for (int slotIndex = 0; slotIndex < BATTLE_SLOT_COUNT; slotIndex++)
            {
                SavedBattleUnitIds[slotIndex] = BattleUnits[slotIndex]?.Data?.id ?? -1;
            }

            for (int slotIndex = 0; slotIndex < SUPPORT_SLOT_COUNT; slotIndex++)
            {
                SavedSupportUnitIds[slotIndex] = SupportUnits[slotIndex]?.Data?.id ?? -1;
            }

            HasSavedFormation = true;
        }

        /// <summary>
        /// 현재 편성 ID를 런 저장 데이터에 기록합니다.
        /// </summary>
        /// <param name="saveData"></param>
        public static void WriteFormationToSaveData(BoardRunSaveData saveData)
        {
            if (saveData == null)
            {
                return;
            }

            saveData.battleFormationUnitIds.Clear();
            saveData.supportFormationUnitIds.Clear();

            for (int slotIndex = 0; slotIndex < SavedBattleUnitIds.Length; slotIndex++)
            {
                saveData.battleFormationUnitIds.Add(SavedBattleUnitIds[slotIndex]);
            }

            for (int slotIndex = 0; slotIndex < SavedSupportUnitIds.Length; slotIndex++)
            {
                saveData.supportFormationUnitIds.Add(SavedSupportUnitIds[slotIndex]);
            }
        }

        /// <summary>
        /// Continue 저장 데이터의 편성 ID를 메인보드 복원용 정적 상태에 적용합니다.
        /// </summary>
        public static void RestoreFormationFromSaveData(BoardRunSaveData saveData)
        {
            ClearSavedFormation();

            if (saveData == null)
            {
                return;
            }

            for (int slotIndex = 0; slotIndex < SavedBattleUnitIds.Length; slotIndex++)
            {
                SavedBattleUnitIds[slotIndex] = GetSavedUnitId(
                    saveData.battleFormationUnitIds,
                    slotIndex);
            }

            for (int slotIndex = 0; slotIndex < SavedSupportUnitIds.Length; slotIndex++)
            {
                SavedSupportUnitIds[slotIndex] = GetSavedUnitId(
                    saveData.supportFormationUnitIds,
                    slotIndex);
            }

            HasSavedFormation = true;
        }

        /// <summary>
        /// New Game에서 이전 런의 전투 및 서브 편성을 제거합니다.
        /// </summary>
        public static void ClearSavedFormation()
        {
            ClearTransferredUnits();
            SceneTransitioner.AllyFormationData = null;

            for (int slotIndex = 0; slotIndex < SavedBattleUnitIds.Length; slotIndex++)
            {
                SavedBattleUnitIds[slotIndex] = -1;
            }

            for (int slotIndex = 0; slotIndex < SavedSupportUnitIds.Length; slotIndex++)
            {
                SavedSupportUnitIds[slotIndex] = -1;
            }

            HasSavedFormation = false;
        }

        /// <summary>
        ///  저장 목록에 슬롯 값이 없으면 빈 슬롯 ID를 반환합니다.
        /// </summary>
        private static int GetSavedUnitId(IReadOnlyList<int> unitIds, int slotIndex)
        {
            return unitIds != null && slotIndex >= 0 && slotIndex < unitIds.Count
                ? unitIds[slotIndex]
                : -1;
        }

        private static void ClearTransferredUnits()
        {
            Array.Clear(BattleUnits, 0, BattleUnits.Length);

            Array.Clear(SupportUnits, 0, SupportUnits.Length);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlayStart()
        {
            ClearTransferredUnits();
            SceneTransitioner.AllyFormationData = null;
            // 새 플레이 세션에는 이전 실행의 편성 ID를 남기지 않습니다.
            for (int index = 0; index < SavedBattleUnitIds.Length; index++)
            {
                SavedBattleUnitIds[index] = -1;
            }

            for (int index = 0; index < SavedSupportUnitIds.Length; index++)
            {
                SavedSupportUnitIds[index] = -1;
            }

            HasSavedFormation = false;
        }
    }
}
