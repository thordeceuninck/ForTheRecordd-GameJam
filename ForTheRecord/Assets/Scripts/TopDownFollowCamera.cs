using UnityEngine;

namespace CameraCoop
{
    public class TopDownFollowCamera : MonoBehaviour
    {
        [Header("Target & Offsets")]
        [SerializeField] private Transform _target;
        [SerializeField] private Vector3 _offset = new Vector3(0f, 13.5f, -9f);
        [SerializeField] private float _pitchAngle = 58f;
        [SerializeField] private float _smoothSpeed = 5f;

        private void Start()
        {
            if (_target == null)
            {
                var controller = Object.FindFirstObjectByType<DualCarrierController>();
                if (controller != null) _target = controller.transform;
            }
            transform.rotation = Quaternion.Euler(_pitchAngle, 0f, 0f);
        }

        private void LateUpdate()
        {
            if (_target == null) return;

            Vector3 desiredPosition = _target.position + _offset;
            transform.position = Vector3.Lerp(transform.position, desiredPosition, _smoothSpeed * Time.deltaTime);
            transform.rotation = Quaternion.Euler(_pitchAngle, 0f, 0f);
        }

        public void SetTarget(Transform target)
        {
            _target = target;
        }
    }
}
