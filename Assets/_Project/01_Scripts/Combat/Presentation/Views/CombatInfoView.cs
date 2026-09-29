using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OzGameLab01.UI
{
    [DisallowMultipleComponent]
    public sealed class CombatInfoView : MonoBehaviour
    {
        [Header("Visibility")]
        [Tooltip("전투 정보 UI 전체를 켜고 끌 루트입니다. 비워두면 이 GameObject만 제어합니다.")]
        [SerializeField] private GameObject visibilityRoot;

        [Header("Enemy")]
        [SerializeField] private TMP_Text enemyNameText;
        [SerializeField] private Image enemyImage;

        [Header("Skill")]
        [SerializeField] private Transform skillContentRoot;

        [SerializeField]
        private CombatInfoSkillItemView skillItemPrefab;

        [Header("Stat")]
        [SerializeField] private Transform statContentRoot;
        [SerializeField] private CombatInfoStatItemView statItemPrefab;

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

        private readonly List<CombatInfoSkillItemView> skillItems = new();
        private readonly List<CombatInfoStatItemView> statItems = new();
        private Sprite[] defaultStatIcons;

        private CombatInfoSkillItemView hoveredSkillItem;
        private CombatInfoStatItemView hoveredStatItem;
        private int _visibleSkillItemCount;
        private int _visibleStatItemCount;

        private Action battleCallback;

        public RectTransform BattleButtonHighlightTarget =>
            battleButton != null
                ? battleButton.transform as RectTransform
                : null;

        public bool IsBattleButtonReady =>
            battleButton != null &&
            battleButton.gameObject.activeInHierarchy &&
            battleButton.interactable;

        public event Action BattleButtonReady;
        public event Action BattleButtonClicked;


        #region Unity Lifecycle

        private void Awake()
        {
            CacheConfiguredItems();
            CacheDefaultStatIcons();
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
        public CombatInfoSkillItemView AddSkill(Sprite icon,string title,string description)
        {
            if (skillItemPrefab == null || skillContentRoot == null)
            {
                return null;
            }

            CombatInfoSkillItemView item;
            if (_visibleSkillItemCount < skillItems.Count)
            {
                item = skillItems[_visibleSkillItemCount];
                item.gameObject.SetActive(true);
            }
            else
            {
                item = Instantiate(skillItemPrefab, skillContentRoot);
                skillItems.Add(item);
            }

            item.Bind(icon, title, description);
            _visibleSkillItemCount++;

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
        public CombatInfoStatItemView AddStat(Sprite icon,string value,string title,string description)
        {
            if (statItemPrefab == null || statContentRoot == null)
            {
                return null;
            }

            CombatInfoStatItemView item;
            if (_visibleStatItemCount < statItems.Count)
            {
                item = statItems[_visibleStatItemCount];
                item.gameObject.SetActive(true);
            }
            else
            {
                item = Instantiate(statItemPrefab, statContentRoot);
                statItems.Add(item);
            }

            item.Bind(icon, value, title, description);
            _visibleStatItemCount++;

            if (isActiveAndEnabled)
            {
                SubscribeStatItem(item);
            }

            return item;
        }

        /// <summary>
        /// 적 데이터의 전투 스탯을 기존 아이콘과 함께 순서대로 표시합니다.
        /// </summary>
        /// <param name="data">표시할 적 데이터입니다.</param>
        public void BindStats(OzGameLab01.Data.MonsterData data)
        {
            if (data == null)
            {
                return;
            }

            CacheDefaultStatIcons();
            BindStat(0, data.healthPoint, false, "체력", "적의 최대 체력");
            BindStat(1, data.attackPoint, false, "공격력", "적의 기본 공격력");
            BindStat(2, data.defensePoint, false, "방어력", "적의 피해 감소 수치");
            BindStat(3, data.attackSpeed, false, "공격속도", "적의 기본 공격 간격");
            BindStat(4, data.criticalRate, true, "치명타 확률", "적의 치명타 발생 확률");
            BindStat(5, data.criticalMult, true, "치명타 피해", "적의 치명타 피해 배율");
            BindStat(6, data.dodgeRate, true, "회피율", "적의 공격 회피 확률");
        }

        /// <summary>
        /// 현재 생성된 모든 스킬 정보 아이템을 제거합니다.
        /// </summary>
        public void ClearSkills()
        {
            HideSkillDetail();

            for (int i = 0; i < skillItems.Count; i++)
            {
                CombatInfoSkillItemView item = skillItems[i];

                if (item == null)
                {
                    continue;
                }

                UnsubscribeSkillItem(item);
                item.Clear();
                item.gameObject.SetActive(false);
            }

            _visibleSkillItemCount = 0;
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
                CombatInfoStatItemView item = statItems[i];

                if (item == null)
                {
                    continue;
                }

                UnsubscribeStatItem(item);
                item.gameObject.SetActive(false);
            }

            _visibleStatItemCount = 0;
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
                bool wasReady = IsBattleButtonReady;
                battleButton.interactable = interactable;

                if (!wasReady && IsBattleButtonReady)
                {
                    BattleButtonReady?.Invoke();
                }
            }
        }

        /// <summary>
        /// 튜토리얼 오버레이처럼 실제 버튼 위에서 입력을 대신 받는 UI가
        /// Battle 버튼과 동일한 동작을 요청할 때 사용합니다.
        /// </summary>
        public bool TryInvokeBattleButton()
        {
            if (!IsBattleButtonReady)
            {
                return false;
            }

            battleButton.onClick.Invoke();
            return true;
        }

        /// <summary>
        /// BattleInfo View 전체의 활성 상태를 설정합니다.
        /// </summary>
        /// <param name="visible">표시 여부입니다.</param>
        public void SetVisible(bool visible)
        {
            GameObject target = visibilityRoot != null
                ? visibilityRoot
                : gameObject;

            if (target.activeSelf != visible)
            {
                target.SetActive(visible);
            }
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

        private void HandleSkillHoverEntered(CombatInfoSkillItemView item)
        {
            if (item == null)
            {
                return;
            }

            hoveredSkillItem = item;

            HideStatDetail();
            ShowSkillDetail(item);
        }

        private void HandleSkillHoverExited(CombatInfoSkillItemView item)
        {
            if (hoveredSkillItem != item)
            {
                return;
            }

            hoveredSkillItem = null;
            HideSkillDetail();
        }

        private void HandleStatHoverEntered(CombatInfoStatItemView item)
        {
            if (item == null)
            {
                return;
            }

            hoveredStatItem = item;

            HideSkillDetail();
            ShowStatDetail(item);
        }

        private void HandleStatHoverExited(CombatInfoStatItemView item)
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
            BattleButtonClicked?.Invoke();
        }

        #endregion


        #region Private Methods

        private void CacheConfiguredItems()
        {
            if (skillContentRoot != null)
            {
                CombatInfoSkillItemView[] configuredSkillItems =
                    skillContentRoot.GetComponentsInChildren<CombatInfoSkillItemView>(true);
                skillItems.AddRange(configuredSkillItems);
            }

            if (statContentRoot != null)
            {
                CombatInfoStatItemView[] configuredStatItems =
                    statContentRoot.GetComponentsInChildren<CombatInfoStatItemView>(true);
                statItems.AddRange(configuredStatItems);
            }
        }

        /// <summary>
        /// 프리팹에 설정된 스탯 아이콘을 최초 한 번 보관합니다.
        /// </summary>
        private void CacheDefaultStatIcons()
        {
            if (defaultStatIcons != null)
            {
                return;
            }

            int count = statItems.Count;
            defaultStatIcons = new Sprite[count];

            for (int index = 0; index < count; index++)
            {
                defaultStatIcons[index] = statItems[index] != null
                    ? statItems[index].Icon
                    : null;
            }
        }

        /// <summary>
        /// 지정한 스탯 슬롯에 기본 아이콘과 형식화한 값을 표시합니다.
        /// </summary>
        private void BindStat(int index,float value,bool percent,string title,string description)
        {
            Sprite icon = defaultStatIcons != null && index >= 0 && index < defaultStatIcons.Length
                ? defaultStatIcons[index]
                : null;
            string formattedValue = value.ToString("0.##") + (percent ? "%" : string.Empty);
            AddStat(icon, formattedValue, title, description);
        }

        private void ShowSkillDetail(CombatInfoSkillItemView item)
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

        private void ShowStatDetail(CombatInfoStatItemView item)
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

        private void SubscribeSkillItem(CombatInfoSkillItemView item)
        {
            if (item == null)
            {
                return;
            }

            item.HoverEntered += HandleSkillHoverEntered;
            item.HoverExited += HandleSkillHoverExited;
        }

        private void UnsubscribeSkillItem(CombatInfoSkillItemView item)
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

        private void SubscribeStatItem(CombatInfoStatItemView item)
        {
            if (item == null)
            {
                return;
            }

            item.HoverEntered += HandleStatHoverEntered;
            item.HoverExited += HandleStatHoverExited;
        }

        private void UnsubscribeStatItem(CombatInfoStatItemView item)
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
