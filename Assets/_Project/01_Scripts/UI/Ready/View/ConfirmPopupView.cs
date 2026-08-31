using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OzGameLab01.UI
{
    [DisallowMultipleComponent]
    public sealed class ConfirmPopupView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TMP_Text messageText;
        [SerializeField] private TMP_Text warningText;
        [SerializeField] private Button confirmButton;
        [SerializeField] private TMP_Text confirmButtonText;
        [SerializeField] private Button cancelButton;
        [SerializeField] private TMP_Text cancelButtonText;

        #region Properties

        public TMP_Text MessageText => messageText;
        public TMP_Text WarningText => warningText;
        public Button ConfirmButton => confirmButton;
        public Button CancelButton => cancelButton;

        public bool IsVisible => gameObject.activeSelf;

        public string Message
        {
            get => messageText != null ? messageText.text : string.Empty;
            set
            {
                if (messageText != null)
                {
                    messageText.text = value ?? string.Empty;
                }
            }
        }

        public string Warning
        {
            get => warningText != null ? warningText.text : string.Empty;
            set
            {
                if (warningText != null)
                {
                    warningText.text = value ?? string.Empty;
                }
            }
        }

        public bool ConfirmInteractable
        {
            get => confirmButton != null && confirmButton.interactable;
            set
            {
                if (confirmButton != null)
                {
                    confirmButton.interactable = value;
                }
            }
        }

        public bool CancelInteractable
        {
            get => cancelButton != null && cancelButton.interactable;
            set
            {
                if (cancelButton != null)
                {
                    cancelButton.interactable = value;
                }
            }
        }

        public event Action<ConfirmPopupView> ConfirmClicked; //확인 버튼 클릭 이벤트
        public event Action<ConfirmPopupView> CancelClicked; //취소 버튼 클릭 이벤트

        #endregion

        #region Lifecycle

        private void OnEnable()
        {
            if (confirmButton != null)
            {
                confirmButton.onClick.AddListener(HandleConfirmClick);
            }

            if (cancelButton != null)
            {
                cancelButton.onClick.AddListener(HandleCancelClick);
            }
        }

        private void OnDisable()
        {
            if (confirmButton != null)
            {
                confirmButton.onClick.RemoveListener(HandleConfirmClick);
            }

            if (cancelButton != null)
            {
                cancelButton.onClick.RemoveListener(HandleCancelClick);
            }
        }

        #endregion

        #region API

        public void Show()
        {
            gameObject.SetActive(true);
        }

        public void Show(string message)
        {
            SetMessage(message);
            Show();
        }

        public void Show(string message, string warning)
        {
            SetMessage(message);
            SetWarning(warning);
            SetWarningVisible(!string.IsNullOrEmpty(warning));
            Show();
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        public void SetMessage(string value)
        {
            Message = value;
        }

        public void SetWarning(string value)
        {
            Warning = value;
        }

        public void SetWarningVisible(bool visible)
        {
            if (warningText != null)
            {
                warningText.gameObject.SetActive(visible);
            }
        }

        public void SetConfirmButtonText(string value)
        {
            if (confirmButtonText != null)
            {
                confirmButtonText.text = value ?? string.Empty;
            }
        }

        public void SetCancelButtonText(string value)
        {
            if (cancelButtonText != null)
            {
                cancelButtonText.text = value ?? string.Empty;
            }
        }

        public void SetCancelButtonVisible(bool visible)
        {
            if (cancelButton != null)
            {
                cancelButton.gameObject.SetActive(visible);
            }
        }

        #endregion

        #region Private Methods

        private void HandleConfirmClick()
        {
            ConfirmClicked?.Invoke(this);
        }

        private void HandleCancelClick()
        {
            CancelClicked?.Invoke(this);
        }

        #endregion
    }
}