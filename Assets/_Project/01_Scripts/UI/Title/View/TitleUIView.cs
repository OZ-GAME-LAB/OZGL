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

        public SettingsView Settings => settingsView; //타이틀 화면에 연결됨, 외부에서 설정 때문에 ui에 접근할 때 사용해요
        public ExitConfirmView ExitConfirm => exitConfirmView;
        public bool IsVisible => gameObject.activeSelf;
        public bool IsSettingsVisible => settingsView != null && settingsView.IsVisible;
        public bool IsExitConfirmVisible => exitConfirmView != null && exitConfirmView.IsVisible;

        #endregion

        #region Events

        public event Action StartRequested; //시작 버튼을 누르면 발생하는 이벤트
        public event Action UpgradeRequested; //업그레이드 버튼을 누르면 발생하는 이벤트
        public event Action ExitConfirmed; //종료 확인 버튼을 누르면 발생하는 이벤트

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

        #endregion

        #region Private Methods
        
        /// <summary>
        /// Ui리소스SO에 정의된 타이틀 스타일을 메인 화면과 하위 파업에 일괄 적용함
        /// Awake에서 초기 ui 외형을 구성하며, ContextMenu를 통해 에디터에서 바로 적용 가능
        /// 그래서 만약에 필요하다면 매니저 쪽에서 Init()해도 되고, 그냥 Awake에서 바로 적용해도 됨
        /// </summary>
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
