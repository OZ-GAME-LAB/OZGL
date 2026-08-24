using TMPro;
using UnityEngine;
using OzGameLab01.Managers;

namespace OzGameLab01.UI
{
    public class PartyStatusUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI swordLevelText;
        [SerializeField] private TextMeshProUGUI bowLevelText;
        [SerializeField] private TextMeshProUGUI staffLevelText;

        private void Start()
        {
            Refresh();
        }

        public void Refresh()
        {
            if (swordLevelText != null)
            {
                swordLevelText.text = "SWD Lv." + SceneTransitioner.SwordLevel;
            }

            if (bowLevelText != null)
            {
                bowLevelText.text = "BOW Lv." + SceneTransitioner.BowLevel;
            }

            if (staffLevelText != null)
            {
                staffLevelText.text = "STF Lv." + SceneTransitioner.StaffLevel;
            }
        }
    }
}
