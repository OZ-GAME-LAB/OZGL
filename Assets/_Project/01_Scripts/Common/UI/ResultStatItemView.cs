using TMPro;
using UnityEngine;

namespace OzGameLab01.UI
{
    /// <summary>결과 화면의 통계 한 항목을 표시합니다.</summary>
    [DisallowMultipleComponent]
    public sealed class ResultStatItemView : MonoBehaviour
    {
        [SerializeField] private TMP_Text labelText;
        [SerializeField] private TMP_Text valueText;

        #region Properties

        /// <summary>통계 항목명을 표시하는 텍스트입니다.</summary>
        public TMP_Text LabelText => labelText;

        /// <summary>통계 값을 표시하는 텍스트입니다.</summary>
        public TMP_Text ValueText => valueText;

        #endregion

        #region Public API

        /// <summary>항목명과 표시 값을 함께 설정합니다. 숫자와 단위의 형식은 호출자가 결정합니다.</summary>
        public void SetData(string label, string value)
        {
            SetLabel(label);
            SetValue(value);
        }

        /// <summary>통계 항목명을 설정합니다.</summary>
        public void SetLabel(string value)
        {
            if (labelText != null) labelText.text = value ?? string.Empty;
        }

        /// <summary>통계의 표시 값을 설정합니다.</summary>
        public void SetValue(string value)
        {
            if (valueText != null) valueText.text = value ?? string.Empty;
        }

        #endregion
    }
}
