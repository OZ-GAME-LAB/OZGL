using UnityEngine;
using UnityEngine.UI;

namespace OzGameLab01.UI.Battle
{
    [DisallowMultipleComponent]
    public sealed class BattleArtifactView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform artifactContentRoot;

        #region Properties

        public Transform ArtifactContentRoot => artifactContentRoot;

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
            if (artifactContentRoot is RectTransform rectTransform)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
            }
        }

        #endregion
    }
}