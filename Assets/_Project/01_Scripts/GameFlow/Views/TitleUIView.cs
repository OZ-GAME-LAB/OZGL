using System;
using UnityEngine;
using UnityEngine.UI;

namespace OzGameLab01.UI.Title
{
    public sealed class TitleUIView : MonoBehaviour
    {
        [Header("Main Canvas")]
        [SerializeField] private Image backgroundVisual;
        [SerializeField] private Image titleVisual;

        [SerializeField] private Button startButton;
        [SerializeField] private Button continueButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button exitButton;

        [Header("Overlay / Popup")]
        [SerializeField] private TitleSettingsView settingsView;
        [SerializeField] private ExitConfirmView exitConfirmView;

        #region Properties

        public TitleSettingsView Settings => settingsView; //타이틀 화면에 연결됨, 외부에서 설정 때문에 ui에 접근할 때 사용해요
        public ExitConfirmView ExitConfirm => exitConfirmView;
        public bool IsVisible => gameObject.activeSelf;
        public bool IsSettingsVisible => settingsView != null && settingsView.IsVisible;
        public bool IsExitConfirmVisible => exitConfirmView != null && exitConfirmView.IsVisible;

        #endregion

        #region Events

        public event Action StartRequested; //시작 버튼을 누르면 발생하는 이벤트
        public event Action ContinueRequested; //이어하기 버튼을 누르면 발생하는 이벤트
        public event Action ExitConfirmed; //종료 확인 버튼을 누르면 발생하는 이벤트

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            startButton.onClick.AddListener(OnStartClicked);
            continueButton.onClick.AddListener(OnContinueClicked);
            settingsButton.onClick.AddListener(OnSettingsClicked);
            exitButton.onClick.AddListener(OnExitClicked);

            settingsView.CloseRequested += HideSettings;

            exitConfirmView.ConfirmRequested += OnExitConfirmed;
            exitConfirmView.CancelRequested += HideExitConfirm;

            HideSettings();
            HideExitConfirm();
        }

        private void OnDestroy()
        {
            startButton.onClick.RemoveListener(OnStartClicked);
            continueButton.onClick.RemoveListener(OnContinueClicked);
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
        
        /// <summary>
        /// 종료 확인 팝업 표시
        /// </summary>
        public void ShowExitConfirm()
        {
            exitConfirmView.Show();
        }

        public void HideExitConfirm()
        {
            exitConfirmView.Hide();
        }

        public void SetContinueInteractable(bool isInteractable)
        {
            continueButton.interactable = isInteractable;
        }

        #endregion

        #region Private Methods

        private void OnStartClicked()
        {
            StartRequested?.Invoke();
        }

        private void OnContinueClicked()
        {
            ContinueRequested?.Invoke();
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
