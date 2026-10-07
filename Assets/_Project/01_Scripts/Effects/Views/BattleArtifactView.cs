using UnityEngine;
using UnityEngine.UI;

namespace OzGameLab01.UI.Battle
{
    [DisallowMultipleComponent]
    public sealed class BattleArtifactView : MonoBehaviour
    {
        [Header("References")]
        [UnityEngine.Serialization.FormerlySerializedAs("artifactContentRoot")]
        [SerializeField] private Transform _artifactContentRoot;

        #region Properties

        public Transform ArtifactContentRoot => _artifactContentRoot;

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
            if (_artifactContentRoot is RectTransform rectTransform)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
            }
        }

        #endregion
    }
}