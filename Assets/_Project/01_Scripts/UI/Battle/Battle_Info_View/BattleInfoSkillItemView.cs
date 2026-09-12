using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace OzGameLab01.UI
{
    [DisallowMultipleComponent]
    public sealed class BattleInfoSkillItemView : MonoBehaviour,IPointerEnterHandler,IPointerExitHandler
    {
        [SerializeField] private Image skillIcon;
        [SerializeField] private GameObject hoverVisual;

        private string detailTitle;
        private string detailDescription;

        /// <summary>
        /// 스킬 아이템에 포인터가 진입했을 때 발생합니다.
        /// </summary>
        public event Action<BattleInfoSkillItemView> HoverEntered;

        /// <summary>
        /// 스킬 아이템에서 포인터가 벗어났을 때 발생합니다.
        /// </summary>
        public event Action<BattleInfoSkillItemView> HoverExited;

        /// <summary>
        /// 현재 스킬 아이콘입니다.
        /// </summary>
        public Sprite Icon => skillIcon != null ? skillIcon.sprite : null;

        /// <summary>
        /// Hover 상세 정보에 표시할 스킬 이름입니다.
        /// </summary>
        public string DetailTitle => detailTitle;

        /// <summary>
        /// Hover 상세 정보에 표시할 스킬 설명입니다.
        /// TMP Rich Text 문자열을 사용할 수 있습니다.
        /// </summary>
        public string DetailDescription => detailDescription;


        #region Unity Lifecycle

        private void Awake()
        {
            SetHovered(false);
        }

        private void OnDisable()
        {
            SetHovered(false);
        }

        #endregion


        #region Public API

        /// <summary>
        /// 스킬 아이템에 표시할 데이터를 설정합니다.
        /// </summary>
        /// <param name="icon">표시할 스킬 아이콘입니다.</param>
        /// <param name="title">상세 정보에 표시할 스킬 이름입니다.</param>
        /// <param name="description">
        /// 상세 정보에 표시할 설명입니다.
        /// TMP Rich Text가 포함될 수 있습니다.
        /// </param>
        public void Bind(Sprite icon,string title,string description)
        {
            detailTitle = title ?? string.Empty;
            detailDescription = description ?? string.Empty;

            if (skillIcon != null)
            {
                skillIcon.sprite = icon;
                skillIcon.enabled = icon != null;
            }

            SetHovered(false);
        }

        /// <summary>
        /// 현재 스킬 아이템의 표시 데이터를 초기화합니다.
        /// </summary>
        public void Clear()
        {
            detailTitle = string.Empty;
            detailDescription = string.Empty;

            if (skillIcon != null)
            {
                skillIcon.sprite = null;
                skillIcon.enabled = false;
            }

            SetHovered(false);
        }

        /// <summary>
        /// Hover 시각 효과의 활성 상태를 설정합니다.
        /// </summary>
        /// <param name="hovered">Hover 상태이면 true입니다.</param>
        public void SetHovered(bool hovered)
        {
            if (hoverVisual != null)
            {
                hoverVisual.SetActive(hovered);
            }
        }

        #endregion


        #region Event Handlers

        public void OnPointerEnter(PointerEventData eventData)
        {
            SetHovered(true);
            HoverEntered?.Invoke(this);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            SetHovered(false);
            HoverExited?.Invoke(this);
        }

        #endregion
    }
}