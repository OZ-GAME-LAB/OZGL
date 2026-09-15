using UnityEngine;
using ReadySettingsView = OzGameLab01.UI.Settings.SettingsView;

namespace OzGameLab01.UI
{
    [DisallowMultipleComponent]
    public sealed class ReadySceneView : MonoBehaviour
    {
        [Header("Views")]
        [UnityEngine.Serialization.FormerlySerializedAs("mainView")]
        [SerializeField] private ReadyMainView _mainView;
        [UnityEngine.Serialization.FormerlySerializedAs("rollView")]
        [SerializeField] private DiceRollView _rollView;
        [UnityEngine.Serialization.FormerlySerializedAs("unitView")]
        [SerializeField] private UnitView _unitView;
        [UnityEngine.Serialization.FormerlySerializedAs("tooltipView")]
        [SerializeField] private TooltipView _tooltipView;
        [UnityEngine.Serialization.FormerlySerializedAs("settingsView")]
        [SerializeField] private ReadySettingsView _settingsView;
        [UnityEngine.Serialization.FormerlySerializedAs("confirmPopupView")]
        [SerializeField] private ConfirmPopupView _confirmPopupView;
        [UnityEngine.Serialization.FormerlySerializedAs("feedbackView")]
        [SerializeField] private FeedbackView _feedbackView;

        #region Properties

        public ReadyMainView MainView => _mainView;
        public DiceRollView RollView => _rollView;
        public UnitView UnitView => _unitView;
        public TooltipView TooltipView => _tooltipView;
        public ReadySettingsView SettingsView => _settingsView;
        public ConfirmPopupView ConfirmPopupView => _confirmPopupView;
        public FeedbackView FeedbackView => _feedbackView;

        #endregion

        #region API

        public void ShowMainView()
        {
            if (_mainView != null)
            {
                _mainView.Show();
            }
        }

        public void HideMainView()
        {
            if (_mainView != null)
            {
                _mainView.Hide();
            }
        }

        public void ShowRollView()
        {
            if (_rollView != null)
            {
                _rollView.Show();
            }
        }

        public void HideRollView()
        {
            if (_rollView != null)
            {
                _rollView.Hide();
            }
        }

        public void ShowUnitView()
        {
            if (_unitView != null)
            {
                _unitView.Show();
            }
        }

        public void HideUnitView()
        {
            if (_unitView != null)
            {
                _unitView.Hide();
            }
        }

        public void ShowSettingsView()
        {
            if (_settingsView != null)
            {
                _settingsView.Show();
            }
        }

        public void HideSettingsView()
        {
            if (_settingsView != null)
            {
                _settingsView.Hide();
            }
        }

        public void ShowConfirmPopup()
        {
            if (_confirmPopupView != null)
            {
                _confirmPopupView.Show();
            }
        }

        public void HideConfirmPopup()
        {
            if (_confirmPopupView != null)
            {
                _confirmPopupView.Hide();
            }
        }

        public void ShowFeedbackView(string message)
        {
            if (_feedbackView != null)
            {
                _feedbackView.Show(message);
            }
        }

        public void HideFeedbackView()
        {
            if (_feedbackView != null)
            {
                _feedbackView.Hide();
            }
        }

        public void HideTooltip()
        {
            if (_tooltipView != null)
            {
                _tooltipView.Hide();
            }
        }

        public void HideAllOverlayViews()
        {
            HideRollView();
            HideUnitView();
            HideSettingsView();
            HideConfirmPopup();
            HideFeedbackView();
            HideTooltip();
        }

        #endregion
    }
}
