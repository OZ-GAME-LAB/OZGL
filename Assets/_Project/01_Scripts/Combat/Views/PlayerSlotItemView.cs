using UnityEngine;
using UnityEngine.UI;

namespace OzGameLab01.UI.Battle
{
    [DisallowMultipleComponent]
    public sealed class PlayerSlotItemView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Image slotVisualImage;
        [SerializeField] private Transform unitAnchor;

        #region Properties

        public Image SlotVisualImage => slotVisualImage;
        public Transform UnitAnchor => unitAnchor;

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

        public void SetSlotSprite(Sprite sprite)
        {
            if (slotVisualImage != null)
            {
                slotVisualImage.sprite = sprite;
            }
        }

        public void SetSlotColor(Color color)
        {
            if (slotVisualImage != null)
            {
                slotVisualImage.color = color;
            }
        }

        public void SetSlotVisualVisible(bool value)
        {
            if (slotVisualImage != null)
            {
                slotVisualImage.enabled = value;
            }
        }

        #endregion
    }
}