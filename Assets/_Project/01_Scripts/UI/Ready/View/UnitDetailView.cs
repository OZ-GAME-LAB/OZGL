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

        private readonly List<InfoSynergyItemView> synergyBadgeItems =
            new List<InfoSynergyItemView>();

        #region API

        public void SetUnitIcon(Sprite icon)
        {
            if (unitIcon == null)
            {
                return;
            }

            unitIcon.sprite = icon;
            unitIcon.enabled = icon != null;
        }

        public void SetUnitName(string unitName)
        {
            if (unitNameText != null)
            {
                unitNameText.text = unitName;
            }
        }

        public void SetSynergies(IReadOnlyList<string> synergyNames)
        {
            ClearSynergies();

            if (synergyNames == null ||
                synergyBadgeRoot == null ||
                synergyBadgeItemPrefab == null)
            {
                return;
            }

            foreach (string synergyName in synergyNames)
            {
                InfoSynergyItemView badgeItem = Instantiate(
                    synergyBadgeItemPrefab,
                    synergyBadgeRoot);

                badgeItem.SetName(synergyName);
                synergyBadgeItems.Add(badgeItem);
            }
        }

        public void SetSkill(int skillIndex, Sprite icon, string description)
        {
            if (skillIndex < 0 ||
                skillIndex >= skillIcons.Length ||
                skillIndex >= skillDescriptionTexts.Length)
            {
                return;
            }

            Image skillIcon = skillIcons[skillIndex];

            if (skillIcon != null)
            {
                skillIcon.sprite = icon;
                skillIcon.enabled = icon != null;
            }

            TMP_Text descriptionText = skillDescriptionTexts[skillIndex];

            if (descriptionText != null)
            {
                descriptionText.text = description;
            }
        }

        public void SetConceptDescription(string description)
        {
            if (conceptDescriptionText != null)
            {
                conceptDescriptionText.text = description;
            }
        }

        public void ClearDetail()
        {
            SetUnitIcon(null);
            SetUnitName(string.Empty);
            ClearSynergies();

            for (int index = 0; index < skillIcons.Length; index++)
            {
                SetSkill(index, null, string.Empty);
            }

            SetConceptDescription(string.Empty);
        }

        #endregion

        #region Private Methods

        private void ClearSynergies()
        {
            foreach (InfoSynergyItemView badgeItem in synergyBadgeItems)
            {
                if (badgeItem != null)
                {
                    Destroy(badgeItem.gameObject);
                }
            }

            synergyBadgeItems.Clear();
        }

        #endregion
    }
}