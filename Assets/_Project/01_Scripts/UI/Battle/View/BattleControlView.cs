using System;
using UnityEngine;
using UnityEngine.UI;

namespace OzGameLab01.UI.Battle
{
    [DisallowMultipleComponent]
    public sealed class BattleControlView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Button speedButton;
        [SerializeField] private Button settingsButton;

        #region Properties

        public Button SpeedButton => speedButton;
        public Button SettingsButton => settingsButton;

        public bool IsVisible => gameObject.activeSelf;

        public event Action<BattleControlView> SpeedClicked;
        public event Action<BattleControlView> SettingsClicked;

        #endregion

        #region Lifecycle

        private void OnEnable()
        {
            if (speedButton != null)
            {
                speedButton.onClick.AddListener(HandleSpeedButtonClick);
            }

            if (settingsButton != null)
            {
                settingsButton.onClick.AddListener(HandleSettingsButtonClick);
            }
        }

        private void OnDisable()
        {
            if (speedButton != null)
            {
                speedButton.onClick.RemoveListener(HandleSpeedButtonClick);
            }

            if (settingsButton != null)
            {
                settingsButton.onClick.RemoveListener(HandleSettingsButtonClick);
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

        public void SetSpeedButtonInteractable(bool value)
        {
            if (speedButton != null)
            {
                speedButton.interactable = value;
            }
        }

        public void SetSettingsButtonInteractable(bool value)
        {
            if (settingsButton != null)
            {
                settingsButton.interactable = value;
            }
        }

        #endregion

        #region Private Methods

        private void HandleSpeedButtonClick()
        {
            SpeedClicked?.Invoke(this);
        }

        private void HandleSettingsButtonClick()
        {
            SettingsClicked?.Invoke(this);
        }

        #endregion
    }
}