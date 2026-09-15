using System;
using UnityEngine;
using UnityEngine.UI;

namespace OzGameLab01.UI.Title
{
    public sealed class TitleUIView : MonoBehaviour
    {
        [Header("Main Canvas")]
        [UnityEngine.Serialization.FormerlySerializedAs("backgroundVisual")]
        [SerializeField] private Image _backgroundVisual;
        [UnityEngine.Serialization.FormerlySerializedAs("titleVisual")]
        [SerializeField] private Image _titleVisual;

        [UnityEngine.Serialization.FormerlySerializedAs("startButton")]

        [SerializeField] private Button _startButton;
        [UnityEngine.Serialization.FormerlySerializedAs("continueButton")]
        [SerializeField] private Button _continueButton;
        [UnityEngine.Serialization.FormerlySerializedAs("settingsButton")]
        [SerializeField] private Button _settingsButton;
        [UnityEngine.Serialization.FormerlySerializedAs("exitButton")]
        [SerializeField] private Button _exitButton;

        [Header("Overlay / Popup")]
        [UnityEngine.Serialization.FormerlySerializedAs("settingsView")]
        [SerializeField] private TitleSettingsView _settingsView;
        [UnityEngine.Serialization.FormerlySerializedAs("exitConfirmView")]
        [SerializeField] private ExitConfirmView _exitConfirmView;

        #region Properties

        public TitleSettingsView Settings => _settingsView; //타이틀 화면에 연결됨, 외부에서 설정 때문에 ui에 접근할 때 사용해요
        public ExitConfirmView ExitConfirm => _exitConfirmView;
        public bool IsVisible => gameObject.activeSelf;
        public bool IsSettingsVisible => _settingsView != null && _settingsView.IsVisible;
        public bool IsExitConfirmVisible => _exitConfirmView != null && _exitConfirmView.IsVisible;

        #endregion

        #region Events

        public event Action StartRequested; //시작 버튼을 누르면 발생하는 이벤트
        public event Action ContinueRequested; //이어하기 버튼을 누르면 발생하는 이벤트
        public event Action ExitConfirmed; //종료 확인 버튼을 누르면 발생하는 이벤트

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _startButton.onClick.AddListener(OnStartClicked);
            _continueButton.onClick.AddListener(OnContinueClicked);
            _settingsButton.onClick.AddListener(OnSettingsClicked);
            _exitButton.onClick.AddListener(OnExitClicked);

            _settingsView.CloseRequested += HideSettings;

            _exitConfirmView.ConfirmRequested += OnExitConfirmed;
            _exitConfirmView.CancelRequested += HideExitConfirm;

            HideSettings();
            HideExitConfirm();
        }

        private void OnDestroy()
        {
            _startButton.onClick.RemoveListener(OnStartClicked);
            _continueButton.onClick.RemoveListener(OnContinueClicked);
            _settingsButton.onClick.RemoveListener(OnSettingsClicked);
            _exitButton.onClick.RemoveListener(OnExitClicked);

            _settingsView.CloseRequested -= HideSettings;

            _exitConfirmView.ConfirmRequested -= OnExitConfirmed;
            _exitConfirmView.CancelRequested -= HideExitConfirm;
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
            _settingsView.Show(tab);
        }

        public void HideSettings()
        {
            _settingsView.Hide();
        }
        
        /// <summary>
        /// 종료 확인 팝업 표시
        /// </summary>
        public void ShowExitConfirm()
        {
            _exitConfirmView.Show();
        }

        public void HideExitConfirm()
        {
            _exitConfirmView.Hide();
        }

        public void SetContinueInteractable(bool isInteractable)
        {
            _continueButton.interactable = isInteractable;
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
