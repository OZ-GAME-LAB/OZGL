using System;
using UnityEngine;
using ReadySettingsView = OzGameLab01.UI.Settings.SettingsView;

namespace OzGameLab01.UI
{
    public enum ReadySceneViewType
    {
        Main,
        Roll,
        Unit,
        Tooltip,
        Settings,
        ConfirmPopup,
        Feedback,
        UnitAcquirePopup
    }

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
        [SerializeField] private UnitAcquirePopupView _unitAcquirePopupView;

        #region Properties

        public ReadyMainView MainView => _mainView;
        public DiceRollView RollView => _rollView;
        public UnitView UnitView => _unitView;
        public TooltipView TooltipView => _tooltipView;
        public ReadySettingsView SettingsView => _settingsView;
        public ConfirmPopupView ConfirmPopupView => _confirmPopupView;
        public FeedbackView FeedbackView => _feedbackView;
        public UnitAcquirePopupView UnitAcquirePopupView => _unitAcquirePopupView;

        /// <summary>
        /// ReadySceneView를 통해 UI가 표시되거나 해제됐을 때 호출됩니다.
        /// bool 값이 true면 표시, false면 해제입니다.
        /// </summary>
        public event Action<ReadySceneViewType,bool> ViewVisibilityChanged;

        #endregion

        #region API

        public void ShowMainView()
        {
            if (_mainView != null)
            {
                bool wasVisible = _mainView.gameObject.activeSelf;
                _mainView.Show();
                NotifyVisibilityChanged(_mainView,ReadySceneViewType.Main,wasVisible);
            }
        }

        public void HideMainView()
        {
            if (_mainView != null)
            {
                bool wasVisible = _mainView.gameObject.activeSelf;
                _mainView.Hide();
                NotifyVisibilityChanged(_mainView,ReadySceneViewType.Main,wasVisible);
            }
        }

        public void ShowRollView()
        {
            if (_rollView != null)
            {
                bool wasVisible = _rollView.gameObject.activeSelf;
                _rollView.Show();
                NotifyVisibilityChanged(_rollView,ReadySceneViewType.Roll,wasVisible);
            }
        }

        public void HideRollView()
        {
            if (_rollView != null)
            {
                bool wasVisible = _rollView.gameObject.activeSelf;
                _rollView.Hide();
                NotifyVisibilityChanged(_rollView,ReadySceneViewType.Roll,wasVisible);
            }
        }

        public void ShowUnitView()
        {
            if (_unitView != null)
            {
                bool wasVisible = _unitView.gameObject.activeSelf;
                _unitView.Show();
                NotifyVisibilityChanged(_unitView,ReadySceneViewType.Unit,wasVisible);
            }
        }

        public void HideUnitView()
        {
            if (_unitView != null)
            {
                bool wasVisible = _unitView.gameObject.activeSelf;
                _unitView.Hide();
                NotifyVisibilityChanged(_unitView,ReadySceneViewType.Unit,wasVisible);
            }
        }

        public void ShowSettingsView()
        {
            if (_settingsView != null)
            {
                bool wasVisible = _settingsView.gameObject.activeSelf;
                _settingsView.Show();
                NotifyVisibilityChanged(_settingsView,ReadySceneViewType.Settings,wasVisible);
            }
        }

        public void HideSettingsView()
        {
            if (_settingsView != null)
            {
                bool wasVisible = _settingsView.gameObject.activeSelf;
                _settingsView.Hide();
                NotifyVisibilityChanged(_settingsView,ReadySceneViewType.Settings,wasVisible);
            }
        }

        public void ShowConfirmPopup()
        {
            if (_confirmPopupView != null)
            {
                bool wasVisible = _confirmPopupView.gameObject.activeSelf;
                _confirmPopupView.Show();
                NotifyVisibilityChanged(
                    _confirmPopupView,
                    ReadySceneViewType.ConfirmPopup,
                    wasVisible);
            }
        }

        public void HideConfirmPopup()
        {
            if (_confirmPopupView != null)
            {
                bool wasVisible = _confirmPopupView.gameObject.activeSelf;
                _confirmPopupView.Hide();
                NotifyVisibilityChanged(
                    _confirmPopupView,
                    ReadySceneViewType.ConfirmPopup,
                    wasVisible);
            }
        }

        public void ShowFeedbackView(string message)
        {
            if (_feedbackView != null)
            {
                bool wasVisible = _feedbackView.gameObject.activeSelf;
                _feedbackView.Show(message);
                NotifyVisibilityChanged(
                    _feedbackView,
                    ReadySceneViewType.Feedback,
                    wasVisible);
            }
        }

        public void HideFeedbackView()
        {
            if (_feedbackView != null)
            {
                bool wasVisible = _feedbackView.gameObject.activeSelf;
                _feedbackView.Hide();
                NotifyVisibilityChanged(
                    _feedbackView,
                    ReadySceneViewType.Feedback,
                    wasVisible);
            }
        }

        public void HideTooltip()
        {
            if (_tooltipView != null)
            {
                bool wasVisible = _tooltipView.gameObject.activeSelf;
                _tooltipView.Hide();
                NotifyVisibilityChanged(
                    _tooltipView,
                    ReadySceneViewType.Tooltip,
                    wasVisible);
            }
        }

        public void PlayUnitAcquirePopup(string unitName, Sprite sprite)
        {
            if (_unitAcquirePopupView != null)
            {
                bool wasVisible = _unitAcquirePopupView.gameObject.activeSelf;
                _unitAcquirePopupView.SetData(unitName, sprite);
                _unitAcquirePopupView.Play(() => {
                    NotifyVisibilityChanged(_unitAcquirePopupView, ReadySceneViewType.UnitAcquirePopup, true);
                });
                
                NotifyVisibilityChanged(_unitAcquirePopupView, ReadySceneViewType.UnitAcquirePopup, wasVisible);
            }
        }

        public void HideUnitAcquirePopup()
        {
            if (_unitAcquirePopupView != null)
            {
                bool wasVisible = _unitAcquirePopupView.gameObject.activeSelf;
                _unitAcquirePopupView.Hide();
                NotifyVisibilityChanged(_unitAcquirePopupView, ReadySceneViewType.UnitAcquirePopup, wasVisible);
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
            HideUnitAcquirePopup();
        }

        private void NotifyVisibilityChanged(
            MonoBehaviour view,
            ReadySceneViewType viewType,
            bool wasVisible)
        {
            bool isVisible = view.gameObject.activeSelf;

            if (wasVisible == isVisible)
                return;

            ViewVisibilityChanged?.Invoke(viewType,isVisible);
        }

        #endregion
    }
}
