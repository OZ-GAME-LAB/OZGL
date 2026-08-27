using System;
using UnityEngine;
using UnityEngine.UI;

namespace OzGameLab01.UI
{
    public sealed class TitleUIView : MonoBehaviour
    {
        [Header("Resource")]
        [SerializeField] private UIResourceSO uiResourceSO;

        [Header("Main Canvas")]
        [SerializeField] private Image backgroundVisual;
        [SerializeField] private Image titleVisual;

        [SerializeField] private Button startButton;
        [SerializeField] private Button upgradeButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button exitButton;

        [Header("Overlay / Popup")]
        [SerializeField] private SettingsView settingsView;
        [SerializeField] private ExitConfirmView exitConfirmView;

        #region Properties

        public SettingsView Settings => settingsView;
        public ExitConfirmView ExitConfirm => exitConfirmView;
        public bool IsVisible => gameObject.activeSelf;
        public bool IsSettingsVisible => settingsView != null && settingsView.IsVisible;
        public bool IsExitConfirmVisible => exitConfirmView != null && exitConfirmView.IsVisible;

        #endregion

        #region Events

        public event Action StartRequested;
        public event Action UpgradeRequested;
        public event Action ExitConfirmed;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            startButton.onClick.AddListener(OnStartClicked);
            upgradeButton.onClick.AddListener(OnUpgradeClicked);
            settingsButton.onClick.AddListener(OnSettingsClicked);
            exitButton.onClick.AddListener(OnExitClicked);

            settingsView.CloseRequested += HideSettings;

            exitConfirmView.ConfirmRequested += OnExitConfirmed;
            exitConfirmView.CancelRequested += HideExitConfirm;

            HideSettings();
            HideExitConfirm();

            ApplyResources();
        }

        private void OnDestroy()
        {
            startButton.onClick.RemoveListener(OnStartClicked);
            upgradeButton.onClick.RemoveListener(OnUpgradeClicked);
            settingsButton.onClick.RemoveListener(OnSettingsClicked);
            exitButton.onClick.RemoveListener(OnExitClicked);

            settingsView.CloseRequested -= HideSettings;

            exitConfirmView.ConfirmRequested -= OnExitConfirmed;
            exitConfirmView.CancelRequested -= HideExitConfirm;
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

        public void ShowSettings(SettingsTab tab = SettingsTab.Game)
        {
            settingsView.Show(tab);
        }

        public void HideSettings()
        {
            settingsView.Hide();
        }

        public void ShowExitConfirm()
        {
            exitConfirmView.Show();
        }

        public void HideExitConfirm()
        {
            exitConfirmView.Hide();
        }

        #endregion

        #region Private Methods

        [ContextMenu("Apply UI Resources")]
        private void ApplyResources()
        {
            if (uiResourceSO == null || uiResourceSO.titleUi == null)
                return;

            TitleUiSO resources = uiResourceSO.titleUi;

            TitleUiStyleApplier.Apply(backgroundVisual,resources.main.background);
            TitleUiStyleApplier.Apply(titleVisual,resources.main.title);
            TitleUiStyleApplier.Apply(startButton,resources.main.startButton);
            TitleUiStyleApplier.Apply(upgradeButton,resources.main.upgradeButton);
            TitleUiStyleApplier.Apply(settingsButton,resources.main.settingsButton);
            TitleUiStyleApplier.Apply(exitButton,resources.main.exitButton);

            settingsView.ApplyResources(resources.settings);
            exitConfirmView.ApplyResources(resources.exitConfirm);
        }

        private void OnStartClicked()
        {
            StartRequested?.Invoke();
        }

        private void OnUpgradeClicked()
        {
            UpgradeRequested?.Invoke();
        }

        private void OnSettingsClicked()
        {
            ShowSettings();
        }

        private void OnExitClicked()
        {
            ShowExitConfirm();
        }

        private void OnExitConfirmed()
        {
            ExitConfirmed?.Invoke();
        }

        #endregion
    }
}
