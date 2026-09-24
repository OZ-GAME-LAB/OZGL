using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

namespace OzGameLab01.Combat
{
    /// <summary>
    /// Addressable Sprite를 직접 소유하는 기본 공격 투사체입니다.
    /// 월드 전투와 Screen Space UI 전투가 같은 프리팹/이동 로직을 사용합니다.
    /// </summary>
    public sealed class Projectile : MonoBehaviour
    {
        [SerializeField] private AssetReferenceSprite spriteReference;
        [SerializeField] private AssetReferenceGameObject travelEffectReference;
        [SerializeField] private AssetReferenceGameObject impactEffectReference;
        [SerializeField] private SpriteRenderer worldRenderer;
        [SerializeField] private Image uiRenderer;
        [SerializeField] private float worldSpeed = 8f;
        [SerializeField] private float uiSpeed = 900f;
        [SerializeField] private float worldScale = 0.25f;
        [SerializeField] private Vector2 uiSize = new Vector2(36f, 36f);
        [SerializeField] private float worldTravelEffectScale = 1f;
        [SerializeField] private float uiTravelEffectScale = 12f;
        [SerializeField] private float worldImpactEffectScale = 0.25f;
        [SerializeField] private float uiImpactEffectScale = 12f;
        [SerializeField] private float impactEffectLifetime = 2.5f;

        private Unit _target;
        private RectTransform _targetAnchor;
        private RectTransform _rectTransform;
        private float _damage;
        private bool _applyDamage;
        private Action _onImpact;
        private bool _isBasicAttack = true;
        private bool _isUiProjectile;
        private bool _launched;
        private bool _addressableInstance;
        private bool _releasing;
        private AsyncOperationHandle<Sprite> _spriteHandle;
        private bool _ownsSpriteHandle;
        private GameObject _travelEffectInstance;

        public AssetReferenceSprite SpriteReference => spriteReference;
        public AssetReferenceGameObject TravelEffectReference => travelEffectReference;
        public AssetReferenceGameObject ImpactEffectReference => impactEffectReference;
        public bool IsVisualReady => _launched;

        private void Awake()
        {
            if (worldRenderer == null) worldRenderer = GetComponentInChildren<SpriteRenderer>(true);
            if (uiRenderer == null) uiRenderer = GetComponentInChildren<Image>(true);
            SetVisualEnabled(false);
        }

        public void MarkAddressableInstance()
        {
            _addressableInstance = true;
        }

        public void Init(Unit target, float damage, bool applyDamage = true, Action onImpact = null,
            float speed = -1f, bool isBasicAttack = true)
        {
            Configure(target, damage, applyDamage, onImpact, isBasicAttack);
            _isUiProjectile = false;
            _targetAnchor = null;
            if (speed > 0f) worldSpeed = speed;
            transform.localScale = Vector3.one * worldScale;
            StartCoroutine(LoadVisualAndLaunch());
        }

        public void InitUi(Transform parent, RectTransform origin, RectTransform targetAnchor, Unit target,
            float damage, bool applyDamage = true, Action onImpact = null, bool isBasicAttack = true)
        {
            Configure(target, damage, applyDamage, onImpact, isBasicAttack);
            _isUiProjectile = true;
            _targetAnchor = targetAnchor;
            transform.SetParent(parent, false);
            _rectTransform = transform as RectTransform;
            if (_rectTransform == null)
            {
                Debug.LogError("[Projectile] UI 투사체 프리팹의 루트에는 RectTransform이 필요합니다.", this);
                ResolveWithoutVisual();
                return;
            }

            _rectTransform.position = GetAnchorCenter(origin);
            _rectTransform.sizeDelta = uiSize;
            _rectTransform.localScale = Vector3.one;
            StartCoroutine(LoadVisualAndLaunch());
        }

        private void Configure(Unit target, float damage, bool applyDamage, Action onImpact, bool isBasicAttack)
        {
            _target = target;
            _damage = damage;
            _applyDamage = applyDamage;
            _onImpact = onImpact;
            _isBasicAttack = isBasicAttack;
        }

        private IEnumerator LoadVisualAndLaunch()
        {
            if (spriteReference == null || !spriteReference.RuntimeKeyIsValid())
            {
                Debug.LogError("[Projectile] Addressable Sprite 주소가 비어 있습니다.", this);
                ResolveWithoutVisual();
                yield break;
            }

            _spriteHandle = spriteReference.LoadAssetAsync<Sprite>();
            _ownsSpriteHandle = true;
            yield return _spriteHandle;

            if (_spriteHandle.Status != AsyncOperationStatus.Succeeded || _spriteHandle.Result == null)
            {
                Debug.LogError($"[Projectile] Sprite 로드 실패: {spriteReference.RuntimeKey}", this);
                ResolveWithoutVisual();
                yield break;
            }

            if (_isUiProjectile)
            {
                if (uiRenderer == null)
                {
                    Debug.LogError("[Projectile] UI Image가 없습니다.", this);
                    ResolveWithoutVisual();
                    yield break;
                }

                uiRenderer.sprite = _spriteHandle.Result;
                uiRenderer.color = Color.white;
                uiRenderer.preserveAspect = true;
                uiRenderer.raycastTarget = false;
            }
            else
            {
                if (worldRenderer == null)
                {
                    Debug.LogError("[Projectile] SpriteRenderer가 없습니다.", this);
                    ResolveWithoutVisual();
                    yield break;
                }

                worldRenderer.sprite = _spriteHandle.Result;
                worldRenderer.color = Color.white;
            }

            SetVisualEnabled(true);
            InstantiateTravelEffect();
            _launched = true;
        }

