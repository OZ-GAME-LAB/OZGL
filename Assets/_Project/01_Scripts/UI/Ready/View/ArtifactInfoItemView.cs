using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace OzGameLab01.UI
{
    [DisallowMultipleComponent]
    public sealed class ArtifactInfoItemView : MonoBehaviour,IPointerClickHandler,IPointerEnterHandler,IPointerExitHandler
    {
        [Header("References")]
        [SerializeField] private RectTransform rectTransform;
        [SerializeField] private Image background;
        [SerializeField] private Image icon;

        private bool isInteractable = true;

        #region Properties

        public RectTransform RectTransform => rectTransform;
        public RectTransform TooltipAnchor => rectTransform;

        public Image Background => background;
        public Image Icon => icon;

        public bool IsInteractable
        {
            get => isInteractable;
            set => SetInteractable(value);
        }

        public event Action<ArtifactInfoItemView, PointerEventData> Clicked; //아이템 클릭 이벤트
        public event Action<ArtifactInfoItemView, PointerEventData> PointerEntered; //아이템 포인터 진입 이벤트
        public event Action<ArtifactInfoItemView, PointerEventData> PointerExited; //아이템 포인터 이탈 이벤트

        #endregion

        #region API

        public void SetIcon(Sprite sprite)
        {
            if (icon != null)
            {
                icon.sprite = sprite;
            }
        }

        public void SetIconVisible(bool visible)
        {
            if (icon != null)
            {
                icon.enabled = visible;
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