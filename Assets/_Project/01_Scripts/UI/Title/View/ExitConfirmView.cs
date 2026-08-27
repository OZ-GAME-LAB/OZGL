using System;
using UnityEngine;
using UnityEngine.UI;

namespace OzGameLab01.UI
{
    public sealed class ExitConfirmView : MonoBehaviour
    {
        [SerializeField] private Image dimmer;
        [SerializeField] private Image panel;

        [SerializeField] private Button confirmButton;
        [SerializeField] private Button cancelButton;

        #region Properties

        public bool IsVisible => gameObject.activeSelf;

        #endregion

        #region Events

        public event Action ConfirmRequested;
        public event Action CancelRequested;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            confirmButton.onClick.AddListener(OnConfirmClicked);
            cancelButton.onClick.AddListener(OnCancelClicked);
        }

        private void OnDestroy()
        {
            confirmButton.onClick.RemoveListener(OnConfirmClicked);
            cancelButton.onClick.RemoveListener(OnCancelClicked);
        }

        #endregion

        #region Public API

        public void Show()
        {
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        #endregion

        #region Internal API

        internal void ApplyResources(TitleExitConfirmResources resources)
        {
            if (resources == null)
                return;

            TitleUiStyleApplier.Apply(dimmer, resources.dimmer);
            TitleUiStyleApplier.Apply(panel, resources.panel);

            TitleUiStyleApplier.Apply(confirmButton, resources.confirmButton);
            TitleUiStyleApplier.Apply(cancelButton, resources.cancelButton);
        }

        #endregion

        #region Private Methods

        private void OnConfirmClicked()
        {
            ConfirmRequested?.Invoke();
        }

        private void OnCancelClicked()
        {
            CancelRequested?.Invoke();
        }

        #endregion
    }
}
