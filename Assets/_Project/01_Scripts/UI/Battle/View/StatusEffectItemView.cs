using UnityEngine;
using UnityEngine.UI;

namespace OzGameLab01.UI.Battle
{
    /// <summary>
    /// 전투 중 상태이상 아이콘과 지속시간 게이지를 표시하는 UI View입니다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class StatusEffectItemView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Image statusIcon;
        [SerializeField] private Image durationFill;

        #region Properties

        public bool IsVisible => gameObject.activeSelf;

        #endregion

        #region Public API

        /// <summary>
        /// 상태이상 아이콘을 설정합니다.
        /// </summary>
        public void SetIcon(Sprite icon)
        {
            if (statusIcon != null)
            {
                statusIcon.sprite = icon;
                statusIcon.enabled = icon != null;
            }
        }

        /// <summary>
        /// 남은 시간과 전체 지속시간으로 게이지를 설정합니다.
        /// 적용 직후 1에서 시작하여 만료 시 0이 됩니다.
        /// </summary>
        public void SetDuration(float remaining, float duration)
        {
            float normalized = duration > 0f ? Mathf.Clamp01(remaining / duration) : 0f;
            SetDurationNormalized(normalized);
        }

        /// <summary>
        /// 남은 지속시간 비율을 설정합니다.
        /// 1은 전체 시간이 남은 상태, 0은 만료 상태입니다.
        /// </summary>
        public void SetDurationNormalized(float normalized)
        {
            if (durationFill != null)
            {
                durationFill.fillAmount = Mathf.Clamp01(normalized);
            }
        }

        /// <summary>
        /// 지속시간 게이지의 표시 여부를 설정합니다.
        /// 지속시간이 없는 효과는 게이지를 숨길 수 있습니다.
        /// </summary>
        public void SetDurationVisible(bool visible)
        {
            if (durationFill != null)
            {
                durationFill.gameObject.SetActive(visible);
            }
        }

        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }

        /// <summary>
        /// 아이콘과 게이지를 초기화합니다.
        /// 루트의 활성 상태는 변경하지 않습니다.
        /// </summary>
        public void Clear()
        {
            SetIcon(null);
            SetDurationNormalized(0f);
            SetDurationVisible(true);
        }

        #endregion
    }
}