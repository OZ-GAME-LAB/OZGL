using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace OzGameLab01.Combat
{
    /// <summary>
    /// Unit에서 분리된 시각 표현(체력바, 색상 연출, 투사체 UI/월드 표시) 책임을 담당합니다.
    /// Inspector 참조는 Unit이 그대로 들고 있고, 이 클래스는 그 값을 생성자로 전달받아
    /// 사용하는 순수 C# 클래스입니다(씬/프리팹 재배선 불필요). 코루틴은 MonoBehaviour인
    /// Unit이 시작시키고, 이 클래스는 IEnumerator 본문만 제공합니다.
    /// </summary>
    public sealed class UnitPresenter
    {
        private readonly HealthBar healthBar;
        private readonly SpriteRenderer spriteRenderer;
        private readonly GameObject projectilePrefab;
        private readonly Color skillGlowColor;
        private readonly float skillGlowDuration;
        private readonly Unit.Team team;

        private Color _originalColor;
        private RectTransform _combatAnchor;
        private Image _combatImage;
        private UIProjectilePool _uiProjectilePool;
        private Sprite _projectileSprite;
        private Color _projectileColor = Color.white;

        public RectTransform CombatAnchor => _combatAnchor;

        public UnitPresenter(
            HealthBar healthBar,
            SpriteRenderer spriteRenderer,
            GameObject projectilePrefab,
            Color skillGlowColor,
            float skillGlowDuration,
            Unit.Team team)
        {
            this.healthBar = healthBar;
            this.spriteRenderer = spriteRenderer;
            this.projectilePrefab = projectilePrefab;
            this.skillGlowColor = skillGlowColor;
            this.skillGlowDuration = skillGlowDuration;
            this.team = team;
        }

        public void InitHealthBar(float maxHP)
        {
            if (healthBar != null)
            {
                healthBar.Init(maxHP);
            }
        }

        public void SetHP(float currentHP)
        {
            if (healthBar != null)
            {
                healthBar.SetHP(currentHP);
            }
        }

        public void CaptureOriginalColor()
        {
            if (spriteRenderer != null)
            {
                _originalColor = spriteRenderer.color;
            }
        }

        /// <summary>
        /// 스프라이트, 이름표, 체력바 등 화면 표시를 전환합니다.
        /// 전투 로직(타겟팅, 데미지, 레지스트리)은 그대로 유지한 채 화면 표시만 끕니다.
        /// </summary>
        public void SetVisualsVisible(bool visible, GameObject root)
        {
            foreach (Renderer childRenderer in root.GetComponentsInChildren<Renderer>(true))
            {
                childRenderer.enabled = visible;
            }

            if (healthBar != null)
            {
                healthBar.gameObject.SetActive(visible);
            }
        }

        /// <summary>
        /// UnitAnchor의 UI 이미지와 투사체 풀을 실제 Unit에 바인딩합니다.
        /// </summary>
        public void BindCombatUI(RectTransform combatAnchor, Image combatImage, UIProjectilePool projectilePool)
        {
            _combatAnchor = combatAnchor;
            _combatImage = combatImage;
            _uiProjectilePool = projectilePool;

            if (projectilePrefab != null)
            {
                SpriteRenderer projectileRenderer = projectilePrefab.GetComponentInChildren<SpriteRenderer>(true);
                if (projectileRenderer != null)
                {
                    _projectileSprite = projectileRenderer.sprite;
                    _projectileColor = projectileRenderer.color;
                }
            }

            // Enemy의 공격은 같은 풀을 사용하되 빨간색으로 표시
            if (team == Unit.Team.Enemy)
            {
                _projectileColor = Color.red;
            }
        }

        public void FireProjectile(Unit target, UnitPresenter targetPresenter, Vector3 worldPosition, float damage)
        {
            // UI에 배치된 유닛은 자신의 UnitAnchor에서 대상 UnitAnchor로 풀링 투사체를 발사합니다.
            if (_uiProjectilePool != null && _combatAnchor != null && targetPresenter != null && targetPresenter._combatAnchor != null)
            {
                _uiProjectilePool.Fire(_combatAnchor, targetPresenter._combatAnchor, target, damage, _projectileSprite, _projectileColor);
                return;
            }

            if (projectilePrefab == null)
            {
                return;
            }

            GameObject projectileObj = Object.Instantiate(projectilePrefab, worldPosition, Quaternion.identity);
            Projectile projectile = projectileObj.GetComponent<Projectile>();
            if (projectile != null)
            {
                projectile.Init(target, damage);
            }
        }

        public void HideCombatImage()
        {
            if (_combatImage != null)
            {
                _combatImage.enabled = false;
            }
        }

        public IEnumerator SkillGlow()
        {
            if (spriteRenderer == null)
            {
                yield break;
            }

            float half = skillGlowDuration / 2f;
            float t = 0f;

            while (t < half)
            {
                t += Time.deltaTime;
                spriteRenderer.color = Color.Lerp(_originalColor, skillGlowColor, t / half);
                yield return null;
            }

            t = 0f;
            while (t < half)
            {
                t += Time.deltaTime;
                spriteRenderer.color = Color.Lerp(skillGlowColor, _originalColor, t / half);
                yield return null;
            }

            spriteRenderer.color = _originalColor;
        }

        public IEnumerator HitFlash()
        {
            if (spriteRenderer == null)
            {
                yield break;
            }

            for (int i = 0; i < 3; i++)
            {
                spriteRenderer.color = Color.white;
                yield return new WaitForSeconds(0.05f);
                spriteRenderer.color = _originalColor;
                yield return new WaitForSeconds(0.05f);
            }
        }
    }
}
