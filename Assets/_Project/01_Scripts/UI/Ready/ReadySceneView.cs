using UnityEngine;
using ReadySettingsView = OzGameLab01.UI.Settings.SettingsView;

namespace OzGameLab01.UI
{
    [DisallowMultipleComponent]
    public sealed class ReadySceneView : MonoBehaviour
    {
        [Header("Views")]
        [SerializeField] private ReadyMainView mainView;
        [SerializeField] private RollView rollView;
        [SerializeField] private UnitView unitView;
        [SerializeField] private TooltipView tooltipView;
        [SerializeField] private ReadySettingsView settingsView;
        [SerializeField] private ConfirmPopupView confirmPopupView;

        #region Properties

        public ReadyMainView MainView => mainView;
        public RollView RollView => rollView;
        public UnitView UnitView => unitView;
        public TooltipView TooltipView => tooltipView;
        public ReadySettingsView SettingsView => settingsView;
        public ConfirmPopupView ConfirmPopupView => confirmPopupView;

        #endregion

        #region API

        public void ShowMainView()
        {
            if (mainView != null)
            {
                mainView.Show();
            }
        }

        public void HideMainView()
        {
            if (mainView != null)
            {
                mainView.Hide();
            }
        }

        public void ShowRollView()
        {
            if (rollView != null)
            {
                rollView.Show();
            }
        }

        public void HideRollView()
        {
            if (rollView != null)
            {
                rollView.Hide();
            }
        }

        public void ShowUnitView()
        {
            if (unitView != null)
            {
                unitView.Show();
            }
        }

        public void HideUnitView()
        {
            if (unitView != null)
            {
                unitView.Hide();
            }
        }

        public void ShowSettingsView()
        {
            if (settingsView != null)
            {
                settingsView.Show();
            }
        }

        public void HideSettingsView()
        {
            if (settingsView != null)
            {
                settingsView.Hide();
            }
        }

        public void ShowConfirmPopup()
        {
            if (confirmPopupView != null)
            {
                confirmPopupView.Show();
            }
        }

        public void HideConfirmPopup()
        {
            if (confirmPopupView != null)
            {
                confirmPopupView.Hide();
            }
        }

        public void HideTooltip()
        {
            if (tooltipView != null)
            {
                tooltipView.Hide();
            }
        }

        public void HideAllOverlayViews()
        {
            HideRollView();
            HideUnitView();
            HideSettingsView();
            HideConfirmPopup();
            HideTooltip();
        }

        #endregion
    }
}