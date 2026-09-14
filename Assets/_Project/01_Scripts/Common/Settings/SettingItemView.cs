using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OzGameLab01.UI.Settings
{
    [DisallowMultipleComponent]
    public sealed class SettingItemView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TMP_Text labelText;
        [SerializeField] private TMP_Text descriptionText;
        [SerializeField] private Button button;

        #region Properties

        public TMP_Text LabelText => labelText;
        public TMP_Text DescriptionText => descriptionText;
        public Button Button => button;

        public string Label
        {
            get => labelText != null ? labelText.text : string.Empty;
            set
            {
                if (labelText != null)
                {
                    labelText.text = value ?? string.Empty;
                }
            }
        }

        public string Description
        {
            get => descriptionText != null ? descriptionText.text : string.Empty;
            set
            {
                if (descriptionText != null)
                {
                    descriptionText.text = value ?? string.Empty;
                }
            }
        }

        public bool Interactable
        {
            get => button != null && button.interactable;
            set
            {
                if (button != null)
                {
                    button.interactable = value;
                }
            }
        }

        public event Action<SettingItemView> Clicked; //설정 항목 클릭 이벤트

        #endregion

        #region Lifecycle

        private void OnEnable()
        {
            if (button != null)
            {
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

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (button == null)
            {
                button = GetComponentInChildren<Button>(true);
            }

            TMP_Text[] textComponents = GetComponentsInChildren<TMP_Text>(true);

            if (labelText == null && textComponents.Length > 0)
            {
                labelText = textComponents[0];
            }

            if (descriptionText == null && textComponents.Length > 1)
            {
                descriptionText = textComponents[1];
            }
        }
#endif

        #endregion

        #region API

        public void SetLabel(string value)
        {
            Label = value;
        }

        public void SetDescription(string value)
        {
            Description = value;
        }

        public void SetDescriptionVisible(bool visible)
        {
            if (descriptionText != null)
            {
                descriptionText.gameObject.SetActive(visible);
            }
        }

        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }

        #endregion

        #region Private Methods

        private void HandleClick()
        {
            Clicked?.Invoke(this);
        }

        #endregion
    }
}
