using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OzGameLab01.UI.Title
{
    public sealed class SettingsActionButtonView : MonoBehaviour
    {
        [SerializeField] private Button _button;
        [SerializeField] private TMP_Text _label;

        private Action _onClicked;

        private void Awake()
        {
            _button.onClick.AddListener(OnClicked);
        }

        private void OnDestroy()
        {
            _button.onClick.RemoveListener(OnClicked);
            _onClicked = null;
        }

        public void Initialize(string label, Action onClicked)
        {
            _label.text = label;
            _onClicked = onClicked;
        }

        public void SetInteractable(bool interactable)
        {
            _button.interactable = interactable;
        }

        private void OnClicked()
        {
            _onClicked?.Invoke();
        }
    }
}