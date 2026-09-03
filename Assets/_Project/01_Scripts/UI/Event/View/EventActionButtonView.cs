using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OzGameLab01.UI
{
    public class EventActionButtonView : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private TextMeshProUGUI labelText;

        private Action _onClick;

        public void Bind(string label, Action onClick)
        {
            labelText.text = label ?? string.Empty;
            _onClick = onClick;

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
            _onClick?.Invoke();
        }
    }
}