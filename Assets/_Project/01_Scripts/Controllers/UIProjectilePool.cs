using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace OzGameLab01.Combat
{
    /// <summary>
    /// Screen Space Overlay 전투 UI에서 투사체 Image를 재사용하는 간단한 오브젝트 풀입니다.
    /// </summary>
    public sealed class UIProjectilePool : MonoBehaviour
    {
        private readonly Queue<UIProjectile> _available = new Queue<UIProjectile>();

        public void Fire(RectTransform origin, RectTransform target, Unit targetUnit, float damage, Sprite sprite, Color color)
        {
            if (origin == null || target == null || targetUnit == null)
            {
                return;
            }

            UIProjectile projectile = _available.Count > 0 ? _available.Dequeue() : CreateProjectile();
            projectile.Launch(this, origin, target, targetUnit, damage, sprite, color);
        }

        internal void Release(UIProjectile projectile)
        {
            projectile.gameObject.SetActive(false);
            projectile.transform.SetParent(transform, false);
            _available.Enqueue(projectile);
        }

        private UIProjectile CreateProjectile()
        {
            GameObject projectileObject = new GameObject(
                "PooledUIProjectile",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(UIProjectile));

            projectileObject.transform.SetParent(transform, false);
            return projectileObject.GetComponent<UIProjectile>();
        }
    }

    /// <summary>
    /// RectTransform 앵커 사이를 이동하고 명중 뒤 풀로 돌아가는 UI 투사체입니다.
    /// </summary>
    public sealed class UIProjectile : MonoBehaviour
    {
        private const float Speed = 900f;
        private UIProjectilePool _pool;
        private RectTransform _targetAnchor;
        private Unit _targetUnit;
        private float _damage;
        private RectTransform _rectTransform;

        public void Launch(UIProjectilePool pool, RectTransform origin, RectTransform target, Unit targetUnit, float damage, Sprite sprite, Color color)
        {
            _pool = pool;
            _targetAnchor = target;
            _targetUnit = targetUnit;
            _damage = damage;
            _rectTransform = (RectTransform)transform;

            transform.SetParent(pool.transform, false);
            // 세로형 UnitAnchor 사각형의 중앙을 실제 캐릭터 몸통/발사 위치로 사용
            _rectTransform.position = GetAnchorCenter(origin);
            _rectTransform.sizeDelta = new Vector2(36f, 36f);

            Image image = GetComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.preserveAspect = true;
            image.raycastTarget = false;
            gameObject.SetActive(true);
        }

        private void Update()
        {
            if (_targetAnchor == null || _targetUnit == null || _targetUnit.IsDead)
            {
                _pool.Release(this);
                return;
            }

            Vector3 targetPosition = GetAnchorCenter(_targetAnchor);
            _rectTransform.position = Vector3.MoveTowards(
                _rectTransform.position,
                targetPosition,
                Speed * Time.deltaTime);

            if (Vector3.Distance(_rectTransform.position, targetPosition) <= 8f)
            {
                _targetUnit.TakeDamage(_damage);
                _pool.Release(this);
            }
        }

        // 피벗 위치와 무관하게 RectTransform 표시 영역의 월드 중앙 좌표를 반환합니다.
        private static Vector3 GetAnchorCenter(RectTransform anchor)
        {
            return anchor.TransformPoint(anchor.rect.center);
        }
    }
}
