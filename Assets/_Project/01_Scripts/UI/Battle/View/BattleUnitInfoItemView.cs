using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OzGameLab01.UI.Battle
{
    [DisallowMultipleComponent]
    public sealed class BattleUnitInfoItemView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Image portraitImage;
        [SerializeField] private TMP_Text unitNameText;
        [SerializeField] private Image skillIconImage;
        [SerializeField] private Transform skillContentRoot;
        [SerializeField] private GameObject gravePortraitObject;

        #region Properties

        public Image PortraitImage => portraitImage;
        public TMP_Text UnitNameText => unitNameText;
        public Image SkillIconImage => skillIconImage;
        public Transform SkillContentRoot => skillContentRoot;

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

        public void SetPortrait(Sprite sprite)
        {
            if (portraitImage == null)
            {
                return;
            }

            portraitImage.sprite = sprite;
            portraitImage.enabled = sprite != null;
        }

        public void SetUnitName(string value)
        {
            if (unitNameText != null)
            {
                unitNameText.text = value ?? string.Empty;
            }
        }

        public void SetSkillIcon(Sprite sprite)
        {
            if (skillIconImage == null)
            {
                return;
            }

            skillIconImage.sprite = sprite;
            skillIconImage.enabled = sprite != null;
        }

        public void SetSkillVisible(bool value)
        {
            if (skillContentRoot != null)
            {
                skillContentRoot.gameObject.SetActive(value);
            }
        }

        public void SetGraveVisible(bool value)
        {
            if (gravePortraitObject != null)
            {
                gravePortraitObject.SetActive(value);
            }
        }

        #endregion
    }
}