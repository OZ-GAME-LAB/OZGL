using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OzGameLab01.UI.Battle
{
    [DisallowMultipleComponent]
    public sealed class DpsInfoItemView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Image unitIconImage;
        [SerializeField] private TMP_Text unitNameText;
        [SerializeField] private TMP_Text dpsValueText;

        #region Properties

        public Image UnitIconImage => unitIconImage;
        public TMP_Text UnitNameText => unitNameText;
        public TMP_Text DpsValueText => dpsValueText;

        public bool IsVisible => gameObject.activeSelf;

        #endregion

        #region API

        public void Show()
        {
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        public void SetUnitIcon(Sprite icon)
        {
            if (unitIconImage == null)
            {
                return;
            }

            unitIconImage.sprite = icon;
            unitIconImage.enabled = icon != null;
        }

        public void SetUnitName(string value)
        {
            if (unitNameText != null)
            {
                unitNameText.text = value ?? string.Empty;
            }
        }

        public void SetDpsText(string value)
        {
            if (dpsValueText != null)
            {
                dpsValueText.text = value ?? string.Empty;
            }
        }

        #endregion
    }
}