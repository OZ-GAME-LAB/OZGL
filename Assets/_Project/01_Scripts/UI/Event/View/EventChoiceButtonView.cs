using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OzGameLab01.UI
{
    public class EventChoiceButtonView : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private GameObject iconRoot;
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI choiceText;

        private string _choiceId;
        private Action<string> _onClick;

        public void Bind(EventChoiceDisplayData data, Action<string> onClick)
        {
            _choiceId = data.Id;
            _onClick = onClick;

            choiceText.text = data.Text ?? string.Empty;

            bool hasIcon = data.Icon != null;

            if (iconRoot != null)
            {
                iconRoot.SetActive(hasIcon);
            }

            if (hasIcon && iconImage != null)
            {
                iconImage.sprite = data.Icon;
            }

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(HandleClick);

            button.interactable = true;
        }

        public void SetInteractable(bool isInteractable)
        {
            button.interactable = isInteractable;
        }

        private void HandleClick()
        {
            _onClick?.Invoke(_choiceId);
        }
    }
}