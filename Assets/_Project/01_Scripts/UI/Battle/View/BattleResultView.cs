using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OzGameLab01.UI.Battle
{
    [DisallowMultipleComponent]
    public sealed class BattleResultView : MonoBehaviour
    {
        private const int MaxDpsInfoCount = 4;

        [Header("References")]
        [SerializeField] private TMP_Text resultText;
        [SerializeField] private TMP_Text optionalMessageText;
        [SerializeField] private Image rewardIconImage;
        [SerializeField] private Transform dpsListRoot;
        [SerializeField] private DpsInfoItemView dpsInfoItemPrefab;
        [SerializeField] private Button endBattleButton;

        private readonly List<DpsInfoItemView> dpsInfoItems =
            new List<DpsInfoItemView>();

        #region Properties

        public TMP_Text ResultText => resultText;
        public TMP_Text OptionalMessageText => optionalMessageText;
        public Image RewardIconImage => rewardIconImage;
        public Transform DpsListRoot => dpsListRoot;
        public Button EndBattleButton => endBattleButton;

        public IReadOnlyList<DpsInfoItemView> DpsInfoItems => dpsInfoItems;

        public bool IsVisible => gameObject.activeSelf;

        public event Action<BattleResultView> EndBattleClicked;

        #endregion

        #region Lifecycle

        private void OnEnable()
        {
            if (endBattleButton != null)
            {
                endBattleButton.onClick.AddListener(HandleEndBattleButtonClick);
            }
        }

        private void OnDisable()
        {
            if (endBattleButton != null)
            {
                endBattleButton.onClick.RemoveListener(HandleEndBattleButtonClick);
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

        public void SetResultText(string value)
        {
            if (resultText != null)
            {
                resultText.text = value ?? string.Empty;
            }
        }

        public void SetOptionalMessage(string value)
        {
            if (optionalMessageText == null)
            {
                return;
            }

            bool hasMessage = !string.IsNullOrEmpty(value);

            optionalMessageText.text = value ?? string.Empty;
            optionalMessageText.gameObject.SetActive(hasMessage);
        }

        public void SetRewardIcon(Sprite icon)
        {
            if (rewardIconImage == null)
            {
                return;
            }

            rewardIconImage.sprite = icon;
            rewardIconImage.enabled = icon != null;
        }

        public void SetEndBattleButtonInteractable(bool value)
        {
            if (endBattleButton != null)
            {
                endBattleButton.interactable = value;
            }
        }

        public DpsInfoItemView CreateDpsInfoItem()
        {
            if (dpsInfoItemPrefab == null || dpsListRoot == null)
            {
                return null;
            }

            if (dpsInfoItems.Count >= MaxDpsInfoCount)
            {
                Debug.LogWarning($"{nameof(BattleResultView)} can display up to {MaxDpsInfoCount} DPS entries.", this);

                return null;
            }

            DpsInfoItemView item = Instantiate(dpsInfoItemPrefab,dpsListRoot);

            dpsInfoItems.Add(item);

            return item;
        }

        public void ClearDpsInfoItems()
        {
            foreach (DpsInfoItemView item in dpsInfoItems)
            {
                if (item != null)
                {
                    Destroy(item.gameObject);
                }
            }

            dpsInfoItems.Clear();
        }

        #endregion

        #region Private Methods

        private void HandleEndBattleButtonClick()
        {
            EndBattleClicked?.Invoke(this);
        }

        #endregion
    }
}