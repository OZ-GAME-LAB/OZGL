using UnityEngine;
using UnityEngine.UI;

namespace OzGameLab01.UI.Battle
{
    [DisallowMultipleComponent]
    public sealed class PlayerBattleUnitItemView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform visualRoot;
        [SerializeField] private Image unitImage;
        [SerializeField] private Image healthFillImage;
        [SerializeField] private Image manaFillImage;

        [Header("Effect Anchors")]
        [SerializeField] private GameObject anchorRoot;
        [SerializeField] private Transform projectileAnchor;
        [SerializeField] private Transform hitEffectAnchor;
        [SerializeField] private Transform skillEffectAnchor;

        #region Properties

        public Transform VisualRoot => visualRoot;
        public Image UnitImage => unitImage;
        public Transform ProjectileAnchor => projectileAnchor;
        public Transform HitEffectAnchor => hitEffectAnchor;
        public Transform SkillEffectAnchor => skillEffectAnchor;

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

        public void SetEffectAnchorsVisible(bool value)
        {
            if (anchorRoot != null)
            {
                anchorRoot.SetActive(value);
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