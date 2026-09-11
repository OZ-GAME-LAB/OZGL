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
        private readonly TMPro.TextMeshPro skillNameLabel;
        private readonly float skillNameDisplayDuration;
        private readonly Unit.Team team;

        private Color _originalColor;
        private bool _isColorEffectPlaying;
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
            TMPro.TextMeshPro skillNameLabel,
            float skillNameDisplayDuration,
            Unit.Team team)
        {
            this.healthBar = healthBar;
            this.spriteRenderer = spriteRenderer;
            this.projectilePrefab = projectilePrefab;
            this.skillNameLabel = skillNameLabel;
            this.skillNameDisplayDuration = skillNameDisplayDuration;
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

        /// <summary>
        /// 시전자 머리 위 라벨에 스킬 이름을 잠깐 띄웁니다. 스킬 발동을 화면에 알리는 연출로,
        /// 예전엔 스프라이트 색을 깜빡이는 방식(SkillGlow)이었으나 텍스트 표시로 대체되었습니다.
        /// </summary>
        public IEnumerator ShowSkillCastText(string skillName)
        {
            if (skillNameLabel == null)
            {
                yield break;
            }

            skillNameLabel.text = skillName;
            yield return new WaitForSeconds(skillNameDisplayDuration);
            skillNameLabel.text = string.Empty;
        }

        public IEnumerator HitFlash()
        {
            if (spriteRenderer == null)
            {
                yield break;
            }

            _isColorEffectPlaying = true;

            for (int i = 0; i < 3; i++)
            {
                spriteRenderer.color = Color.white;
                yield return new WaitForSeconds(0.05f);
                spriteRenderer.color = _originalColor;
                yield return new WaitForSeconds(0.05f);
            }

            _isColorEffectPlaying = false;
        }

        /// <summary>
        /// 활성 디버프를 나타내는 플레이스홀더 틴트. HitFlash가 진행 중일 때는
        /// 색상 채널을 두고 다투지 않도록 무시한다(짧게 끝나므로 다음 프레임에 다시 반영됨).
        /// </summary>
        public void SetDebuffTint(Color? tint)
        {
            if (spriteRenderer == null || _isColorEffectPlaying)
            {
                return;
            }

            spriteRenderer.color = tint ?? _originalColor;
        }
    }
}
