using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OzGameLab01.UI
{
    [DisallowMultipleComponent]
    public sealed class EffectRowView : MonoBehaviour
    {
        [UnityEngine.Serialization.FormerlySerializedAs("effectText")]
        [SerializeField] private TMP_Text _effectText;

        [Header("Layout")]
        [SerializeField, Min(1f)] private float _preferredHeight = 50f;
        [SerializeField, Min(1f)] private float _minimumFontSize = 12f;
        [SerializeField, Min(1f)] private float _maximumFontSize = 22f;
        [SerializeField, Min(0f)] private float _horizontalPadding = 10f;
        [SerializeField, Min(0f)] private float _verticalPadding = 6f;

        private LayoutElement _layoutElement;

        public float PreferredHeight =>
            _layoutElement != null ? _layoutElement.preferredHeight : _preferredHeight;

        private void Awake()
        {
            ConfigureLayout();
        }

        public void Bind(string text, Color color)
        {
            if (_effectText == null)
                return;

            ConfigureLayout();
            _effectText.text = text ?? string.Empty;
            _effectText.color = color;
            _effectText.raycastTarget = false;
            RefreshLayout();

            if (transform is RectTransform rectTransform)
                LayoutRebuilder.MarkLayoutForRebuild(rectTransform);
        }

        public void RefreshLayout(float availableWidth = -1f)
        {
            ConfigureLayout();

            if (_effectText == null)
                return;

            float rowWidth = availableWidth > 0f
                ? availableWidth
                : transform is RectTransform rectTransform
                    ? rectTransform.rect.width
                    : 0f;
            float textWidth = Mathf.Max(1f, rowWidth - _horizontalPadding * 2f);
            float textHeight = _effectText.GetPreferredValues(
                _effectText.text ?? string.Empty,
                textWidth,
                Mathf.Infinity).y;
            float preferredHeight = Mathf.Max(
                _preferredHeight,
                Mathf.Ceil(textHeight + _verticalPadding * 2f));

            _layoutElement.minHeight = preferredHeight;
            _layoutElement.preferredHeight = preferredHeight;
            _layoutElement.flexibleHeight = 0f;
        }

        /// <summary>
        /// 효과 텍스트와 좌우 여백을 포함한 권장 너비를 반환합니다.
        /// </summary>
        public float GetPreferredWidth()
        {
            ConfigureLayout();

            if (_effectText == null)
                return 0f;

            float textWidth = _effectText.GetPreferredValues(
                _effectText.text ?? string.Empty,
                Mathf.Infinity,
                Mathf.Infinity).x;

            return Mathf.Ceil(textWidth + _horizontalPadding * 2f);
        }

        private void ConfigureLayout()
        {
            if (_layoutElement == null)
            {
                _layoutElement = GetComponent<LayoutElement>();
                if (_layoutElement == null)
                    _layoutElement = gameObject.AddComponent<LayoutElement>();
            }

            if (_effectText == null)
                return;

            _effectText.enableAutoSizing = true;
            _effectText.fontSizeMin = Mathf.Min(_minimumFontSize, _maximumFontSize);
            _effectText.fontSizeMax = Mathf.Max(_minimumFontSize, _maximumFontSize);
            _effectText.overflowMode = TextOverflowModes.Overflow;

            RectTransform textRect = _effectText.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(_horizontalPadding, _verticalPadding);
            textRect.offsetMax = new Vector2(-_horizontalPadding, -_verticalPadding);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_effectText == null)
                _effectText = GetComponentInChildren<TMP_Text>(true);
        }
#endif
    }
}
