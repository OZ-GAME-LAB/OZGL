using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace OzGameLab01.UI
{
    [DisallowMultipleComponent]
    public sealed class BattleInfoStatItemView : MonoBehaviour,IPointerEnterHandler,IPointerExitHandler
    {
        [SerializeField] private Image statIcon;
        [SerializeField] private TMP_Text valueText;

        private string detailTitle;
        private string detailDescription;

        /// <summary>
        /// 스탯 아이템에 포인터가 진입했을 때 발생합니다.
        /// </summary>
        public event Action<BattleInfoStatItemView> HoverEntered;

        /// <summary>
        /// 스탯 아이템에서 포인터가 벗어났을 때 발생합니다.
        /// </summary>
        public event Action<BattleInfoStatItemView> HoverExited;

        /// <summary>
        /// 현재 스탯 아이콘입니다.
        /// </summary>
        public Sprite Icon => statIcon != null ? statIcon.sprite : null;

        /// <summary>
        /// 현재 표시 중인 스탯 값입니다.
        /// </summary>
        public string Value => valueText != null ? valueText.text : string.Empty;

        /// <summary>
        /// Hover 상세 정보에 표시할 스탯 이름입니다.
        /// </summary>
        public string DetailTitle => detailTitle;

        /// <summary>
        /// Hover 상세 정보에 표시할 스탯 설명입니다.
        /// TMP Rich Text 문자열을 사용할 수 있습니다.
        /// </summary>
        public string DetailDescription => detailDescription;


        #region Public API

        /// <summary>
        /// 스탯 아이템에 표시할 데이터를 설정합니다.
        /// </summary>
        /// <param name="icon">표시할 스탯 아이콘입니다.</param>
        /// <param name="value">표시할 스탯 값입니다.</param>
        /// <param name="title">상세 정보에 표시할 스탯 이름입니다.</param>
        /// <param name="description">
        /// 상세 정보에 표시할 설명입니다.
        /// TMP Rich Text가 포함될 수 있습니다.
        /// </param>
        public void Bind(Sprite icon,string value,string title,string description)
        {
            detailTitle = title ?? string.Empty;
            detailDescription = description ?? string.Empty;

            if (statIcon != null)
            {
                statIcon.sprite = icon;
                statIcon.enabled = icon != null;
            }

            if (valueText != null)
            {
                valueText.text = value ?? string.Empty;
            }
        }

        /// <summary>
        /// 현재 스탯 아이템의 표시 데이터를 초기화합니다.
        /// </summary>
        public void Clear()
        {
            detailTitle = string.Empty;
            detailDescription = string.Empty;

            if (statIcon != null)
            {
                statIcon.sprite = null;
                statIcon.enabled = false;
            }

            if (valueText != null)
            {
                valueText.text = string.Empty;
            }
        }

        #endregion


        #region Event Handlers

        public void OnPointerEnter(PointerEventData eventData)
        {
            HoverEntered?.Invoke(this);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            HoverExited?.Invoke(this);
        }

        #endregion
    }
}