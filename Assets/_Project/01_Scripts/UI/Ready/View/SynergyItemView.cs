using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace OzGameLab01.UI
{
    [DisallowMultipleComponent]
    public sealed class SynergyItemView : MonoBehaviour,
        IPointerClickHandler,
        IPointerEnterHandler,
        IPointerExitHandler
    {
        [Header("References")]
        [SerializeField] private RectTransform rectTransform;
        [SerializeField] private Image background;
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text stackText;

        private bool isInteractable = true;

        #region Properties

        public RectTransform RectTransform => rectTransform;
        public RectTransform TooltipAnchor => rectTransform;

        public Image Background => background;
        public Image Icon => icon;
        public TMP_Text TitleText => titleText;
        public TMP_Text StackText => stackText;

        public string Title
        {
            get => titleText != null ? titleText.text : string.Empty;
            set
            {
                if (titleText != null)
                {
                    titleText.text = value ?? string.Empty;
                }
            }
        }

        public string StackValue
        {
            get => stackText != null ? stackText.text : string.Empty;
            set
            {
                if (stackText != null)
                {
                    stackText.text = value ?? string.Empty;
                }
            }
        }

        public bool IsInteractable
        {
            get => isInteractable;
            set => SetInteractable(value);
        }

        public event Action<SynergyItemView, PointerEventData> Clicked; //시너지 아이템 클릭 이벤트
        public event Action<SynergyItemView, PointerEventData> PointerEntered; //시너지 아이템 포인터 진입 이벤트
        public event Action<SynergyItemView, PointerEventData> PointerExited; //시너지 아이템 포인터 이탈 이벤트

        #endregion

        #region API

        public void SetTitle(string value)
        {
            Title = value;
        }

        public void SetStackText(string value)
        {
            StackValue = value;
        }

        public void SetStackCount(int value)
        {
            SetStackText(value.ToString());
        }

        public void SetIcon(Sprite sprite)
        {
            if (icon != null)
            {
                icon.sprite = sprite;
            }
        }

        public void SetIconColor(Color color)
        {
            if (icon != null)
            {
                icon.color = color;
            }
        }

        public void SetBackgroundColor(Color color)
        {
            if (background != null)
            {
                background.color = color;
            }
        }

        public void SetInteractable(bool value)
        {
            isInteractable = value;

            if (background != null)
            {
                background.raycastTarget = value;
            }
        }

        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!isInteractable)
            {
                return;
            }

            Clicked?.Invoke(this, eventData);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!isInteractable)
            {
                return;
            }

            PointerEntered?.Invoke(this, eventData);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!isInteractable)
            {
                return;
            }

            PointerExited?.Invoke(this, eventData);
        }

        #endregion
    }
}