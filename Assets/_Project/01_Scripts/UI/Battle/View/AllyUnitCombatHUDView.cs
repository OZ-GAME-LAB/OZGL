using UnityEngine;
using UnityEngine.UI;

namespace OzGameLab01.UI.Battle
{
    /// <summary>
    /// 실질적으로 전투 중인 아군의 유닛에 사용되는 HUD View입니다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class AllyUnitCombatHUDView : MonoBehaviour
    {
        [Header("Health")]
        [SerializeField] private Image healthFill;

        [Header("Skill Cooldown")]
        [SerializeField] private Image cooldownFill;

        #region Properties

        /// <summary>
        /// 현재 HUD의 활성 상태입니다.
        /// </summary>
        public bool IsVisible => gameObject.activeSelf;

        #endregion

        #region API

        /// <summary>
        /// 유닛의 현재 체력과 최대 체력을 기준으로 체력 게이지를 갱신합니다.
        /// </summary>
        /// <param name="current">현재 체력입니다.</param>
        /// <param name="max">최대 체력입니다.</param>
        public void SetHealth(float current, float max)
        {
            float normalized = max > 0f ? Mathf.Clamp01(current / max) : 0f;

            SetHealthNormalized(normalized);
        }

        /// <summary>
        /// 정규화된 체력 비율을 설정합니다.
        /// 0은 체력이 없는 상태이며 1은 최대 체력 상태입니다.
        /// </summary>
        /// <param name="normalized">0~1 범위의 체력 비율입니다.</param>
        public void SetHealthNormalized(float normalized)
        {
            if (healthFill != null)
            {
                healthFill.fillAmount = Mathf.Clamp01(normalized);
            }
        }

        /// <summary>
        /// 남은 쿨타임과 전체 쿨타임을 기준으로 스킬 게이지를 갱신합니다.
        /// 스킬 사용 직후 0에서 시작하여 사용 가능 상태가 될 때 1까지 채워집니다.
        /// </summary>
        /// <param name="remaining">현재 남은 쿨타임입니다.</param>
        /// <param name="duration">전체 쿨타임입니다.</param>
        public void SetSkillCooldown(float remaining, float duration)
        {
            remaining = Mathf.Max(0f, remaining);

            float normalized = duration > 0f ? 1f - Mathf.Clamp01(remaining / duration) : 1f;

            SetSkillCooldownNormalized(normalized);
        }

        /// <summary>
        /// 정규화된 스킬 쿨타임 진행도를 설정합니다.
        /// 0은 스킬 사용 직후이며 1은 사용 가능한 상태입니다.
        /// </summary>
        /// <param name="normalized">0~1 범위의 쿨타임 진행도입니다.</param>
        public void SetSkillCooldownNormalized(float normalized)
        {
            if (cooldownFill != null)
            {
                cooldownFill.fillAmount = Mathf.Clamp01(normalized);
            }
        }

        /// <summary>
        /// 스킬을 즉시 사용 가능한 상태로 표시합니다.
        /// </summary>
        public void SetSkillReady()
        {
            SetSkillCooldownNormalized(1f);
        }

        /// <summary>
        /// HUD를 표시합니다.
        /// </summary>
        public void Show()
        {
            gameObject.SetActive(true);
        }

        /// <summary>
        /// HUD를 숨깁니다.
        /// </summary>
        public void Hide()
        {
            gameObject.SetActive(false);
        }

        /// <summary>
        /// HUD의 체력 및 스킬 게이지를 초기 상태로 되돌립니다.
        /// </summary>
        public void Clear()
        {
            SetHealthNormalized(0f);
            SetSkillCooldownNormalized(0f);
        }

        #endregion
    }
}