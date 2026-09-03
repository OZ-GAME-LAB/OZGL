using System;
using System.Collections.Generic;
using OzGameLab01.Combat;
using OzGameLab01.Managers;
using OzGameLab01.UI;
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

        private const int BattleSlotCount = 9;

        private const int SupportSlotCount = 2;

        private static readonly TransferredUnit[] battleUnits = new TransferredUnit[BattleSlotCount];

        private static readonly TransferredUnit[] supportUnits = new TransferredUnit[SupportSlotCount];

        [Header("유닛 배치")]
        [SerializeField]
        [Tooltip("유닛 배치 정보를 관리하는 컨트롤러")]
        private UnitFormationController formationController;

        public static IReadOnlyList<TransferredUnit> BattleUnits => battleUnits;

        public static IReadOnlyList<TransferredUnit> SupportUnits => supportUnits;

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

            SceneTransitioner.AllyFormationSlots = CreateCombatFormation();

            Debug.Log("[UnitFormationCombatLink] 최신 유닛 편성을 저장했습니다.", this);
        }

        private void SaveTransferredUnits()
        {
            ClearTransferredUnits();

            for (int slotIndex = 0; slotIndex < BattleSlotCount; slotIndex++)
            {
                UnitData unitData = formationController.GetBattleUnitData(slotIndex);

                UnitItemView unitItem = formationController.GetBattleUnitItem(slotIndex);

                battleUnits[slotIndex] = CreateTransferredUnit(unitData, unitItem);
            }

            for (int slotIndex = 0; slotIndex < SupportSlotCount; slotIndex++)
            {
                UnitData unitData = formationController.GetSupportUnitData(slotIndex);

                UnitItemView unitItem = formationController.GetSupportUnitItem(slotIndex);

                supportUnits[slotIndex] = CreateTransferredUnit(unitData, unitItem);
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

        private Unit[] CreateCombatFormation()
        {
            Unit[] combatFormation = new Unit[BattleSlotCount];

            for (int slotIndex = 0; slotIndex < BattleSlotCount; slotIndex++)
            {
                TransferredUnit transferredUnit = battleUnits[slotIndex];

                if (transferredUnit == null || transferredUnit.Data == null)
                {
                    continue;
                }

                Unit unitPrefab = FindUnitPrefab(transferredUnit.Data.id);

                if (unitPrefab == null)
                {
                    Debug.LogWarning(
                        $"[UnitFormationCombatLink] ID {transferredUnit.Data.id}에 " +
                        $"연결된 전투 프리팹이 없습니다. Slot: {slotIndex}", this);

                    continue;
                }

                combatFormation[slotIndex] = unitPrefab;
            }

            return combatFormation;
        }

        private Unit FindUnitPrefab(int unitId)
        {
            UnitRosterData rosterData = formationController != null ? formationController.RosterData : null;

            if (rosterData == null)
            {
                return null;
            }

            foreach (UnitRosterData.UnitPrefabEntry entry in rosterData.UnitPrefabs)
            {
                if (entry.id == unitId && entry.prefab != null)
                {
                    return entry.prefab.GetComponent<Unit>();
                }
            }

            return null;
        }

        private static void ClearTransferredUnits()
        {
            Array.Clear(battleUnits, 0, battleUnits.Length);

            Array.Clear(supportUnits, 0, supportUnits.Length);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlayStart()
        {
            ClearTransferredUnits();
            SceneTransitioner.AllyFormationSlots = null;
        }
    }
}
