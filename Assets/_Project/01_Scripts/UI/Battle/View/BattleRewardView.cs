using System;
using System.Collections.Generic;
using UnityEngine;

namespace OzGameLab01.UI.Battle
{
    [DisallowMultipleComponent]
    public sealed class BattleRewardView : MonoBehaviour
    {
        private const int MaxOptionCount = 3;

        [Header("References")]
        [SerializeField] private Transform rewardChoicesRoot;
        [SerializeField] private RewardOptionItemView rewardOptionItemPrefab;

        private readonly List<RewardOptionItemView> rewardOptions = new ();

        private bool isListening;

        #region Properties

        public Transform RewardChoicesRoot => rewardChoicesRoot;

        public IReadOnlyList<RewardOptionItemView> RewardOptions => rewardOptions;

        public bool IsVisible => gameObject.activeSelf;

        public event Action<RewardOptionItemView> RewardSelected;

        #endregion

        #region Lifecycle

        private void OnEnable()
        {
            isListening = true;
            SubscribeRewardOptions();
        }

        private void OnDisable()
        {
            UnsubscribeRewardOptions();
            isListening = false;
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

        public RewardOptionItemView CreateRewardOption()
        {
            if (rewardOptionItemPrefab == null || rewardChoicesRoot == null)
            {
                return null;
            }

            if (rewardOptions.Count >= MaxOptionCount)
            {
                Debug.LogWarning(
                    $"{nameof(BattleRewardView)} can display up to {MaxOptionCount} reward options.",
                    this);

                return null;
            }

            RewardOptionItemView option = Instantiate(rewardOptionItemPrefab,rewardChoicesRoot);

            RegisterRewardOption(option);

            return option;
        }

        public void ClearRewardOptions()
        {
            foreach (RewardOptionItemView option in rewardOptions)
            {
                if (option == null)
                {
                    continue;
                }

                option.Selected -= HandleRewardOptionSelected;
                Destroy(option.gameObject);
            }

            rewardOptions.Clear();
        }

        public void RegisterRewardOption(RewardOptionItemView option)
        {
            if (option == null || rewardOptions.Contains(option))
            {
                return;
            }

            rewardOptions.Add(option);

            if (isListening)
            {
                option.Selected += HandleRewardOptionSelected;
            }
        }

        public void UnregisterRewardOption(RewardOptionItemView option)
        {
            if (option == null || !rewardOptions.Remove(option))
            {
                return;
            }

            option.Selected -= HandleRewardOptionSelected;
        }

        #endregion

        #region Private Methods

        private void SubscribeRewardOptions()
        {
            foreach (RewardOptionItemView option in rewardOptions)
            {
                if (option != null)
                {
                    option.Selected += HandleRewardOptionSelected;
                }
            }
        }

        private void UnsubscribeRewardOptions()
        {
            foreach (RewardOptionItemView option in rewardOptions)
            {
                if (option != null)
                {
                    option.Selected -= HandleRewardOptionSelected;
                }
            }
        }

        private void HandleRewardOptionSelected(RewardOptionItemView option)
        {
            RewardSelected?.Invoke(option);
        }

        #endregion
    }
}