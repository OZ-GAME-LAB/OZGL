using System.Collections.Generic;
using OzGameLab01.Data;
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
                UnitFormationCombatLink.TransferredUnit unit = supportUnits[slotIndex];
                if (unit?.Data == null)
                {
                    continue;
                }

                CombatUnitInfoItemView card = unitInfoView.CreateSupportUnitInfoItem();

                if (card == null)
                {
                    Debug.LogWarning("[CombatFormationInfoController] 서브 유닛 카드를 생성하지 못했습니다.", this);

                    continue;
                }

                SetUnitInfoCard(card, unit, unit.Data.passiveSkillId);
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
                if (unit?.Data == null)
                {
                    continue;
                }

                CombatUnitInfoItemView card = unitInfoView.CreateBattleUnitInfoItem();
                if (card == null)
                {
                    continue;
                }

                SetUnitInfoCard(card, unit, unit.Data.activeSkillId);
                createdBattleCardCount++;
            }
        }

        /// <summary>
        /// 전투/서포트 카드에 배치 유닛 정보와 역할에 맞는 스킬 아이콘을 표시합니다.
        /// </summary>
        private void SetUnitInfoCard(
            CombatUnitInfoItemView card,
            UnitFormationCombatLink.TransferredUnit unit,
            int displaySkillId)
        {
            card.SetPortrait(unit.Sprite);

            if (card.PortraitImage != null)
            {
                card.PortraitImage.color = unit.Color;
            }

            card.SetUnitName(unit.Data.name);

            SkillData displaySkill = displaySkillId > 0
                ? RuntimeContent.Catalog.GetSkill(displaySkillId)
                : null;
            _ = card.SetSkillIconAsync(displaySkill?.iconAddress);

            card.SetGraveVisible(false);
            card.Show();
        }

    }
}
