using UnityEngine;
using UnityEngine.UI;

namespace OzGameLab01.UI.Battle
{
    [DisallowMultipleComponent]
    public sealed class BattleSynergyView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform synergyContentRoot;

        #region Properties

        public Transform SynergyContentRoot => synergyContentRoot;

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
            if (synergyContentRoot is RectTransform rectTransform)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
            }
        }

        #endregion
    }
}