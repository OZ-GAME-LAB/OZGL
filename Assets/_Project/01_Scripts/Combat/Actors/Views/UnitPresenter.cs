using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.UI;
using OzGameLab01.UI.Battle;

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
        private readonly GameObject attackEffectPrefab;
        private readonly GameObject hitEffectPrefab;
        private readonly TMPro.TextMeshPro skillNameLabel;
        private readonly float skillNameDisplayDuration;
        private readonly Unit.Team team;

        // 파티클 기반 1회성 VFX 재생 길이. 소스 프리팹(12. Enemy 세트)이 전부 lengthInSec 2초
        // 이내라 여유를 두고 파괴한다 — VFX마다 정확한 길이를 읽어오는 대신 고정값으로 통일.
        private const float EffectAutoDestroySeconds = 2.5f;

        private Color _originalColor;
        private bool _isColorEffectPlaying;
        private RectTransform _combatAnchor;
        private Image _combatImage;
        private UIProjectilePool _uiProjectilePool;
        private Sprite _projectileSprite;
        private Color _projectileColor = Color.white;
        private AllyUnitCombatHUDView _hud;
        // 상태이상(기절/침묵/도트)별 지속 재생 중인 VFX 인스턴스. 적용 시 생성, 해제 시 파괴.
        private readonly Dictionary<DebuffType, GameObject> _activeStatusEffects = new Dictionary<DebuffType, GameObject>();

        public RectTransform CombatAnchor => _combatAnchor;

        public UnitPresenter(
            HealthBar healthBar,
            SpriteRenderer spriteRenderer,
            GameObject projectilePrefab,
            GameObject attackEffectPrefab,
            GameObject hitEffectPrefab,
            TMPro.TextMeshPro skillNameLabel,
            float skillNameDisplayDuration,
            Unit.Team team)
        {
            this.healthBar = healthBar;
            this.spriteRenderer = spriteRenderer;
            this.projectilePrefab = projectilePrefab;
            this.attackEffectPrefab = attackEffectPrefab;
            this.hitEffectPrefab = hitEffectPrefab;
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

        /// <summary>
        /// UnitAnchor 아래에 스폰된 AllyUnitCombatHUDView를 바인딩합니다(아군 전용, 적은 null).
        /// </summary>
        public void BindHud(AllyUnitCombatHUDView hud)
        {
            _hud = hud;
        }

        /// <summary>
        /// 매 프레임 체력/액티브 스킬 쿨다운 게이지를 갱신합니다. HUD가 바인딩되지 않았으면
        /// (적, 또는 HUD 프리팹 미지정) 아무 것도 하지 않습니다.
        /// </summary>
        public void UpdateHud(float currentHp, float maxHp, bool hasCooldown, float cooldownRemaining, float cooldownDuration)
        {
            if (_hud == null)
            {
                return;
            }

            _hud.SetHealth(currentHp, maxHp);
            if (hasCooldown)
            {
                _hud.SetSkillCooldown(cooldownRemaining, cooldownDuration);
            }
        }

        public void FireProjectile(Unit target, UnitPresenter targetPresenter, Vector3 worldPosition, float damage,
            bool applyDamage = true, Action onImpact = null)
        {
            // UI에 배치된 유닛은 자신의 UnitAnchor에서 대상 UnitAnchor로 풀링 투사체를 발사합니다.
            // 종족별 발사 이펙트는 UI Image로 표현할 수 없어 캐스터 위치의 캐스트 플래시로만 남긴다.
            if (_uiProjectilePool != null && _combatAnchor != null && targetPresenter != null && targetPresenter._combatAnchor != null)
            {
                _uiProjectilePool.Fire(_combatAnchor, targetPresenter._combatAnchor, target, damage, _projectileSprite, _projectileColor, applyDamage, onImpact);
                PlayAttackEffect(worldPosition);
                return;
            }

            if (projectilePrefab == null)
            {
                PlayAttackEffect(worldPosition);
                return;
            }

            GameObject projectileObj = UnityEngine.Object.Instantiate(projectilePrefab, worldPosition, Quaternion.identity);

            if (attackEffectPrefab != null)
            {
                // 종족별 발사 이펙트(화살/총알/쿠키 등)는 트레일이 달린 "날아가는 물체" 아트라
                // 캐스터 위치에 고정해서 재생하면 트레일이 그려지지 않고 그냥 멈춰있는 것처럼
                // 보인다(리포트: 쿠키 이펙트가 적 발밑에 멈춰있고 공용 원형 스프라이트만 날아감).
                // 실제 이동을 담당하는 투사체 오브젝트의 자식으로 붙여 함께 이동시키고,
                // 겹쳐 보이지 않도록 공용 원형 스프라이트는 숨긴다.
                GameObject flightFx = UnityEngine.Object.Instantiate(attackEffectPrefab, projectileObj.transform);
                flightFx.transform.localPosition = Vector3.zero;
                SpriteRenderer genericRenderer = projectileObj.GetComponentInChildren<SpriteRenderer>(true);
                if (genericRenderer != null)
                {
                    genericRenderer.enabled = false;
                }
            }
            else
            {
                SpriteRenderer projectileRenderer = projectileObj.GetComponentInChildren<SpriteRenderer>(true);
                if (projectileRenderer != null)
                {
                    if (_projectileSprite != null)
                    {
                        projectileRenderer.sprite = _projectileSprite;
                    }

                    projectileRenderer.color = _projectileColor;
                }
            }

            Projectile projectile = projectileObj.GetComponent<Projectile>();
            if (projectile != null)
            {
                projectile.Init(target, damage, applyDamage, onImpact);
            }
        }

        /// <summary>
        /// 이 유닛이 공격/스킬을 시전한 자기 위치에서 재생하는 1회성 VFX. 프리팹이 없으면
        /// 아무 것도 하지 않습니다(아군은 애니메이터 기반 연출을 쓰므로 보통 비워둡니다).
        /// </summary>
        public void PlayAttackEffect(Vector3 worldPosition)
        {
            if (attackEffectPrefab == null) return;
            GameObject fx = UnityEngine.Object.Instantiate(attackEffectPrefab, worldPosition, Quaternion.identity);
            DestroySafely(fx, EffectAutoDestroySeconds);
        }

        /// <summary>피격당한 자기 위치에서 재생하는 1회성 VFX. HitFlash(색상 점멸)와 별개로 더해진다.</summary>
        public void PlayHitEffect(Vector3 worldPosition)
        {
            if (hitEffectPrefab == null) return;
            GameObject fx = UnityEngine.Object.Instantiate(hitEffectPrefab, worldPosition, Quaternion.identity);
            DestroySafely(fx, EffectAutoDestroySeconds);
        }

        /// <summary>
        /// CombatVfxLibrary에서 가져온 힐/스탯 버프·디버프 1회성 VFX 재생. prefab이 null이면
        /// (라이브러리 미배치, 해당 스탯에 대응하는 VFX 없음 등) 조용히 무시한다.
        /// </summary>
        public void PlayEffect(GameObject prefab, Vector3 worldPosition)
        {
            if (prefab == null) return;
            GameObject fx = UnityEngine.Object.Instantiate(prefab, worldPosition, Quaternion.identity);
            DestroySafely(fx, EffectAutoDestroySeconds);
        }

        /// <summary>
        /// 상태이상 VFX를 켜거나 끈다. active=true면 CombatVfxLibrary에서 대응하는 프리팹을 찾아
        /// parent 아래에 생성해 유닛을 따라다니게 하고, active=false면 앞서 생성한 인스턴스를
        /// 파괴한다. 같은 타입이 이미 재생 중이면 중복 생성하지 않는다.
        /// </summary>
        public void SetStatusEffectActive(DebuffType type, bool active, Transform parent)
        {
            if (active)
            {
                if (_activeStatusEffects.ContainsKey(type)) return;
                GameObject prefab = CombatVfxLibrary.Instance?.GetDebuffEffect(type);
                if (prefab == null) return;
                GameObject fx = UnityEngine.Object.Instantiate(prefab, parent);
                fx.transform.localPosition = Vector3.zero;
                _activeStatusEffects[type] = fx;
            }
            else if (_activeStatusEffects.TryGetValue(type, out GameObject fx))
            {
                DestroySafely(fx);
                _activeStatusEffects.Remove(type);
            }
        }

        /// <summary>현재 재생 중인 모든 상태이상 VFX를 즉시 정리한다(CleanseDebuffs 등 전체 해제 시).</summary>
        public void ClearAllStatusEffects()
        {
            foreach (KeyValuePair<DebuffType, GameObject> pair in _activeStatusEffects)
            {
                DestroySafely(pair.Value);
            }
            _activeStatusEffects.Clear();
        }

        /// <summary>
        /// Object.Destroy는 에디트 모드에서 호출하면 에러를 던진다(EditMode 테스트가 Heal/
        /// ApplyStatEffect/ApplyDebuff를 호출할 때 걸리는 경우). 플레이 중이 아니면 지연 없이
        /// DestroyImmediate로 정리한다 — 에디트 모드에서는 "잠시 후 사라짐" 자체가 의미 없다.
        /// </summary>
        private static void DestroySafely(GameObject fx, float delaySeconds = 0f)
        {
            if (fx == null) return;
            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(fx, delaySeconds);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(fx);
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
