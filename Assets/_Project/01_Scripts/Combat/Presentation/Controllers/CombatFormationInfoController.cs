using System.Collections.Generic;
using OzGameLab01.UI.Battle;
using UnityEngine;

namespace OzGameLab01.Controllers
{
    /// <summary>
    /// 보드 씬에서 전달받은 편성 정보를
    /// 배틀 UI의 3×3 슬롯과 하단 유닛 카드에 표시합니다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CombatFormationInfoController : MonoBehaviour
    {
        private const int BattleSlotCount = 9;
        private const int MaxBattleUnitCount = 4;
        private const int SupportSlotCount = 2;

        [Header("배틀 UI")]
        [SerializeField]
        [Tooltip("배틀 UI 전체 화면을 관리하는 View")]
        private CombatUIView battleUIView;

        private void Start()
        {
            RefreshFormationUI();
        }

        /// <summary>
        /// 전달받은 전투 및 서브 유닛 편성을 배틀 UI에 표시합니다.
        /// </summary>
        public void RefreshFormationUI()
        {
            if (battleUIView == null)
            {
                Debug.LogError("[CombatFormationInfoController] BattleUIView가 연결되지 않았습니다.", this);

                return;
            }

            CombatMainView mainView = battleUIView.MainView;

            if (mainView == null)
            {
                Debug.LogError("[CombatFormationInfoController] BattleMainView가 연결되지 않았습니다.", this);

                return;
            }

            CombatUnitInfoView unitInfoView =
                mainView.UnitInfoView;

            if (unitInfoView == null)
            {
                Debug.LogError("[CombatFormationInfoController] BattleUnitInfoView가 연결되지 않았습니다.", this);

                return;
            }

            unitInfoView.ClearUnitInfoItems();

            battleUIView.Show();
            battleUIView.ShowMainView();
            battleUIView.HideAllOverlayViews();

            CreateSupportCards(unitInfoView);
            CreateBattleCards(unitInfoView);

            Debug.Log("[CombatFormationInfoController] 전투 및 서브 유닛 편성을 표시했습니다.", this);
        }

        /// <summary>
        /// 하단 카드의 앞쪽 두 칸에 서브 유닛을 표시합니다.
        /// </summary>
        private void CreateSupportCards(CombatUnitInfoView unitInfoView)
        {
            IReadOnlyList<UnitFormationCombatLink.TransferredUnit>
                supportUnits = UnitFormationCombatLink.SupportUnitList;

            for (int slotIndex = 0; slotIndex < SupportSlotCount; slotIndex++)
            {
                CombatUnitInfoItemView card = unitInfoView.CreateSupportUnitInfoItem();

                if (card == null)
                {
                    Debug.LogWarning("[CombatFormationInfoController] 서브 유닛 카드를 생성하지 못했습니다.", this);

                    continue;
                }

                UnitFormationCombatLink.TransferredUnit unit = supportUnits[slotIndex];
                SetUnitInfoCard(card, unit);
            }
        }

        /// <summary>
        /// 하단 카드의 뒤쪽 네 칸에 전투 유닛을 표시합니다.
        /// </summary>
        private void CreateBattleCards(CombatUnitInfoView unitInfoView)
        {
            IReadOnlyList<UnitFormationCombatLink.TransferredUnit>
                battleUnits = UnitFormationCombatLink.BattleUnitList;

            int createdBattleCardCount = 0;

            for (int slotIndex = 0; slotIndex < BattleSlotCount && createdBattleCardCount < MaxBattleUnitCount; slotIndex++)
            {
                UnitFormationCombatLink.TransferredUnit unit = battleUnits[slotIndex];
                if (unit == null)
                {
                    continue;
                }

                CombatUnitInfoItemView card = unitInfoView.CreateBattleUnitInfoItem();
                if (card == null)
                {
                    continue;
                }

                SetUnitInfoCard(card, unit);
                createdBattleCardCount++;
            }

            while (createdBattleCardCount < MaxBattleUnitCount)
            {
                CombatUnitInfoItemView emptyCard = unitInfoView.CreateBattleUnitInfoItem();
                if (emptyCard == null)
                {
                    break;
                }

                SetUnitInfoCard(emptyCard, null);
                createdBattleCardCount++;
            }
        }

        /// <summary>
        /// 전투/서브 카드 공용 표시 로직입니다. unit이 null이면 빈 카드로 표시합니다.
        /// </summary>
        private void SetUnitInfoCard(CombatUnitInfoItemView card, UnitFormationCombatLink.TransferredUnit unit)
        {
            card.SetPortrait(unit?.Sprite);

            if (unit != null && card.PortraitImage != null)
            {
                card.PortraitImage.color = unit.Color;
            }

            card.SetUnitName(unit != null ? unit.Data.name : string.Empty);
            card.SetSkillVisible(false);
            card.SetGraveVisible(false);
            card.Show();
        }

    }
}
