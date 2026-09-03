using System;
using UnityEngine;

namespace OzGameLab01.UI.Battle
{
    [DisallowMultipleComponent]
    public sealed class BattleUIView : MonoBehaviour
    {
        [Header("Views")]
        [SerializeField] private BattleMainView mainView;
        [SerializeField] private BattleRewardView rewardView;
        [SerializeField] private BattleResultView resultView;

        #region Properties

        public BattleMainView MainView => mainView;
        public BattleRewardView RewardView => rewardView;
        public BattleResultView ResultView => resultView;

        public bool IsVisible => gameObject.activeSelf;

        public event Action<RewardOptionItemView> RewardSelected;
        public event Action<BattleResultView> EndBattleClicked;

        #endregion

        #region Lifecycle

        private void OnEnable()
        {
            if (rewardView != null)
            {
                rewardView.RewardSelected += HandleRewardSelected;
            }

            if (resultView != null)
            {
                resultView.EndBattleClicked += HandleEndBattleClicked;
            }
        }

        private void OnDisable()
        {
            if (rewardView != null)
            {
                rewardView.RewardSelected -= HandleRewardSelected;
            }

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

        public void ShowRewardView()
        {
            if (resultView != null)
            {
                resultView.Hide();
            }

            if (rewardView != null)
            {
                rewardView.Show();
            }
        }

        public void ShowResultView()
        {
            if (rewardView != null)
            {
                rewardView.Hide();
            }

            if (resultView != null)
            {
                resultView.Show();
            }
        }

        public void HideAllOverlayViews()
        {
            if (rewardView != null)
            {
                rewardView.Hide();
            }

            if (resultView != null)
            {
                resultView.Hide();
            }
        }

        #endregion

        #region Private Methods

        private void HandleRewardSelected(RewardOptionItemView option)
        {
            RewardSelected?.Invoke(option);
        }

        private void HandleEndBattleClicked(BattleResultView view)
        {
            EndBattleClicked?.Invoke(view);
        }

        #endregion
    }
}