using UnityEngine;

namespace OzGameLab01.Controllers
{
    public class CameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private float deadZoneRadius = 2.5f;
        [SerializeField] private float followSpeed = 3f;

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            Vector3 targetPos = new Vector3(target.position.x, target.position.y, transform.position.z);
            Vector3 offset = targetPos - transform.position;
            float distance = offset.magnitude;

            if (distance > deadZoneRadius)
            {
                Vector3 excess = offset.normalized * (distance - deadZoneRadius);
                Vector3 desired = transform.position + excess;
                transform.position = Vector3.Lerp(transform.position, desired, followSpeed * Time.deltaTime);
            }
        }
    }
}
