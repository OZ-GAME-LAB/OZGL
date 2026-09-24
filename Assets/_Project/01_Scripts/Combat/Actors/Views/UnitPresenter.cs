using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.UI;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
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
        private readonly AssetReferenceGameObject projectilePrefabReference;

        // 파티클 기반 1회성 VFX 재생 길이. 소스 프리팹(12. Enemy 세트)이 전부 lengthInSec 2초
        // 이내라 여유를 두고 파괴한다 — VFX마다 정확한 길이를 읽어오는 대신 고정값으로 통일.
        private const float EffectAutoDestroySeconds = 2.5f;

        private Color _originalColor;
        private bool _isColorEffectPlaying;
        private RectTransform _combatAnchor;
        private Image _combatImage;
        private UIProjectilePool _uiProjectilePool;
        private AllyUnitCombatHUDView _hud;
        // 상태이상(기절/침묵/도트)별 지속 재생 중인 VFX 인스턴스. 적용 시 생성, 해제 시 파괴.
        private readonly Dictionary<DebuffType, GameObject> _activeStatusEffects = new Dictionary<DebuffType, GameObject>();

        public RectTransform CombatAnchor => _combatAnchor;

        public UnitPresenter(
            HealthBar healthBar,
            SpriteRenderer spriteRenderer,
            AssetReferenceGameObject projectilePrefabReference)
        {
            this.healthBar = healthBar;
            this.spriteRenderer = spriteRenderer;
            this.projectilePrefabReference = projectilePrefabReference;
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
            bool applyDamage, Action onImpact)
        {
            // 월드와 UI 모두 동일한 Addressable 프리팹을 생성하고 표시 방식만 Projectile이 선택합니다.
            if (projectilePrefabReference == null || !projectilePrefabReference.RuntimeKeyIsValid())
            {
                Debug.LogError("[UnitPresenter] Addressable 투사체 프리팹 주소가 비어 있습니다.");
                ResolveProjectileFailure(target, damage, applyDamage, onImpact);
                return;
            }

            AsyncOperationHandle<GameObject> handle = Addressables.InstantiateAsync(
                projectilePrefabReference, worldPosition, Quaternion.identity);
            handle.Completed += operation =>
            {
                if (operation.Status != AsyncOperationStatus.Succeeded || operation.Result == null)
                {
                    Debug.LogError($"[UnitPresenter] Addressable 투사체 생성 실패: {projectilePrefabReference.RuntimeKey}");
                    if (operation.IsValid()) Addressables.Release(operation);
                    ResolveProjectileFailure(target, damage, applyDamage, onImpact);
                    return;
                }

                GameObject projectileObject = operation.Result;
                Projectile projectile = projectileObject.GetComponent<Projectile>();
                if (projectile == null)
                {
                    Debug.LogError("[UnitPresenter] 투사체 프리팹에 Projectile 컴포넌트가 없습니다.", projectileObject);
                    Addressables.ReleaseInstance(projectileObject);
                    ResolveProjectileFailure(target, damage, applyDamage, onImpact);
                    return;
                }

                projectile.MarkAddressableInstance();
                if (_uiProjectilePool != null && _combatAnchor != null && targetPresenter?._combatAnchor != null)
                {
                    projectile.InitUi(_uiProjectilePool.transform, _combatAnchor, targetPresenter._combatAnchor,
                        target, damage, applyDamage, onImpact, isBasicAttack: true);
                }
                else
                {
                    projectile.Init(target, damage, applyDamage, onImpact, isBasicAttack: true);
                }
            };
        }

        private static void ResolveProjectileFailure(Unit target, float damage, bool applyDamage, Action onImpact)
        {
            if (target == null || target.IsDead) return;
            if (applyDamage) target.TakeDamage(damage, isBasicAttack: true);
            onImpact?.Invoke();
        }

        /// <summary>
        /// 스킬 발동 시 시전자 위치에서 재생하는 1회성 VFX. 주소는 SkillData.castVfxAddress(Addressables)이며
        /// 비어 있으면 아무 것도 하지 않습니다.
        /// </summary>
        public void PlaySkillCastEffect(string address, Vector3 worldPosition)
        {
            if (string.IsNullOrEmpty(address)) return;
            Addressables.InstantiateAsync(address, worldPosition, Quaternion.identity).Completed += operation =>
            {
                if (operation.Status != AsyncOperationStatus.Succeeded || operation.Result == null)
                {
                    if (operation.IsValid()) Addressables.Release(operation);
                    Debug.LogWarning($"[UnitPresenter] 스킬 시전 VFX 로드 실패: {address}");
                    return;
                }

                operation.Result.AddComponent<AddressableVfxLifetime>().Initialize(EffectAutoDestroySeconds);
            };
        }

        // 스킬 연출 VFX의 기본 월드 스케일(기본공격 피격 VFX와 같은 기준).
        private const float DefaultSkillVfxScale = 0.25f;

        /// <summary>
        /// 스킬 효과 연출 VFX를 target 위치에 재생합니다. attachSeconds가 0보다 크면 target에 붙여
        /// 그 시간 동안 유지하고(루프 VFX), 아니면 그 자리에 1회성으로 재생합니다.
        /// </summary>
        public void PlaySkillVfx(string address, Transform target, float scale, float attachSeconds)
        {
            if (string.IsNullOrEmpty(address) || target == null) return;
            bool attach = attachSeconds > 0f;
            Addressables.InstantiateAsync(address, target.position, Quaternion.identity, attach ? target : null).Completed += operation =>
            {
                if (operation.Status != AsyncOperationStatus.Succeeded || operation.Result == null)
                {
                    if (operation.IsValid()) Addressables.Release(operation);
                    Debug.LogWarning($"[UnitPresenter] 스킬 VFX 로드 실패: {address}");
                    return;
                }

                GameObject fx = operation.Result;
                if (target != null) fx.transform.position = target.position;
                fx.transform.localScale = Vector3.one * (scale > 0f ? scale : DefaultSkillVfxScale);
                fx.AddComponent<AddressableVfxLifetime>().Initialize(attach ? attachSeconds : EffectAutoDestroySeconds);
            };
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
