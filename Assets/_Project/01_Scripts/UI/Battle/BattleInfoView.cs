using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OzGameLab01.UI
{
    [DisallowMultipleComponent]
    public sealed class BattleInfoView : MonoBehaviour
    {
        [Header("Enemy")]
        [SerializeField] private TMP_Text enemyNameText;
        [SerializeField] private Image enemyImage;

        [Header("Skill")]
        [SerializeField] private Transform skillContentRoot;

        [SerializeField]
        private BattleInfoSkillItemView skillItemPrefab;

        [Header("Stat")]
        [SerializeField] private Transform statContentRoot;
        [SerializeField] private BattleInfoStatItemView statItemPrefab;

        [Header("Skill Hover Info")]
        [SerializeField] private GameObject skillHoverInfoArea;
        [SerializeField] private Image skillDetailIcon;
        [SerializeField] private TMP_Text skillDetailTitleText;
        [SerializeField] private TMP_Text skillDetailDescriptionText;

        [Header("Stat Hover Info")]
        [SerializeField] private GameObject statHoverInfoArea;
        [SerializeField] private TMP_Text statDetailTitleText;
        [SerializeField] private TMP_Text statDetailDescriptionText;

        [Header("Battle")]
        [SerializeField] private Button battleButton;

        private readonly List<BattleInfoSkillItemView> skillItems = new();
        private readonly List<BattleInfoStatItemView> statItems = new();

        private BattleInfoSkillItemView hoveredSkillItem;
        private BattleInfoStatItemView hoveredStatItem;

        private Action battleCallback;


        #region Unity Lifecycle

        private void Awake()
        {
            HideSkillDetail();
            HideStatDetail();
        }

        private void OnEnable()
        {
            if (battleButton != null)
            {
                battleButton.onClick.AddListener(HandleBattleClicked);
            }

            SubscribeSkillItems();
            SubscribeStatItems();
        }

        private void OnDisable()
        {
            if (battleButton != null)
            {
                battleButton.onClick.RemoveListener(HandleBattleClicked);
            }

            UnsubscribeSkillItems();
            UnsubscribeStatItems();

            hoveredSkillItem = null;
            hoveredStatItem = null;

            HideSkillDetail();
            HideStatDetail();
        }

        #endregion


        #region Public API

        /// <summary>
        /// 전투 정보 화면에 표시할 적의 기본 정보를 설정합니다.
        /// </summary>
        /// <param name="enemyName">표시할 적 이름입니다.</param>
        /// <param name="image">표시할 적 이미지입니다.</param>
        public void SetEnemy(string enemyName, Sprite image)
        {
            if (enemyNameText != null)
            {
                enemyNameText.text = enemyName ?? string.Empty;
            }

            if (enemyImage != null)
            {
                enemyImage.sprite = image;
                enemyImage.enabled = image != null;
            }
        }

        /// <summary>
        /// 새로운 스킬 정보 아이템을 생성하여 표시합니다.
        /// </summary>
        /// <param name="icon">스킬 아이콘입니다.</param>
        /// <param name="title">Hover 상세 정보에 표시할 스킬 이름입니다.</param>
        /// <param name="description">
        /// Hover 상세 정보에 표시할 스킬 설명입니다.
        /// TMP Rich Text 문자열을 사용할 수 있습니다.
        /// </param>
        /// <returns>생성된 스킬 아이템 View입니다.</returns>
        public BattleInfoSkillItemView AddSkill(Sprite icon,string title,string description)
        {
            if (skillItemPrefab == null || skillContentRoot == null)
            {
                return null;
            }

            BattleInfoSkillItemView item = Instantiate(skillItemPrefab, skillContentRoot);

            item.Bind(icon, title, description);

            skillItems.Add(item);

            if (isActiveAndEnabled)
            {
                SubscribeSkillItem(item);
            }

            return item;
        }

        /// <summary>
        /// 새로운 스탯 정보 아이템을 생성하여 표시합니다.
        /// </summary>
        /// <param name="icon">스탯 아이콘입니다.</param>
        /// <param name="value">화면에 표시할 스탯 값입니다.</param>
        /// <param name="title">Hover 상세 정보에 표시할 스탯 이름입니다.</param>
        /// <param name="description">
        /// Hover 상세 정보에 표시할 스탯 설명입니다.
        /// TMP Rich Text 문자열을 사용할 수 있습니다.
        /// </param>
        /// <returns>생성된 스탯 아이템 View입니다.</returns>
        public BattleInfoStatItemView AddStat(Sprite icon,string value,string title,string description)
        {
            if (statItemPrefab == null || statContentRoot == null)
            {
                return null;
            }

            BattleInfoStatItemView item = Instantiate(statItemPrefab, statContentRoot);

            item.Bind(icon, value, title, description);

            statItems.Add(item);

            if (isActiveAndEnabled)
            {
                SubscribeStatItem(item);
            }

            return item;
        }

        /// <summary>
        /// 현재 생성된 모든 스킬 정보 아이템을 제거합니다.
        /// </summary>
        public void ClearSkills()
        {
            HideSkillDetail();

            for (int i = 0; i < skillItems.Count; i++)
            {
                BattleInfoSkillItemView item = skillItems[i];

                if (item == null)
                {
                    continue;
                }

                UnsubscribeSkillItem(item);
                Destroy(item.gameObject);
            }

            skillItems.Clear();
            hoveredSkillItem = null;
        }

        /// <summary>
        /// 현재 생성된 모든 스탯 정보 아이템을 제거합니다.
        /// </summary>
        public void ClearStats()
        {
            HideStatDetail();

            for (int i = 0; i < statItems.Count; i++)
            {
                BattleInfoStatItemView item = statItems[i];

                if (item == null)
                {
                    continue;
                }

                UnsubscribeStatItem(item);
                Destroy(item.gameObject);
            }

            statItems.Clear();
            hoveredStatItem = null;
        }

        /// <summary>
        /// Battle 버튼을 눌렀을 때 실행할 콜백을 설정합니다.
        /// 기존 콜백은 전달된 콜백으로 교체됩니다.
        /// </summary>
        /// <param name="callback">Battle 버튼 선택 시 실행할 콜백입니다.</param>
        public void SetBattleAction(Action callback)
        {
            battleCallback = callback;
        }

        /// <summary>
        /// Battle 버튼의 상호작용 가능 여부를 설정합니다.
        /// </summary>
        /// <param name="interactable">버튼 입력 가능 여부입니다.</param>
        public void SetBattleInteractable(bool interactable)
        {
            if (battleButton != null)
            {
                battleButton.interactable = interactable;
            }
        }

        /// <summary>
        /// BattleInfo View 전체의 활성 상태를 설정합니다.
        /// </summary>
        /// <param name="visible">표시 여부입니다.</param>
        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }

        /// <summary>
        /// 현재 표시 중인 적, 스킬, 스탯 및 Hover 정보를 모두 초기화합니다.
        /// </summary>
        public void Clear()
        {
            ClearSkills();
            ClearStats();

            if (enemyNameText != null)
            {
                enemyNameText.text = string.Empty;
            }

            if (enemyImage != null)
            {
                enemyImage.sprite = null;
                enemyImage.enabled = false;
            }

            battleCallback = null;

            HideSkillDetail();
            HideStatDetail();
        }

        #endregion


        #region Event Handlers

        private void HandleSkillHoverEntered(BattleInfoSkillItemView item)
        {
            if (item == null)
            {
                return;
            }

            hoveredSkillItem = item;

            HideStatDetail();
            ShowSkillDetail(item);
        }

        private void HandleSkillHoverExited(BattleInfoSkillItemView item)
        {
            if (hoveredSkillItem != item)
            {
                return;
            }

            hoveredSkillItem = null;
            HideSkillDetail();
        }

        private void HandleStatHoverEntered(BattleInfoStatItemView item)
        {
            if (item == null)
            {
                return;
            }

            hoveredStatItem = item;

            HideSkillDetail();
            ShowStatDetail(item);
        }

        private void HandleStatHoverExited(BattleInfoStatItemView item)
        {
            if (hoveredStatItem != item)
            {
                return;
            }

            hoveredStatItem = null;
            HideStatDetail();
        }

        private void HandleBattleClicked()
        {
            battleCallback?.Invoke();
        }

        #endregion


        #region Private Methods

        private void ShowSkillDetail(BattleInfoSkillItemView item)
        {
            if (skillDetailIcon != null)
            {
                skillDetailIcon.sprite = item.Icon;
                skillDetailIcon.enabled = item.Icon != null;
            }

            if (skillDetailTitleText != null)
            {
                skillDetailTitleText.text = item.DetailTitle;
            }

            if (skillDetailDescriptionText != null)
            {
                skillDetailDescriptionText.text = item.DetailDescription;
            }

            if (skillHoverInfoArea != null)
            {
                skillHoverInfoArea.SetActive(true);
            }
        }

        private void HideSkillDetail()
        {
            if (skillHoverInfoArea != null)
            {
                skillHoverInfoArea.SetActive(false);
            }
        }

        private void ShowStatDetail(BattleInfoStatItemView item)
        {
            if (statDetailTitleText != null)
            {
                statDetailTitleText.text = item.DetailTitle;
            }

            if (statDetailDescriptionText != null)
            {
                statDetailDescriptionText.text = item.DetailDescription;
            }

            if (statHoverInfoArea != null)
            {
                statHoverInfoArea.SetActive(true);
            }
        }

        private void HideStatDetail()
        {
            if (statHoverInfoArea != null)
            {
                statHoverInfoArea.SetActive(false);
            }
        }

        private void SubscribeSkillItems()
        {
            for (int i = 0; i < skillItems.Count; i++)
            {
                SubscribeSkillItem(skillItems[i]);
            }
        }

        private void UnsubscribeSkillItems()
        {
            for (int i = 0; i < skillItems.Count; i++)
            {
                UnsubscribeSkillItem(skillItems[i]);
            }
        }

        private void SubscribeSkillItem(BattleInfoSkillItemView item)
        {
            if (item == null)
            {
                return;
            }

            item.HoverEntered += HandleSkillHoverEntered;
            item.HoverExited += HandleSkillHoverExited;
        }

        private void UnsubscribeSkillItem(BattleInfoSkillItemView item)
        {
            if (item == null)
            {
                return;
            }

            item.HoverEntered -= HandleSkillHoverEntered;
            item.HoverExited -= HandleSkillHoverExited;
        }

        private void SubscribeStatItems()
        {
            for (int i = 0; i < statItems.Count; i++)
            {
                SubscribeStatItem(statItems[i]);
            }
        }

        private void UnsubscribeStatItems()
        {
            for (int i = 0; i < statItems.Count; i++)
            {
                UnsubscribeStatItem(statItems[i]);
            }
        }

        private void SubscribeStatItem(BattleInfoStatItemView item)
        {
            if (item == null)
            {
                return;
            }

            item.HoverEntered += HandleStatHoverEntered;
            item.HoverExited += HandleStatHoverExited;
        }

        private void UnsubscribeStatItem(BattleInfoStatItemView item)
        {
            if (item == null)
            {
                return;
            }

            item.HoverEntered -= HandleStatHoverEntered;
            item.HoverExited -= HandleStatHoverExited;
        }

        #endregion
    }
}