using TMPro;
using UnityEngine;

namespace OzGameLab01.UI
{
    [DisallowMultipleComponent]
    public sealed class EffectRowView : MonoBehaviour
    {
        [SerializeField] private TMP_Text effectText;

        public void Bind(string text, Color color)
        {
            if (effectText == null)
                return;

            effectText.text = text ?? string.Empty;
            effectText.color = color;
            effectText.raycastTarget = false;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (effectText == null)
                effectText = GetComponentInChildren<TMP_Text>(true);
        }
#endif
    }
}