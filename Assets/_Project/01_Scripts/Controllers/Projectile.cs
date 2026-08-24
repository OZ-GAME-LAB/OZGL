using UnityEngine;

namespace OzGameLab01.Controllers
{
    public class Projectile : MonoBehaviour
    {
        private Unit _target;
        private float _damage;
        private float _speed;

        public void Init(Unit target, float damage, float speed = 8f)
        {
            _target = target;
            _damage = damage;
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
                _target.TakeDamage(_damage);
                Destroy(gameObject);
            }
        }
    }
}
