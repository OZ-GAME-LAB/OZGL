using UnityEngine;

namespace OzGameLab01.Combat
{
    /// <summary>
    /// 월드 좌표의 대상을 추적하고 도착 시 피해를 적용합니다.
    /// </summary>
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

            // 배속 및 낮은 프레임 환경의 목표 지점 초과 이동 방지
            transform.position = Vector3.MoveTowards(
                transform.position, _target.transform.position, _speed * Time.deltaTime);

            float distance = Vector3.Distance(transform.position, _target.transform.position);
            if (distance <= 0.1f)
            {
                _target.TakeDamage(_damage);
                Destroy(gameObject);
            }
        }
    }
}
