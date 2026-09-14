using UnityEngine;
using UnityEngine.UI;

namespace OzGameLab01.UI.Battle
{
    [DisallowMultipleComponent]
    public sealed class EnemyBattleUnitItemView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform visualRoot;
        [SerializeField] private Image unitImage;
        [SerializeField] private Image healthFillImage;
        [SerializeField] private Image manaFillImage;
        [SerializeField] private Transform statusEffectRoot;

        #region Properties

        public Transform VisualRoot => visualRoot;
        public Image UnitImage => unitImage;
        public Transform StatusEffectRoot => statusEffectRoot;

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

        public void SetUnitSprite(Sprite sprite)
        {
            if (unitImage == null)
            {
                return;
            }

            unitImage.sprite = sprite;
            unitImage.enabled = sprite != null;
        }

        public void SetHealthRatio(float value)
        {
            SetFillRatio(healthFillImage, value);
        }

        public void SetManaRatio(float value)
        {
            SetFillRatio(manaFillImage, value);
        }

        public void SetVisualVisible(bool value)
        {
            if (visualRoot != null)
            {
                visualRoot.gameObject.SetActive(value);
            }
        }

        public void SetStatusEffectRootVisible(bool value)
        {
            if (statusEffectRoot != null)
            {
                statusEffectRoot.gameObject.SetActive(value);
            }
        }

        #endregion

        #region Private Methods

        private static void SetFillRatio(Image fillImage, float value)
        {
            if (fillImage == null)
            {
                return;
            }

            RectTransform rectTransform = fillImage.rectTransform;
            Vector2 anchorMax = rectTransform.anchorMax;

            anchorMax.x = Mathf.Clamp01(value);
            rectTransform.anchorMax = anchorMax;
        }

        #endregion
    }
}