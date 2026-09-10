using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OzGameLab01.UI
{
    [DisallowMultipleComponent]
    public sealed class EventActionButtonView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Button button;
        [SerializeField] private TextMeshProUGUI labelText;

        [Header("Icon")]
        [SerializeField] private GameObject iconRoot;
        [SerializeField] private Image iconImage;

        private Action _onClick;

        #region Unity Lifecycle

        private void OnEnable()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(HandleClick);
                button.onClick.AddListener(HandleClick);
            }
        }

        private void OnDisable()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(HandleClick);
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// 아이콘이 없는 일반 이벤트 액션을 표시합니다.
        /// 기존 표시 데이터와 클릭 콜백을 새로운 액션 정보로 교체합니다.
        /// </summary>
        /// <param name="label">버튼에 표시할 액션 이름입니다.</param>
        /// <param name="onClick">액션 선택 시 호출되는 단발성 콜백입니다.</param>
        public void Bind(string label,Action onClick)
        {
            Bind(label,null,onClick);
        }

        /// <summary>
        /// 아이콘이 포함된 이벤트 액션을 표시합니다.
        /// 아이콘이 null이면 일반 액션 형태로 표시하고,
        /// 아이콘이 존재하면 아이콘 영역을 활성화합니다.
        /// 기존 표시 데이터와 클릭 콜백은 새로운 정보로 교체됩니다.
        /// </summary>
        /// <param name="label">버튼에 표시할 액션 또는 아이템 이름입니다.</param>
        /// <param name="icon">표시할 아이콘입니다. null이면 아이콘 영역을 숨깁니다.</param>
        /// <param name="onClick">액션 선택 시 호출되는 단발성 콜백입니다.</param>
        public void Bind(string label,Sprite icon,Action onClick)
        {
            gameObject.SetActive(true);

            _onClick = onClick;

            if (labelText != null)
            {
                labelText.text = label ?? string.Empty;
            }

            SetIcon(icon);

            if (button != null)
            {
                button.interactable = true;
            }
        }

        /// <summary>
        /// 현재 액션 버튼의 입력 가능 여부를 변경합니다.
        /// </summary>
        /// <param name="isInteractable">
        /// true면 입력을 허용하고 false면 입력을 차단합니다.
        /// </param>
        public void SetInteractable(bool isInteractable)
        {
            if (button != null)
            {
                button.interactable = isInteractable;
            }
        }

        /// <summary>
        /// 현재 액션의 표시 데이터와 클릭 콜백을 초기화합니다.
        /// 프리팹 자체는 활성 상태로 유지합니다.
        /// </summary>
        public void Clear()
        {
            _onClick = null;

            if (labelText != null)
            {
                labelText.text = string.Empty;
            }

            SetIcon(null);

            if (button != null)
            {
                button.interactable = false;
            }
        }

        /// <summary>
        /// 현재 액션을 초기화하고 화면에서 숨깁니다.
        /// 이후 Bind 호출 시 다시 활성화하여 재사용할 수 있습니다.
        /// </summary>
        public void Hide()
        {
            Clear();
            gameObject.SetActive(false);
        }

        #endregion

        #region Visual

        private void SetIcon(Sprite icon)
        {
            bool hasIcon = icon != null;

            if (iconRoot != null)
            {
                iconRoot.SetActive(hasIcon);
            }

            if (iconImage != null)
            {
                iconImage.sprite = icon;
            }
        }

        #endregion

        #region Event Handler

        private void HandleClick()
        {
            _onClick?.Invoke();
        }

        #endregion
    }
}