using UnityEngine;
using UnityEngine.UI;

namespace OzGameLab01.UI
{
    /// <summary>기존 프레임과 원형 마스크를 유지하면서 결과 아이콘을 표시합니다.</summary>
    [DisallowMultipleComponent]
    public sealed class ResultIconItemView : MonoBehaviour
    {
        [SerializeField] private Image iconImage;

        #region Properties

        /// <summary>원형 마스크 내부의 아이콘 이미지입니다.</summary>
        public Image IconImage => iconImage;

        #endregion

        #region Public API

        /// <summary>아이콘을 설정합니다. 이미지가 없으면 아이콘만 숨기고 프레임은 유지합니다.</summary>
        public void SetIcon(Sprite sprite)
        {
            if (iconImage == null) return;
            iconImage.sprite = sprite;
            iconImage.enabled = sprite != null;
        }

        #endregion
    }
}
