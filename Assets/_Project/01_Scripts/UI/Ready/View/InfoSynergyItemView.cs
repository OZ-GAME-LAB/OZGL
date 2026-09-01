using TMPro;
using UnityEngine;

namespace OzGameLab01.UI
{
    [DisallowMultipleComponent]
    public sealed class InfoSynergyItemView : MonoBehaviour
    {
        [SerializeField] private TMP_Text nameText;

        public void SetName(string synergyName)
        {
            if (nameText != null)
            {
                nameText.text = synergyName;
            }
        }

        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }
    }
}