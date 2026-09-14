using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OzGameLab01.UI
{
    /// <summary>
    /// 적 스킬 쿨타임 UI를 관리하는 View입니다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EnemySkillCooldownItemView : MonoBehaviour
    {
        [SerializeField] private Image skillIcon;
        [SerializeField] private Image cooldownFill;
        [SerializeField] private TMP_Text cooldownText;

        #region Public API

        /// <summary>
        /// 표시할 스킬 아이콘을 설정합니다.
        /// </summary>
        /// <param name="icon">표시할 스킬 아이콘입니다.</param>
        public void SetIcon(Sprite icon)
        {
            if (skillIcon == null)
            {
                return;
            }

            skillIcon.sprite = icon;
            skillIcon.enabled = icon != null;
        }

        /// <summary>
        /// 남은 쿨타임과 전체 쿨타임을 기준으로
        /// 쿨타임 게이지와 텍스트를 갱신합니다.
        /// 게이지는 스킬 사용 직후 0에서 시작하여
        /// 사용 가능 상태가 될 때 1까지 채워집니다.
        /// </summary>
        /// <param name="remaining">현재 남은 쿨타임입니다.</param>
        /// <param name="duration">전체 쿨타임입니다.</param>
        public void SetCooldown(float remaining, float duration)
        {
            remaining = Mathf.Max(0f, remaining);

            float progress = duration > 0f ? 1f - Mathf.Clamp01(remaining / duration) : 1f;

            SetCooldownNormalized(progress);

            if (cooldownText != null)
            {
                cooldownText.text = remaining.ToString("0.00");
            }
        }

        /// <summary>
        /// 정규화된 쿨타임 진행도를 설정합니다.
        /// 0은 스킬 사용 직후, 1은 사용 가능한 상태를 의미합니다.
        /// </summary>
        /// <param name="normalized">0~1 범위의 쿨타임 진행도입니다.</param>
        public void SetCooldownNormalized(float normalized)
        {
            if (cooldownFill != null)
            {
                cooldownFill.fillAmount = Mathf.Clamp01(normalized);
            }
        }

        /// <summary>
        /// 쿨타임에 표시할 텍스트를 직접 설정합니다.
        /// </summary>
        /// <param name="text">표시할 문자열입니다.</param>
        public void SetCooldownText(string text)
        {
            if (cooldownText != null)
            {
                cooldownText.text = text ?? string.Empty;
            }
        }

        /// <summary>
        /// 스킬이 즉시 사용 가능한 상태로 표시되도록 설정합니다.
        /// </summary>
        public void SetReady()
        {
            SetCooldownNormalized(1f);

            if (cooldownText != null)
            {
                cooldownText.text = "0.00";
            }
        }

        /// <summary>
        /// 스킬 쿨타임 UI의 표시 상태를 설정합니다.
        /// </summary>
        /// <param name="visible">표시 여부입니다.</param>
        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }

        /// <summary>
        /// 현재 표시 중인 스킬 정보를 초기화합니다.
        /// </summary>
        public void Clear()
        {
            if (skillIcon != null)
            {
                skillIcon.sprite = null;
                skillIcon.enabled = false;
            }

            if (cooldownFill != null)
            {
                cooldownFill.fillAmount = 0f;
            }

            if (cooldownText != null)
            {
                cooldownText.text = string.Empty;
            }
        }

        #endregion
    }
}