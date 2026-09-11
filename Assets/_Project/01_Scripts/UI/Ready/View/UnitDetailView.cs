using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OzGameLab01.UI
{
    /// <summary>
    /// 유닛 데이터를 표시하고 호버 팝업의 입력 통과를 관리합니다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class UnitDetailView : MonoBehaviour
    {
        [Header("Selected Unit")]
        [SerializeField] private Image unitIcon;
        [SerializeField] private TMP_Text unitNameText;
        [SerializeField] private Transform synergyBadgeRoot;
        [SerializeField] private InfoSynergyItemView synergyBadgeItemPrefab;

        [Header("Skills")]
        [SerializeField] private Image[] skillIcons;
        [SerializeField] private TMP_Text[] skillDescriptionTexts;

        [Header("Description")]
        [SerializeField] private TMP_Text conceptDescriptionText;

        private readonly List<InfoSynergyItemView> synergyBadgeItems = new ();

        public bool IsVisible => gameObject.activeSelf;

        // 현재 데이터 로드와 추후 상세 정보 확장 진입점 추가
        /// <summary>
        /// 기존 UI 틀을 유지하고 연결된 유닛 데이터를 표시합니다.
        /// 스킬 데이터가 준비되면 이 함수에서 data.passiveSkillKey와 data.activeSkillKey로
        /// 해당 스킬의 아이콘과 설명을 조회한 뒤 SetSkill(슬롯 번호, 아이콘, 설명)을 호출합니다.
        /// 슬롯 번호 0과 1은 Inspector의 Skill Icons 및 Skill Description Texts 배열 순서입니다.
        /// 패시브와 액티브의 표시 순서는 기획 확정 후 해당 슬롯에 맞춰 연결합니다.
        /// 유닛 상세 설명은 data.id로 설명 데이터를 조회한 뒤 SetConceptDescription(설명)으로 전달합니다.
        /// 현재 스킬 및 상세 설명의 데이터 조회 기능은 구현되어 있지 않습니다.
        /// 연결 전에는 호출하지 않아 기존 스킬 칸과 설명의 기본 표시를 유지합니다.
        /// SetSkill에 null 아이콘을 전달하면 이미지가 숨겨지므로, 데이터 누락 시에는
        /// 이전 유닛의 값 대신 해당 슬롯의 기본 아이콘과 기본 설명을 전달하도록 연결합니다.
        /// </summary>
        public void LoadUnit(OzGameLab01.Data.UnitData data, Sprite icon)
        {
//            ClearDetail();
            // 미연결 스킬 칸과 설명의 프리팹 기본 표시 유지
            if (data == null)
            {
                return;
            }
            SetUnitIcon(icon);
            if (unitIcon != null)
            {
                unitIcon.color = data.color;
            }
            SetUnitName(data.name);
//            SetSynergies(new[] { data.jobType.ToString(), data.tribeType.ToString() });
            // 고정 UI 보존을 위한 자동 시너지 이름표 생성 제외, SetSynergies 연결 함수 유지
            // 활성 상태에서 유닛 변경으로 생성된 시너지 이름표도 입력 통과 처리
            foreach (Graphic graphic in GetComponentsInChildren<Graphic>(true))
                graphic.raycastTarget = false;
//            SetConceptDescription($" healthPoint  {data.healthPoint:0.##}\n " +
//                $"attackPoint  {data.attackPoint:0.##}\n defensePoint  {data.defensePoint:0.##}\n " +
//                $"attackSpeed  {data.attackSpeed:0.##}\n criticalMult   {data.criticalMult:0.##}\n " +
//                $"criticalRate  {data.criticalRate:0.##}\n dodgeRate  {data.dodgeRate:0.##}");
            // 설명 영역의 임시 능력치 출력 제외, SetConceptDescription 연결 함수 유지
        }

        private void OnEnable()
        {
            // 팝업과 자식 그래픽의 클릭 및 드래그 가로채기 방지
            foreach (Graphic graphic in GetComponentsInChildren<Graphic>(true))
                graphic.raycastTarget = false;
            foreach (CanvasGroup group in GetComponentsInChildren<CanvasGroup>(true))
            {
                group.blocksRaycasts = false;
                group.interactable = false;
            }
        }

        #region API

        /// <summary>
        /// 현재 내용으로 패널을 표시합니다.
        /// </summary>
        public void Show()
        {
            gameObject.SetActive(true);
        }

        /// <summary>
        /// 내용을 유지한 채 패널을 숨깁니다.
        /// </summary>
        public void Hide()
        {
            gameObject.SetActive(false);
        }

        public void SetUnitIcon(Sprite icon)
        {
            if (unitIcon == null)
                return;

            unitIcon.sprite = icon;
            unitIcon.enabled = icon != null;
        }

        public void SetUnitName(string unitName)
        {
            if (unitNameText != null)
                unitNameText.text = unitName ?? string.Empty;
        }

        public void SetSynergies(IReadOnlyList<string> synergyNames)
        {
            ClearSynergies();

            if (synergyNames == null || synergyBadgeRoot == null || synergyBadgeItemPrefab == null)
            {
                return;
            }

            foreach (string synergyName in synergyNames)
            {
                InfoSynergyItemView badgeItem = Instantiate(synergyBadgeItemPrefab,synergyBadgeRoot,false);
                badgeItem.SetName(synergyName ?? string.Empty);
                badgeItem.SetVisible(true);

                synergyBadgeItems.Add(badgeItem);
            }
        }

        /// <summary>
        /// 지정한 슬롯의 스킬 아이콘과 설명을 갱신합니다.
        /// 아이콘이 null이면 해당 Image만 숨깁니다.
        /// </summary>
        public void SetSkill(int skillIndex,Sprite icon,string description)
        {
            if (skillIndex < 0)
                return;

            if (skillIcons != null && skillIndex < skillIcons.Length)
            {
                Image targetIcon = skillIcons[skillIndex];

                if (targetIcon != null)
                {
                    targetIcon.sprite = icon;
                    targetIcon.enabled = icon != null;
                }
            }

            if (skillDescriptionTexts != null && skillIndex < skillDescriptionTexts.Length)
            {
                TMP_Text targetText = skillDescriptionTexts[skillIndex];

                if (targetText != null)
                    targetText.text = description ?? string.Empty;
            }
        }

        public void SetConceptDescription(string description)
        {
            if (conceptDescriptionText != null)
                conceptDescriptionText.text = description ?? string.Empty;
        }

        /// <summary>
        /// 표시 여부를 변경하지 않고 내용만 초기화합니다.
        /// </summary>
        public void ClearDetail()
        {
            SetUnitIcon(null);
            SetUnitName(string.Empty);
            ClearSynergies();

            int iconCount = skillIcons != null ? skillIcons.Length : 0;
            int descriptionCount = skillDescriptionTexts != null ? skillDescriptionTexts.Length : 0;
            int count = Mathf.Max(iconCount, descriptionCount);

            for (int index = 0; index < count; index++)
                SetSkill(index, null, string.Empty);

            SetConceptDescription(string.Empty);
        }

        #endregion

        #region Private Methods

        private void ClearSynergies()
        {
            foreach (InfoSynergyItemView badgeItem in synergyBadgeItems)
            {
                if (badgeItem == null)
                    continue;

                badgeItem.SetVisible(false);
                Destroy(badgeItem.gameObject);
            }

            synergyBadgeItems.Clear();
        }

        #endregion
    }
}
