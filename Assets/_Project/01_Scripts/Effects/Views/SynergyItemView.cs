using System;
using System.Threading.Tasks;
using OzGameLab01.Data;
using OzGameLab01.Managers;
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
        [UnityEngine.Serialization.FormerlySerializedAs("rectTransform")]
        [SerializeField] private RectTransform _rectTransform;
        [UnityEngine.Serialization.FormerlySerializedAs("background")]
        [SerializeField] private Image _background;
        [UnityEngine.Serialization.FormerlySerializedAs("icon")]
        [SerializeField] private Image _icon;
        [UnityEngine.Serialization.FormerlySerializedAs("titleText")]
        [SerializeField] private TMP_Text _titleText;
        [UnityEngine.Serialization.FormerlySerializedAs("stackText")]
        [SerializeField] private TMP_Text _stackText;

        private bool _isInteractable = true;
        private string _currentIconAddress;
        private SynergyData _synergyData;
        private int _currentCount;

        #region Properties

        public RectTransform RectTransform => _rectTransform;
        public RectTransform TooltipAnchor => _rectTransform;

        public Image Background => _background;
        public Image Icon => _icon;
        public TMP_Text TitleText => _titleText;
        public TMP_Text StackText => _stackText;
        public SynergyData SynergyData => _synergyData;
        public int CurrentCount => _currentCount;

        public string Title
        {
            get => _titleText != null ? _titleText.text : string.Empty;
            set
            {
                if (_titleText != null)
                {
                    _titleText.text = value ?? string.Empty;
                }
            }
        }

        public string StackValue
        {
            get => _stackText != null ? _stackText.text : string.Empty;
            set
            {
                if (_stackText != null)
                {
                    _stackText.text = value ?? string.Empty;
                }
            }
        }

        public bool IsInteractable
        {
            get => _isInteractable;
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

        public void BindTooltip(SynergyData synergyData, int currentCount)
        {
            _synergyData = synergyData;
            _currentCount = Mathf.Max(0, currentCount);
        }

        public void SetIcon(Sprite sprite)
        {
            if (_icon != null)
            {
                _icon.sprite = sprite;
                _icon.enabled = sprite != null;
            }
        }

        public async Task SetIconAsync(string iconAddress)
        {
            _currentIconAddress = iconAddress;
            SetIcon(null);
            if (string.IsNullOrWhiteSpace(iconAddress))
                return;

            Sprite sprite = await SpriteManager.GetSpriteAsync(iconAddress);
            if (this != null && _currentIconAddress == iconAddress)
                SetIcon(sprite);
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
