using UnityEngine;
using System;

namespace OzGameLab01.Combat
{
    public class Projectile : MonoBehaviour
    {
        private Unit _target;
        private float _damage;
        private float _speed;
        private bool _applyDamage;
        private Action _onImpact;

        public void Init(Unit target, float damage, bool applyDamage = true, Action onImpact = null, float speed = 8f)
        {
            _target = target;
            _damage = damage;
            _applyDamage = applyDamage;
            _onImpact = onImpact;
            _speed = speed;
        }

        private void Update()
        {
            if (_target == null || _target.IsDead)
            {
                Destroy(gameObject);
                return;
            }

            Vector3 direction = (_target.transform.position - transform.position).normalized;
            transform.position += direction * _speed * Time.deltaTime;

            float distance = Vector3.Distance(transform.position, _target.transform.position);
            if (distance <= 0.1f)
            {
                if (_applyDamage) _target.TakeDamage(_damage);
                Action onImpact = _onImpact;
                _onImpact = null;
                onImpact?.Invoke();
                Destroy(gameObject);
            }
        }
    }
}
