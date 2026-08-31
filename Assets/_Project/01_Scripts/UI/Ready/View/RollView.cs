using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OzGameLab01.UI
{
    [DisallowMultipleComponent]
    public sealed class RollView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Button rollButton;
        [SerializeField] private TMP_Text rollText;

        #region Properties

        public Button RollButton => rollButton;
        public TMP_Text RollText => rollText;

        public bool IsVisible => gameObject.activeSelf;

        public bool IsInteractable
        {
            get => rollButton != null && rollButton.interactable;
            set => SetInteractable(value);
        }

        public string Label
        {
            get => rollText != null ? rollText.text : string.Empty;
            set
            {
                if (rollText != null)
                {
                    rollText.text = value ?? string.Empty;
                }
            }
        }

        public event Action<RollView> RollClicked; //롤 버튼 클릭 이벤트

        #endregion

        #region Lifecycle

        private void OnEnable()
        {
            if (rollButton != null)
            {
                rollButton.onClick.AddListener(HandleRollClick);
            }
        }

        private void OnDisable()
        {
            if (rollButton != null)
            {
                rollButton.onClick.RemoveListener(HandleRollClick);
            }
        }

        #endregion

        #region API

        public void Show()
        {
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        public void SetLabel(string value)
        {
            Label = value;
        }

        public void SetInteractable(bool value)
        {
            if (rollButton != null)
            {
                rollButton.interactable = value;
            }
        }

        #endregion

        #region Private Methods

        private void HandleRollClick()
        {
            RollClicked?.Invoke(this);
        }

        #endregion
    }
}