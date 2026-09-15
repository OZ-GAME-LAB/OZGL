using TMPro;
using UnityEngine;

namespace OzGameLab01.UI
{
    [DisallowMultipleComponent]
    public sealed class EffectRowView : MonoBehaviour
    {
        [UnityEngine.Serialization.FormerlySerializedAs("effectText")]
        [SerializeField] private TMP_Text _effectText;

        public void Bind(string text, Color color)
        {
            if (_effectText == null)
                return;

            _effectText.text = text ?? string.Empty;
            _effectText.color = color;
            _effectText.raycastTarget = false;
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