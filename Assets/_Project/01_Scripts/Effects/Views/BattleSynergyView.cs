using UnityEngine;
using UnityEngine.UI;

namespace OzGameLab01.UI.Battle
{
    [DisallowMultipleComponent]
    public sealed class BattleSynergyView : MonoBehaviour
    {
        [Header("References")]
        [UnityEngine.Serialization.FormerlySerializedAs("synergyContentRoot")]
        [SerializeField] private Transform _synergyContentRoot;

        #region Properties

        public Transform SynergyContentRoot => _synergyContentRoot;

        public bool IsVisible => gameObject.activeSelf;

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

        public void RefreshLayout()
        {
            if (_synergyContentRoot is RectTransform rectTransform)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
            }
        }

        #endregion
    }
}