using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OzGameLab01.UI.Battle
{
    /// <summary>
    /// 전투 제어 UI View입니다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BattleControlView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Button speedButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private TMP_Text speedText;

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

        /// <summary>
        /// 배속 버튼에 표시할 문구를 설정합니다.
        /// 실제 게임 배속은 호출하는 쪽에서 처리합니다.
        /// </summary>
        public void SetSpeedText(string value)
        {
            if (speedText != null)
            {
                speedText.text = value ?? string.Empty;
            }
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