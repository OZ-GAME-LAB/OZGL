using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OzGameLab01.UI.Battle
{
    /// <summary>
    /// 적 이름, 상태이상, 체력 UI를 관리하는 View입니다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BattleEnemyHeaderView : MonoBehaviour
    {
        [SerializeField] private TMP_Text enemyNameText;
        [SerializeField] private Transform statusEffectRoot;
        [SerializeField] private Image healthFill;


        #region Properties

        /// <summary>
        /// 상태이상 UI가 배치될 부모 Transform입니다.
        /// </summary>
        public Transform StatusEffectRoot => statusEffectRoot;

        /// <summary>
        /// 현재 View의 활성 상태입니다.
        /// </summary>
        public bool IsVisible => gameObject.activeSelf;

        #endregion


        #region Public API

        /// <summary>
        /// 적의 표시 이름을 설정합니다.
        /// </summary>
        /// <param name="enemyName">표시할 적 이름입니다.</param>
        public void SetEnemyName(string enemyName)
        {
            if (enemyNameText != null)
            {
                enemyNameText.text = enemyName ?? string.Empty;
            }
        }

        /// <summary>
        /// 현재 체력과 최대 체력을 이용해 체력 게이지를 갱신합니다.
        /// </summary>
        /// <param name="current">현재 체력입니다.</param>
        /// <param name="max">최대 체력입니다.</param>
        public void SetHealth(float current, float max)
        {
            float normalized = max > 0f ? Mathf.Clamp01(current / max) : 0f;

            SetHealthNormalized(normalized);
        }

        /// <summary>
        /// 정규화된 체력 값을 설정합니다.
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
        /// 적 이름과 체력 표시를 초기화합니다.
        /// </summary>
        public void Clear()
        {
            if (enemyNameText != null)
            {
                enemyNameText.text = string.Empty;
            }

            if (healthFill != null)
            {
                healthFill.fillAmount = 0f;
            }
        }

        /// <summary>
        /// Enemy Header를 표시합니다.
        /// </summary>
        public void Show()
        {
            gameObject.SetActive(true);
        }

        /// <summary>
        /// Enemy Header를 숨깁니다.
        /// </summary>
        public void Hide()
        {
            gameObject.SetActive(false);
        }

        #endregion
    }
}