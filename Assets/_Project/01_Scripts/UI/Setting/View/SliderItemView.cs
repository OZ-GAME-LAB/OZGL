using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OzGameLab01.UI.Settings
{
    [DisallowMultipleComponent]
    public sealed class SliderItemView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TMP_Text labelText;
        [SerializeField] private Slider slider;

        #region Properties

        public TMP_Text LabelText => labelText;
        public Slider Slider => slider;

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

        public float Value
        {
            get => slider != null ? slider.value : 0f;
            set => SetValue(value);
        }

        public float NormalizedValue
        {
            get => slider != null ? slider.normalizedValue : 0f;
            set => SetNormalizedValue(value);
        }

        public float MinValue => slider != null ? slider.minValue : 0f;
        public float MaxValue => slider != null ? slider.maxValue : 0f;

        public bool Interactable
        {
            get => slider != null && slider.interactable;
            set
            {
                if (slider != null)
                {
                    slider.interactable = value;
                }
            }
        }

        public event Action<SliderItemView, float> ValueChanged; //슬라이더 값 변경 이벤트

        #endregion

        #region Lifecycle

        private void OnEnable()
        {
            if (slider != null)
            {
                slider.onValueChanged.AddListener(HandleValueChanged);
            }
        }

        private void OnDisable()
        {
            if (slider != null)
            {
                slider.onValueChanged.RemoveListener(HandleValueChanged);
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (slider == null)
            {
                slider = GetComponentInChildren<Slider>(true);
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
        public void SetValue(float value)
        {
            if (slider == null)
            {
                return;
            }

            slider.SetValueWithoutNotify(value);
        }

        // 의도적으로 ValueChanged 이벤트를 전달할 때 사용합니다.
        public void SetValueAndNotify(float value)
        {
            if (slider == null)
            {
                return;
            }

            slider.value = value;
        }

        public void SetNormalizedValue(float value)
        {
            if (slider == null)
            {
                return;
            }

            float clampedValue = Mathf.Clamp01(value);
            float sliderValue = Mathf.Lerp(slider.minValue, slider.maxValue, clampedValue);

            slider.SetValueWithoutNotify(sliderValue);
        }

        public void SetRange(float minValue, float maxValue, bool wholeNumbers = false)
        {
            if (slider == null)
            {
                return;
            }

            slider.minValue = minValue;
            slider.maxValue = Mathf.Max(minValue, maxValue);
            slider.wholeNumbers = wholeNumbers;

            SetValue(slider.value);
        }

        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }

        #endregion

        #region Private Methods

        private void HandleValueChanged(float value)
        {
            ValueChanged?.Invoke(this, value);
        }

        #endregion
    }
}
