using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using OzGameLab01.Managers;
using OzGameLab01.Data;

namespace OzGameLab01.UI.Battle
{
    [DisallowMultipleComponent]
    public sealed class CombatResultView : MonoBehaviour, IRelicDisplayable
    {
        [Header("References")]
        [SerializeField] private TMP_Text resultText;
        [SerializeField] private TMP_Text optionalMessageText;
        [SerializeField] private Image rewardIconImage;
        [SerializeField] private Transform dpsListRoot;
        [SerializeField] private DpsInfoItemView dpsInfoItemPrefab;
        [SerializeField] private Button endBattleButton;

        [Header("Transition")]
        [SerializeField] private OverlayTransitionView overlayTransition;

        private readonly List<DpsInfoItemView> dpsInfoItems = new ();
        private string _currentRewardIconAddress;
        private TooltipView _tooltipView;
        private RelicData _rewardRelic;
        private RelicTooltipTrigger _rewardTooltipTrigger;

        #region Properties

        public TMP_Text ResultText => resultText;
        public TMP_Text OptionalMessageText => optionalMessageText;
        public Image RewardIconImage => rewardIconImage;
        public Transform DpsListRoot => dpsListRoot;
        public Button EndBattleButton => endBattleButton;

        public IReadOnlyList<DpsInfoItemView> DpsInfoItems => dpsInfoItems;

        public bool IsVisible => gameObject.activeSelf;

        public event Action<CombatResultView> EndBattleClicked;

        public async Task UpdateRelicIconAsync(string iconAddress) => await SetRewardIconAsync(iconAddress);

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
            RefreshDpsListVisibility();

            if (overlayTransition != null)
            {
                overlayTransition.Show();
                return;
            }

            gameObject.SetActive(true);
        }

        public void Hide()
        {
            if (overlayTransition != null)
            {
                overlayTransition.Hide();
                return;
            }

            gameObject.SetActive(false);
        }

        public void HideImmediate()
        {
            if (overlayTransition != null)
            {
                overlayTransition.HideImmediate();
                return;
            }

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

        public void BindTooltipView(TooltipView tooltipView)
        {
            _tooltipView = tooltipView;
            RefreshRewardTooltipBinding();
        }

        public void SetRewardRelic(RelicData relicData)
        {
            _rewardRelic = relicData;
            RefreshRewardTooltipBinding();
            _ = SetRewardIconAsync(relicData?.iconAddress);
        }

        /// <summary>
        /// 어드레서블 주소를 기반 전투 결과 보상 아이콘 설정
        /// </summary>
        public async Task SetRewardIconAsync(string iconAddress)
        {
            if (rewardIconImage == null) return;

            _currentRewardIconAddress = iconAddress;
            if (string.IsNullOrEmpty(iconAddress))
            {
                SetRewardIcon(null);
                return;
            }

            Sprite sprite = await SpriteManager.GetSpriteAsync(iconAddress);

            if (_currentRewardIconAddress == iconAddress && rewardIconImage != null)
            {
                rewardIconImage.sprite = sprite;
                rewardIconImage.enabled = sprite != null;
            }
        }

        public void SetEndBattleButtonInteractable(bool value)
        {
            if (endBattleButton != null)
            {
                endBattleButton.interactable = value;
            }
        }

        public void SetEndBattleButtonText(string value)
        {
            if (endBattleButton == null)
            {
                return;
            }

            TMP_Text buttonText = endBattleButton.GetComponentInChildren<TMP_Text>(true);
            if (buttonText != null)
            {
                buttonText.text = value ?? string.Empty;
            }
        }

        public DpsInfoItemView CreateDpsInfoItem()
        {
            if (dpsInfoItemPrefab == null)
            {
                Debug.LogError("[CombatResultView] DpsInfoItemPrefab 참조가 없습니다.", this);
                return null;
            }

            if (dpsListRoot == null)
            {
                Debug.LogError("[CombatResultView] DpsListRoot 참조가 없습니다.", this);
                return null;
            }

            DpsInfoItemView item = Instantiate(dpsInfoItemPrefab, dpsListRoot);

            dpsInfoItems.Add(item);
            RefreshDpsListVisibility();

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
            RefreshDpsListVisibility();
        }

        #endregion

        #region Private Methods

        private void HandleEndBattleButtonClick()
        {
            EndBattleClicked?.Invoke(this);
        }

        private void RefreshRewardTooltipBinding()
        {
            if (rewardIconImage == null)
            {
                return;
            }

            if (_rewardTooltipTrigger == null)
            {
                _rewardTooltipTrigger = rewardIconImage.GetComponent<RelicTooltipTrigger>();
                if (_rewardTooltipTrigger == null)
                {
                    _rewardTooltipTrigger = rewardIconImage.gameObject.AddComponent<RelicTooltipTrigger>();
                }
            }

            _rewardTooltipTrigger.Bind(
                _tooltipView,
                _rewardRelic,
                rewardIconImage.rectTransform);
        }

        private void RefreshDpsListVisibility()
        {
            if (dpsListRoot != null)
            {
                dpsListRoot.gameObject.SetActive(dpsInfoItems.Count > 0);
            }
        }

        #endregion
    }
}
