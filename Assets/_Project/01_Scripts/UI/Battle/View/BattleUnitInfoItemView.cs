using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OzGameLab01.UI.Battle
{
    /// <summary>
    /// 전투 유닛 정보 UI View입니다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BattleUnitInfoItemView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Image portraitImage;
        [SerializeField] private TMP_Text unitNameText;

        [Header("Skill")]
        [SerializeField] private Image skillIconImage;
        [SerializeField] private Transform skillContentRoot;

        [Header("Status Effect")]
        [SerializeField] private Transform statusEffectContentRoot;

        [Header("State")]
        [SerializeField] private GameObject gravePortraitObject;

        #region Properties

        /// <summary>
        /// 유닛 초상화 Image입니다.
        /// </summary>
        public Image PortraitImage => portraitImage;

        /// <summary>
        /// 유닛 이름 Text입니다.
        /// </summary>
        public TMP_Text UnitNameText => unitNameText;

        /// <summary>
        /// 표시 중인 스킬 아이콘 Image입니다.
        /// </summary>
        public Image SkillIconImage => skillIconImage;

        /// <summary>
        /// 스킬 UI가 배치되는 부모 Transform입니다.
        /// </summary>
        public Transform SkillContentRoot => skillContentRoot;

        /// <summary>
        /// 상태이상 UI가 배치되는 부모 Transform입니다.
        /// </summary>
        public Transform StatusEffectContentRoot => statusEffectContentRoot;

        /// <summary>
        /// 현재 View의 활성 상태입니다.
        /// </summary>
        public bool IsVisible => gameObject.activeSelf;

        #endregion

        #region API

        /// <summary>
        /// 유닛 정보 View를 표시합니다.
        /// </summary>
        public void Show()
        {
            gameObject.SetActive(true);
        }

        /// <summary>
        /// 유닛 정보 View를 숨깁니다.
        /// </summary>
        public void Hide()
        {
            gameObject.SetActive(false);
        }

        /// <summary>
        /// 유닛 초상화를 설정합니다.
        /// </summary>
        /// <param name="sprite">표시할 초상화 Sprite입니다.</param>
        public void SetPortrait(Sprite sprite)
        {
            if (portraitImage == null)
            {
                return;
            }

            portraitImage.sprite = sprite;
            portraitImage.enabled = sprite != null;
        }

        /// <summary>
        /// 유닛 이름을 설정합니다.
        /// </summary>
        /// <param name="value">표시할 유닛 이름입니다.</param>
        public void SetUnitName(string value)
        {
            if (unitNameText != null)
            {
                unitNameText.text = value ?? string.Empty;
            }
        }

        /// <summary>
        /// 스킬 아이콘을 설정합니다.
        /// </summary>
        /// <param name="sprite">표시할 스킬 아이콘 Sprite입니다.</param>
        public void SetSkillIcon(Sprite sprite)
        {
            if (skillIconImage == null)
            {
                return;
            }

            skillIconImage.sprite = sprite;
            skillIconImage.enabled = sprite != null;
        }

        /// <summary>
        /// 스킬 영역의 표시 여부를 설정합니다.
        /// </summary>
        /// <param name="value">표시 여부입니다.</param>
        public void SetSkillVisible(bool value)
        {
            if (skillContentRoot != null)
            {
                skillContentRoot.gameObject.SetActive(value);
            }
        }

        /// <summary>
        /// 상태이상 영역의 표시 여부를 설정합니다.
        /// </summary>
        /// <param name="value">표시 여부입니다.</param>
        public void SetStatusEffectVisible(bool value)
        {
            if (statusEffectContentRoot != null)
            {
                statusEffectContentRoot.gameObject.SetActive(value);
            }
        }

        /// <summary>
        /// 사망 상태 표시의 활성 여부를 설정합니다.
        /// </summary>
        /// <param name="value">사망 상태 표시 여부입니다.</param>
        public void SetGraveVisible(bool value)
        {
            if (gravePortraitObject != null)
            {
                gravePortraitObject.SetActive(value);
            }
        }

        #endregion
    }
}