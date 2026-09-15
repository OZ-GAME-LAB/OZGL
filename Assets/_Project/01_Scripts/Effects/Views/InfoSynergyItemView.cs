using TMPro;
using UnityEngine;

namespace OzGameLab01.UI
{
    [DisallowMultipleComponent]
    public sealed class InfoSynergyItemView : MonoBehaviour
    {
        [UnityEngine.Serialization.FormerlySerializedAs("nameText")]
        [SerializeField] private TMP_Text _nameText;

        public void SetName(string synergyName)
        {
            if (_nameText != null)
            {
                _nameText.text = synergyName;
            }
        }

        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }
    }
}