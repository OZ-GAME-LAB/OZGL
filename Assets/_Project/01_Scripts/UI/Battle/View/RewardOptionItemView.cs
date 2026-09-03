using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OzGameLab01.UI.Battle
{
    [DisallowMultipleComponent]
    public sealed class RewardOptionItemView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Button selectButton;
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text descriptionText;

        #region Properties

        public Button SelectButton => selectButton;
        public Image IconImage => iconImage;
        public TMP_Text DescriptionText => descriptionText;

        public bool IsVisible => gameObject.activeSelf;

        public event Action<RewardOptionItemView> Selected;

        #endregion

        #region Lifecycle

        private void OnEnable()
        {
            if (selectButton != null)
            {
                selectButton.onClick.AddListener(HandleSelectButtonClick);
            }
        }

        private void OnDisable()
        {
            if (selectButton != null)
            {
                selectButton.onClick.RemoveListener(HandleSelectButtonClick);
            }
        }

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

        public void SetIcon(Sprite icon)
        {
            if (iconImage == null)
            {
                return;
            }

            iconImage.sprite = icon;
            iconImage.enabled = icon != null;
        }

        public void SetDescription(string value)
        {
            if (descriptionText != null)
            {
                descriptionText.text = value ?? string.Empty;
            }
        }

        public void SetInteractable(bool value)
        {
            if (selectButton != null)
            {
                selectButton.interactable = value;
            }
        }

        #endregion

        #region Private Methods

        private void HandleSelectButtonClick()
        {
            Selected?.Invoke(this);
        }

        #endregion
    }
}