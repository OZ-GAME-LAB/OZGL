using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using OzGameLab01.Controllers;
using OzGameLab01.Managers;

namespace OzGameLab01.UI
{
    public class LevelUpSelector : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private Button swordButton;
        [SerializeField] private Button bowButton;
        [SerializeField] private Button staffButton;
        [SerializeField] private TextMeshProUGUI swordLevelText;
        [SerializeField] private TextMeshProUGUI bowLevelText;
        [SerializeField] private TextMeshProUGUI staffLevelText;

        private Action _onSelected;

        private void Awake()
        {
            if (panel != null)
            {
                panel.SetActive(false);
            }

            if (swordButton != null)
            {
                swordButton.onClick.AddListener(() => Select(Unit.SkillType.Warrior));
            }

            if (bowButton != null)
            {
                bowButton.onClick.AddListener(() => Select(Unit.SkillType.Archer));
            }

            if (staffButton != null)
            {
                staffButton.onClick.AddListener(() => Select(Unit.SkillType.Mage));
            }
        }

        public void Show(Action onComplete)
        {
            _onSelected = onComplete;
            RefreshLevelTexts();

            if (panel != null)
            {
                panel.SetActive(true);
            }
        }

        private void RefreshLevelTexts()
        {
            if (swordLevelText != null)
            {
                swordLevelText.text = "Lv." + SceneTransitioner.SwordLevel;
            }

            if (bowLevelText != null)
            {
                bowLevelText.text = "Lv." + SceneTransitioner.BowLevel;
            }

            if (staffLevelText != null)
            {
                staffLevelText.text = "Lv." + SceneTransitioner.StaffLevel;
            }
        }

        private void Select(Unit.SkillType skillType)
        {
            SceneTransitioner.LevelUp(skillType);

            if (panel != null)
            {
                panel.SetActive(false);
            }

            _onSelected?.Invoke();
        }
    }
}
