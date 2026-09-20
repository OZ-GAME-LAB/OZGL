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
        [SerializeField] private BattleRewardView rewardView;
        [SerializeField] private CombatResultView resultView;

        #region Properties

        public CombatMainView MainView => mainView;
        public BattleRewardView RewardView => rewardView;
        public CombatResultView ResultView => resultView;

        public bool IsVisible => gameObject.activeSelf;

        public event Action<RewardOptionItemView> RewardSelected;
        public event Action<CombatResultView> EndBattleClicked;

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
                rewardView.ShowConfiguredRewards();
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

        private void HandleEndBattleClicked(CombatResultView view)
        {
            EndBattleClicked?.Invoke(view);
        }

        #endregion
    }
}
