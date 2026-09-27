using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OzGameLab01.UI
{
    /// <summary>
    /// 유닛 데이터를 표시하고 호버 팝업의 입력 통과를 관리합니다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class UnitDetailView : MonoBehaviour
    {
        [Header("Stats")]
        [SerializeField] private CombatInfoStatItemView[] statItems;
        [SerializeField] private ScrollRect statsScrollRect;

        [Header("Selected Unit")]
        [SerializeField] private Image unitIcon;
        [SerializeField] private TMP_Text unitNameText;
        [SerializeField] private Transform synergyBadgeRoot;
        [SerializeField] private InfoSynergyItemView synergyBadgeItemPrefab;

        [Header("Skills")]
        [SerializeField] private Image[] skillIcons;
        [SerializeField] private TMP_Text[] skillDescriptionTexts;

        [Header("Description")]
        [SerializeField] private TMP_Text conceptDescriptionText;

        private readonly List<InfoSynergyItemView> synergyBadgeItems = new ();
        private Sprite[] defaultStatIcons;

        public bool IsVisible => gameObject.activeSelf;

        private void Awake()
        {
            CacheDefaultStatIcons();
            CacheSynergyBadgeItems();
        }

        /// <summary>
        /// 스탯만 필요한 기존 호출 경로입니다. 상세 데이터는 빈 값으로 초기화합니다.
        /// </summary>
        public void LoadUnit(OzGameLab01.Data.UnitData data, Sprite icon)
        {
            LoadUnit(data, icon, null, null, null, null, null);
        }

        /// <summary>
        /// 편성 창에서 선택한 유닛의 모든 상세 표시 데이터를 갱신합니다.
        /// </summary>
        public void LoadUnit(
            OzGameLab01.Data.UnitData data,
            Sprite icon,
            IReadOnlyList<string> synergyNames,
            OzGameLab01.Data.SkillData activeSkill,
            Sprite activeSkillIcon,
            OzGameLab01.Data.SkillData passiveSkill,
            Sprite passiveSkillIcon)
        {
            if (data == null)
            {
                ClearDetail();
                return;
            }

            SetUnitIcon(icon);

            if (unitIcon != null)
            {
                unitIcon.color = Color.white;
            }
            SetUnitName(data.name);
            SetSynergies(synergyNames);
            SetSkill(0, activeSkillIcon, FormatSkill(activeSkill));
            SetSkill(1, passiveSkillIcon, FormatSkill(passiveSkill));
            SetConceptDescription(data.flavorText);
            BindStats(data);
            ResetStatsScroll();
        }

        private static string FormatSkill(OzGameLab01.Data.SkillData skill)
        {
            if (skill == null)
                return string.Empty;

            if (string.IsNullOrWhiteSpace(skill.name))
                return skill.description ?? string.Empty;

            if (string.IsNullOrWhiteSpace(skill.description))
                return skill.name;

            return $"<b>{skill.name}</b>\n{skill.description}";
        }

        private void BindStats(OzGameLab01.Data.UnitData data)
        {
            CacheDefaultStatIcons();
            BindStat(0, data.healthPoint, false, "체력", "전투 시작 시 보유하는 최대 체력입니다.");
            BindStat(1, data.attackPoint, false, "공격력", "기본 공격과 공격력 계수 스킬의 기준값입니다.");
            BindStat(2, data.defensePoint, false, "방어력", "받는 피해를 감소시키는 방어 수치입니다.");
            BindStat(3, data.attackSpeed, false, "공격 속도", "기본 공격 주기에 적용되는 공격 속도입니다.");
            BindStat(4, data.criticalRate, true, "치명타 확률", "공격이 치명타로 적중할 확률입니다.");
            BindStat(5, data.criticalMult, true, "치명타 피해", "치명타 적중 시 적용되는 피해 배율입니다.");
            BindStat(6, data.dodgeRate, true, "회피율", "적의 공격을 회피할 확률입니다.");
        }

        private void BindStat(int index, float value, bool percent, string title, string description)
        {
            Sprite icon = defaultStatIcons != null && index >= 0 && index < defaultStatIcons.Length
                ? defaultStatIcons[index]
                : null;
            string formattedValue = value.ToString("0.##") + (percent ? "%" : string.Empty);
            SetStat(index, icon, formattedValue, title, description);
        }

        private void CacheDefaultStatIcons()
        {
            if (defaultStatIcons != null)
                return;

            int count = statItems != null ? statItems.Length : 0;
            defaultStatIcons = new Sprite[count];

            for (int index = 0; index < count; index++)
                defaultStatIcons[index] = statItems[index] != null ? statItems[index].Icon : null;
        }

        private void OnEnable()
        {
            // 팝업과 자식 그래픽의 클릭 및 드래그 가로채기 방지 //삭제 대상
            //foreach (Graphic graphic in GetComponentsInChildren<Graphic>(true))
            //    graphic.raycastTarget = false;
            //foreach (CanvasGroup group in GetComponentsInChildren<CanvasGroup>(true))
            //{
            //    group.blocksRaycasts = false;
            //    group.interactable = false;
            //}
        }

        #region API

        /// <summary>
        /// 현재 내용으로 패널을 표시합니다.
        /// </summary>
        public void Show()
        {
            gameObject.SetActive(true);
        }

        /// <summary>
        /// 내용을 유지한 채 패널을 숨깁니다.
        /// </summary>
        public void Hide()
        {
            gameObject.SetActive(false);
        }

        public void SetUnitIcon(Sprite icon)
        {
            if (unitIcon == null)
                return;

            unitIcon.sprite = icon;
            unitIcon.enabled = icon != null;
        }

        public void SetUnitName(string unitName)
        {
            if (unitNameText != null)
                unitNameText.text = unitName ?? string.Empty;
        }

        public void SetSynergies(IReadOnlyList<string> synergyNames)
        {
            CacheSynergyBadgeItems();
            ClearSynergies();

            if (synergyNames == null || synergyBadgeRoot == null || synergyBadgeItemPrefab == null)
            {
                return;
            }

            for (int index = 0; index < synergyNames.Count; index++)
            {
                InfoSynergyItemView badgeItem;
                if (index < synergyBadgeItems.Count)
                {
                    badgeItem = synergyBadgeItems[index];
                }
                else
                {
                    badgeItem = Instantiate(synergyBadgeItemPrefab, synergyBadgeRoot, false);
                    synergyBadgeItems.Add(badgeItem);
                }

                badgeItem.SetName(synergyNames[index] ?? string.Empty);
                badgeItem.SetVisible(true);
            }
        }

        /// <summary>
        /// 지정한 슬롯의 스킬 아이콘과 설명을 갱신합니다.
        /// 아이콘이 null이면 해당 Image만 숨깁니다.
        /// </summary>
        public void SetSkill(int skillIndex,Sprite icon,string description)
        {
            if (skillIndex < 0)
                return;

            if (skillIcons != null && skillIndex < skillIcons.Length)
            {
                Image targetIcon = skillIcons[skillIndex];

                if (targetIcon != null)
                {
                    targetIcon.sprite = icon;
                    targetIcon.enabled = icon != null;
                }
            }

            if (skillDescriptionTexts != null && skillIndex < skillDescriptionTexts.Length)
            {
                TMP_Text targetText = skillDescriptionTexts[skillIndex];

                if (targetText != null)
                    targetText.text = description ?? string.Empty;
            }
        }

        public void SetConceptDescription(string description)
        {
            if (conceptDescriptionText != null)
                conceptDescriptionText.text = description ?? string.Empty;
        }

        /// <summary>
        /// 표시 여부를 변경하지 않고 내용만 초기화합니다.
        /// </summary>
        public void ClearDetail()
        {
            SetUnitIcon(null);
            SetUnitName(string.Empty);
            ClearSynergies();

            int iconCount = skillIcons != null ? skillIcons.Length : 0;
            int descriptionCount = skillDescriptionTexts != null ? skillDescriptionTexts.Length : 0;
            int count = Mathf.Max(iconCount, descriptionCount);

            for (int index = 0; index < count; index++)
                SetSkill(index, null, string.Empty);

            SetConceptDescription(string.Empty);
            ClearStats();
        }

        public void SetStat(int index,Sprite icon,string value,string title,string description)
        {
            if (statItems == null || index < 0 || index >= statItems.Length)
                return;

            if (statItems[index] != null)
                statItems[index].Bind(icon, value, title, description);
        }

        public void ClearStats()
        {
            if (statItems == null)
                return;

            foreach (CombatInfoStatItemView item in statItems)
            {
                if (item != null)
                    item.Clear();
            }
        }

        /// <summary>
        /// 활성 상태의 스탯 목록을 맨 위로 이동합니다.
        /// 패널을 표시한 뒤 외부에서 호출하세요.
        /// </summary>
        public void ResetStatsScroll()
        {
            if (statsScrollRect == null ||
                !statsScrollRect.isActiveAndEnabled)
                return;

            Canvas.ForceUpdateCanvases();

            statsScrollRect.StopMovement();
            statsScrollRect.verticalNormalizedPosition = 1f;
        }

        #endregion

        #region Private Methods

        private void ClearSynergies()
        {
            CacheSynergyBadgeItems();

            foreach (InfoSynergyItemView badgeItem in synergyBadgeItems)
            {
                if (badgeItem == null)
                    continue;

                badgeItem.SetVisible(false);
            }
        }

        private void CacheSynergyBadgeItems()
        {
            if (synergyBadgeItems.Count > 0 || synergyBadgeRoot == null)
                return;

            synergyBadgeItems.AddRange(
                synergyBadgeRoot.GetComponentsInChildren<InfoSynergyItemView>(true));
        }

        private void BindUnitStats(OzGameLab01.Data.UnitData data)
        {
            ClearStats();

            SetStat(
                0,
                null,
                data.healthPoint.ToString("0.##"),
                "체력",
                "유닛의 최대 체력");

            SetStat(
                1,
                null,
                data.attackPoint.ToString("0.##"),
                "공격력",
                "유닛의 기본 공격력");

            SetStat(
                2,
                null,
                data.defensePoint.ToString("0.##"),
                "방어력",
                "유닛의 피해 감소 수치");

            SetStat(
                3,
                null,
                data.attackSpeed.ToString("0.##"),
                "공격속도",
                "유닛의 기본 공격 간격");

            SetStat(
                4,
                null,
                $"{data.criticalRate:0.##}%",
                "치명타 확률",
                "유닛의 치명타 발생 확률");

            SetStat(
                5,
                null,
                $"{data.criticalMult:0.##}%",
                "치명타 피해",
                "유닛의 치명타 피해 배율");

            SetStat(
                6,
                null,
                $"{data.dodgeRate:0.##}%",
                "회피율",
                "유닛의 공격 회피 확률");
        }

        #endregion
    }
}
