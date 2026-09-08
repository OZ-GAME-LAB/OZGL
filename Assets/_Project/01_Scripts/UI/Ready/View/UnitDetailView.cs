using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OzGameLab01.UI
{
    [DisallowMultipleComponent]
    public sealed class UnitDetailView : MonoBehaviour
    {
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

        public bool IsVisible => gameObject.activeSelf;

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
            ClearSynergies();

            if (synergyNames == null || synergyBadgeRoot == null || synergyBadgeItemPrefab == null)
            {
                return;
            }

            foreach (string synergyName in synergyNames)
            {
                InfoSynergyItemView badgeItem = Instantiate(synergyBadgeItemPrefab,synergyBadgeRoot,false);
                badgeItem.SetName(synergyName ?? string.Empty);
                badgeItem.SetVisible(true);

                synergyBadgeItems.Add(badgeItem);
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
        }

        #endregion

        #region Private Methods

        private void ClearSynergies()
        {
            foreach (InfoSynergyItemView badgeItem in synergyBadgeItems)
            {
                if (badgeItem == null)
                    continue;

                badgeItem.SetVisible(false);
                Destroy(badgeItem.gameObject);
            }

            synergyBadgeItems.Clear();
        }

        #endregion
    }
}