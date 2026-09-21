using System;
using UnityEngine;

namespace OzGameLab01.UI.Battle
{
    /// <summary>
    /// 전투 UI View입니다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CombatUIView : MonoBehaviour
    {
        [Header("Views")]
        [SerializeField] private CombatMainView mainView;
        [SerializeField] private CombatResultView resultView;

        #region Properties

        public CombatMainView MainView => mainView;
        public CombatResultView ResultView => resultView;

        public bool IsVisible => gameObject.activeSelf;

        public event Action<CombatResultView> EndBattleClicked;

        #endregion

        #region Lifecycle

        private void OnEnable()
        {
            if (resultView != null)
            {
                resultView.EndBattleClicked += HandleEndBattleClicked;
            }
        }

        private void OnDisable()
        {
            if (resultView != null)
            {
                resultView.EndBattleClicked -= HandleEndBattleClicked;
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

        public void ShowResultView()
        {
            if (resultView != null)
            {
                resultView.Show();
            }
        }

        public void HideAllOverlayViews()
        {
            if (resultView != null)
            {
                resultView.Hide();
            }
        }

        #endregion

        #region Private Methods

        private void HandleEndBattleClicked(CombatResultView view)
        {
            EndBattleClicked?.Invoke(view);
        }

        #endregion
    }
}