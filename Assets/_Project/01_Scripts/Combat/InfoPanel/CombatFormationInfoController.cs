using System.Collections.Generic;
using OzGameLab01.UI.Battle;
using UnityEngine;
using UnityEngine.UI;

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

        private readonly List<GameObject> createdSlotIcons = new List<GameObject>();

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

            ClearSlotIcons();
            unitInfoView.ClearUnitInfoItems();

            battleUIView.Show();
            battleUIView.ShowMainView();
            battleUIView.HideAllOverlayViews();

            CreateBattleSlotIcons(mainView);
            CreateSupportCards(unitInfoView);
            CreateBattleCards(unitInfoView);

            Debug.Log("[CombatFormationInfoController] 전투 및 서브 유닛 편성을 표시했습니다.", this);
        }

        /// <summary>
        /// 전투 편성 인덱스 0~8과 동일한 회색 슬롯에
        /// 유닛 아이콘을 생성합니다.
        /// </summary>
        private void CreateBattleSlotIcons(CombatMainView mainView)
        {
            IReadOnlyList<PlayerSlotItemView> slotViews = mainView.PlayerSlotViews;

            IReadOnlyList<UnitFormationCombatLink.TransferredUnit>
                transferredUnits = UnitFormationCombatLink.BattleUnits;

            int slotCount = Mathf.Min(BattleSlotCount, slotViews.Count);

            for (int slotIndex = 0; slotIndex < slotCount; slotIndex++)
            {
                UnitFormationCombatLink.TransferredUnit unit = transferredUnits[slotIndex];

                PlayerSlotItemView slotView = slotViews[slotIndex];

                if (unit == null || slotView == null || slotView.UnitAnchor == null)
                {
                    continue;
                }

                // CreateSlotIcon(slotView.UnitAnchor, unit, slotIndex);
                // [수정] : CombatManager가 실제 Unit과 연결된 이미지를 이미 만들었다면 중복 아이콘을 생성하지 않습니다.
                if (slotView.UnitAnchor.Find($"BattleCombatUnit_{slotIndex:00}") == null)
                {
                    CreateSlotIcon(slotView.UnitAnchor, unit, slotIndex);
                }
            }
        }

        /// <summary>
        /// 회색 슬롯 중앙에 유닛 아이콘을 생성합니다.
        /// </summary>
        private void CreateSlotIcon(Transform unitAnchor, UnitFormationCombatLink.TransferredUnit unit, int slotIndex)
        {
            GameObject iconObject = new GameObject(
                $"BattleSlotIcon_{slotIndex:00}",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));

            RectTransform iconRect = iconObject.GetComponent<RectTransform>();

            iconRect.SetParent(unitAnchor, false);

            // 부모 UnitAnchor 영역 전체에 맞춤
            iconRect.anchorMin = Vector2.zero;
            iconRect.anchorMax = Vector2.one;
            iconRect.offsetMin = Vector2.zero;
            iconRect.offsetMax = Vector2.zero;
            iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.localScale = Vector3.one;

            Image iconImage = iconObject.GetComponent<Image>();

            iconImage.sprite = unit.Sprite;
            iconImage.color = unit.Color;
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;
            iconImage.enabled = unit.Sprite != null;

            createdSlotIcons.Add(iconObject);
        }

        /// <summary>
        /// 하단 카드의 앞쪽 두 칸에 서브 유닛을 표시합니다.
        /// </summary>
        private void CreateSupportCards(CombatUnitInfoView unitInfoView)
        {
            IReadOnlyList<UnitFormationCombatLink.TransferredUnit>
                supportUnits = UnitFormationCombatLink.SupportUnits;

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
                battleUnits = UnitFormationCombatLink.BattleUnits;

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

        private void ClearSlotIcons()
        {
            foreach (GameObject iconObject in createdSlotIcons)
            {
                if (iconObject != null)
                {
                    Destroy(iconObject);
                }
            }

            createdSlotIcons.Clear();
        }
    }
}
