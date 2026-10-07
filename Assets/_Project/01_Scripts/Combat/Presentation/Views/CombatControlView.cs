using System;
using UnityEngine;
using UnityEngine.UI;

namespace OzGameLab01.UI.Battle
{
    /// <summary>
    /// 전투 제어 UI View입니다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CombatControlView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Button speedButton;
        [SerializeField] private Button settingsButton;

        [Header("Speed Icons")]
        [SerializeField] private Image speedIconImage;
        [SerializeField] private Sprite normalSpeedSprite;
        [SerializeField] private Sprite fastSpeedSprite;

        #region Properties

        public Button SpeedButton => speedButton;
        public Button SettingsButton => settingsButton;

        public bool IsVisible => gameObject.activeSelf;

        public event Action<CombatControlView> SpeedClicked;
        public event Action<CombatControlView> SettingsClicked;

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

        /// <summary>
        /// 현재 배속에 맞는 아이콘을 표시합니다.
        /// 실제 배속 변경과 클릭 처리는 호출하는 쪽에서 담당합니다.
        /// </summary>
        /// <param name="isFastForward">false: 1x, true: 2x</param>
        public void SetSpeedVisual(bool isFastForward)
        {
            if (speedIconImage == null)
            {
                return;
            }

            speedIconImage.sprite = isFastForward ? fastSpeedSprite : normalSpeedSprite;
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

        #region Event Handlers

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