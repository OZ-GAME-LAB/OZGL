using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Combat;
using OzGameLab01.Managers;

namespace OzGameLab01.UI
{
    public class BattleRewardPanel : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private TextMeshProUGUI swordLevelText;
        [SerializeField] private TextMeshProUGUI bowLevelText;
        [SerializeField] private TextMeshProUGUI staffLevelText;

        private void Awake()
        {
            if (panel != null)
            {
                panel.SetActive(false);
            }
        }

        public void Show(IEnumerable<Unit.SkillType> participatingSkillTypes)
        {
            HashSet<Unit.SkillType> leveledTypes = new HashSet<Unit.SkillType>(participatingSkillTypes);
            foreach (Unit.SkillType skillType in leveledTypes)
            {
                SceneTransitioner.LevelUp(skillType);
            }

            RefreshLevelTexts(leveledTypes);

            if (panel != null)
            {
                panel.SetActive(true);
            }
        }

        private void RefreshLevelTexts(HashSet<Unit.SkillType> leveledTypes)
        {
            SetLevelText(swordLevelText, "전사", SceneTransitioner.SwordLevel, leveledTypes.Contains(Unit.SkillType.Warrior));
            SetLevelText(bowLevelText, "궁수", SceneTransitioner.BowLevel, leveledTypes.Contains(Unit.SkillType.Archer));
            SetLevelText(staffLevelText, "마법사", SceneTransitioner.StaffLevel, leveledTypes.Contains(Unit.SkillType.Mage));
        }

        private static void SetLevelText(TextMeshProUGUI text, string label, int level, bool leveledUp)
        {
            if (text == null)
            {
                return;
            }

            text.text = leveledUp ? $"{label} Lv.{level} (+1)" : $"{label} Lv.{level}";
        }
    }
}