        private void Update()
        {
            if (!_launched) return;
            if (_target == null || _target.IsDead || (_isUiProjectile && _targetAnchor == null))
            {
                _onImpact = null;
                ReleaseSelf();
                return;
            }

            Vector3 targetPosition = _isUiProjectile
                ? GetAnchorCenter(_targetAnchor)
                : _target.transform.position;
            float speed = _isUiProjectile ? uiSpeed : worldSpeed;
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, speed * Time.deltaTime);

            float threshold = _isUiProjectile ? 8f : 0.1f;
            if (Vector3.Distance(transform.position, targetPosition) <= threshold)
            {
                InstantiateImpactEffect();
                ResolveImpact();
                ReleaseSelf();
            }
        }

        private void InstantiateTravelEffect()
        {
            if (travelEffectReference == null || !travelEffectReference.RuntimeKeyIsValid()) return;

            AsyncOperationHandle<GameObject> handle = Addressables.InstantiateAsync(
                travelEffectReference, transform.position, Quaternion.identity, transform);
            handle.Completed += operation =>
            {
                if (operation.Status != AsyncOperationStatus.Succeeded || operation.Result == null)
                {
                    if (operation.IsValid()) Addressables.Release(operation);
                    Debug.LogWarning($"[Projectile] Travel VFX load failed: {travelEffectReference.RuntimeKey}", this);
                    return;
                }

                if (_releasing || this == null)
                {
                    Addressables.ReleaseInstance(operation.Result);
                    return;
                }

                _travelEffectInstance = operation.Result;
                Transform effectTransform = _travelEffectInstance.transform;
                effectTransform.SetParent(transform, false);
                effectTransform.localPosition = Vector3.zero;
                effectTransform.localRotation = Quaternion.identity;
                effectTransform.localScale = Vector3.one * (_isUiProjectile
                    ? uiTravelEffectScale
                    : worldTravelEffectScale);
            };
        }

        private void InstantiateImpactEffect()
        {
            if (impactEffectReference == null || !impactEffectReference.RuntimeKeyIsValid()) return;

            bool isUiEffect = _isUiProjectile;
            Transform parent = isUiEffect ? transform.parent : null;
            Vector3 position = transform.position;
            float scale = isUiEffect ? uiImpactEffectScale : worldImpactEffectScale;
            float lifetime = impactEffectLifetime;
            AssetReferenceGameObject reference = impactEffectReference;
            AsyncOperationHandle<GameObject> handle = Addressables.InstantiateAsync(
                reference, position, Quaternion.identity, parent);
            handle.Completed += operation =>
            {
                if (operation.Status != AsyncOperationStatus.Succeeded || operation.Result == null)
                {
                    if (operation.IsValid()) Addressables.Release(operation);
                    Debug.LogWarning($"[Projectile] Impact VFX load failed: {reference.RuntimeKey}");
                    return;
                }

                GameObject effect = operation.Result;
                effect.transform.position = position;
                effect.transform.localScale = Vector3.one * scale;
                effect.AddComponent<AddressableVfxLifetime>().Initialize(lifetime);
            };
        }

        private void ResolveWithoutVisual()
        {
            if (_target != null && !_target.IsDead) ResolveImpact();
            ReleaseSelf();
        }

        private void ResolveImpact()
        {
            if (_applyDamage) _target.TakeDamage(_damage, _isBasicAttack);
            Action onImpact = _onImpact;
            _onImpact = null;
            onImpact?.Invoke();
        }

        private void SetVisualEnabled(bool enabled)
        {
            if (worldRenderer != null) worldRenderer.enabled = enabled && !_isUiProjectile;
            if (uiRenderer != null) uiRenderer.enabled = enabled && _isUiProjectile;
        }

        private void ReleaseSelf()
        {
            if (_releasing) return;
            _releasing = true;
            if (_addressableInstance && Addressables.ReleaseInstance(gameObject)) return;
            Destroy(gameObject);
        }

        private void OnDestroy()
        {
            if (_travelEffectInstance != null) Addressables.ReleaseInstance(_travelEffectInstance);
            if (_ownsSpriteHandle && _spriteHandle.IsValid()) Addressables.Release(_spriteHandle);
        }

        private static Vector3 GetAnchorCenter(RectTransform anchor)
        {
            return anchor != null ? anchor.TransformPoint(anchor.rect.center) : Vector3.zero;
        }
    }
}
