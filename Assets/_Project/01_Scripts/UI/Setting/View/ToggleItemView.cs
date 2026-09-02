using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OzGameLab01.UI.Settings
{
    [DisallowMultipleComponent]
    public sealed class ToggleItemView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TMP_Text labelText;
        [SerializeField] private Toggle toggle;

        #region Properties

        public TMP_Text LabelText => labelText;
        public Toggle Toggle => toggle;

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

        public bool IsOn
        {
            get => toggle != null && toggle.isOn;
            set => SetIsOn(value);
        }

        public bool Interactable
        {
            get => toggle != null && toggle.interactable;
            set
            {
                if (toggle != null)
                {
                    toggle.interactable = value;
                }
            }
        }

        public event Action<ToggleItemView, bool> ValueChanged; //토글 값 변경 이벤트

        #endregion

        #region Lifecycle

        private void OnEnable()
        {
            if (toggle != null)
            {
                toggle.onValueChanged.AddListener(HandleValueChanged);
            }
        }

        private void OnDisable()
        {
            if (toggle != null)
            {
                toggle.onValueChanged.RemoveListener(HandleValueChanged);
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (toggle == null)
            {
                toggle = GetComponentInChildren<Toggle>(true);
            }

            if (labelText == null)
            {
                labelText = GetComponentInChildren<TMP_Text>(true);
            }
        }
#endif

        #endregion

        #region API

        public void SetLabel(string value)
        {
            Label = value;
        }

        // 외부 데이터로 화면만 갱신할 때 사용합니다.
        // ValueChanged 이벤트는 호출하지 않습니다.
        public void SetIsOn(bool isOn)
        {
            if (toggle == null)
            {
                return;
            }

            toggle.SetIsOnWithoutNotify(isOn);
        }

        // 의도적으로 ValueChanged 이벤트를 전달할 때 사용합니다.
        public void SetIsOnAndNotify(bool isOn)
        {
            if (toggle == null)
            {
                return;
            }

            toggle.isOn = isOn;
        }

        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }

        #endregion

        #region Private Methods

        private void HandleValueChanged(bool isOn)
        {
            ValueChanged?.Invoke(this, isOn);
        }

        #endregion
    }
}
