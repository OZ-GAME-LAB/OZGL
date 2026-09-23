using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using OzGameLab01.Managers;

namespace OzGameLab01.UI
{
    [DisallowMultipleComponent]
    public sealed class ArtifactInfoItemView : MonoBehaviour,IPointerClickHandler,IPointerEnterHandler,IPointerExitHandler, IRelicDisplayable
    {
        [Header("References")]
        [UnityEngine.Serialization.FormerlySerializedAs("rectTransform")]
        [SerializeField] private RectTransform _rectTransform;
        [UnityEngine.Serialization.FormerlySerializedAs("background")]
        [SerializeField] private Image _background;
        [UnityEngine.Serialization.FormerlySerializedAs("icon")]
        [SerializeField] private Image _icon;

        private bool _isInteractable = true;
        private string _currentIconAddress;

        #region Properties

        public RectTransform RectTransform => _rectTransform;
        public RectTransform TooltipAnchor => _rectTransform;

        public Image Background => _background;
        public Image Icon => _icon;

        public bool IsInteractable
        {
            get => _isInteractable;
            set => SetInteractable(value);
        }

        public event Action<ArtifactInfoItemView, PointerEventData> Clicked; //아이템 클릭 이벤트
        public event Action<ArtifactInfoItemView, PointerEventData> PointerEntered; //아이템 포인터 진입 이벤트
        public event Action<ArtifactInfoItemView, PointerEventData> PointerExited; //아이템 포인터 이탈 이벤트

        public async Task UpdateRelicIconAsync(string iconAddress) => await SetIconAsync(iconAddress);

        #endregion

        #region API

        public void SetIcon(Sprite sprite)
        {
            _currentIconAddress = null;
            if (_icon != null)
            {
                _icon.sprite = sprite;
            }
        }

        public async Task SetIconAsync(string iconAddress)
        {
            if (string.IsNullOrEmpty(iconAddress))
            {
                SetIcon(null);
                return;
            }

            _currentIconAddress = iconAddress;
            Sprite sprite = await SpriteManager.GetSpriteAsync(iconAddress);

            // 비동기 완료 후 요청 주소 일치 여부 검증 (레이스 조건 방지)
            if (_currentIconAddress == iconAddress && _icon != null)
            {
                _icon.sprite = sprite;
                SetIconVisible(sprite != null);
            }
        }

        public void SetIconVisible(bool visible)
        {
            if (_icon != null)
            {
                _icon.enabled = visible;
            }
        }

        public void SetIconColor(Color color)
        {
            if (_icon != null)
            {
                _icon.color = color;
            }
        }

        public void SetBackgroundColor(Color color)
        {
            if (_background != null)
            {
                _background.color = color;
            }
        }

        public void SetInteractable(bool value)
        {
            _isInteractable = value;

            if (_background != null)
            {
                _background.raycastTarget = value;
            }
        }

        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!_isInteractable)
            {
                return;
            }

            Clicked?.Invoke(this, eventData);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!_isInteractable)
            {
                return;
            }

            PointerEntered?.Invoke(this, eventData);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!_isInteractable)
            {
                return;
            }

            PointerExited?.Invoke(this, eventData);
        }

        #endregion
    }
}