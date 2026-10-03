using UnityEngine;

namespace CameraCoop
{
    public class DancingCouple : MonoBehaviour
    {
        [Header("Orbit / Dance Settings")]
        [SerializeField] private Transform _brideTransform;
        [SerializeField] private Transform _groomTransform;
        [SerializeField] private float _orbitRadius = 0.5f;
        [SerializeField] private float _spinSpeed = 80f;
        [SerializeField] private float _waltzBobSpeed = 4f;
        [SerializeField] private float _waltzBobAmount = 0.06f;

        [Header("Floor Path Movement")]
        [SerializeField] private Vector3 _danceFloorCenter = new Vector3(5f, 0f, -4f);
        [SerializeField] private float _pathRadiusX = 2.8f;
        [SerializeField] private float _pathRadiusZ = 2.0f;
        [SerializeField] private float _pathSpeed = 0.35f;

        private float _orbitAngle = 0f;
        private float _pathProgress = 0f;
        private BumpableTarget _brideBump;
        private BumpableTarget _groomBump;

        private void Start()
        {
            if (_danceFloorCenter == Vector3.zero)
            {
                _danceFloorCenter = transform.position;
            }
            if (_brideTransform != null) _brideBump = _brideTransform.GetComponent<BumpableTarget>();
            if (_groomTransform != null) _groomBump = _groomTransform.GetComponent<BumpableTarget>();
        }

        private void Update()
        {
            // Move along an elliptical waltz path on the dance floor
            _pathProgress += Time.deltaTime * _pathSpeed;
            float px = _danceFloorCenter.x + Mathf.Cos(_pathProgress) * _pathRadiusX;
            float pz = _danceFloorCenter.z + Mathf.Sin(_pathProgress * 2f) * 0.5f * _pathRadiusZ;
            transform.position = new Vector3(px, 0f, pz);

            // Orbit couple around each other
            _orbitAngle += _spinSpeed * Time.deltaTime;
            float rad = _orbitAngle * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(Mathf.Cos(rad), 0f, Mathf.Sin(rad)) * _orbitRadius;

            float bob = Mathf.Abs(Mathf.Sin(Time.time * _waltzBobSpeed)) * _waltzBobAmount;

            if (_brideTransform != null && (_brideBump == null || !_brideBump.IsKnockedDown))
            {
                _brideTransform.localPosition = offset + new Vector3(0f, bob, 0f);
                _brideTransform.localRotation = Quaternion.Euler(0f, -_orbitAngle + 90f, 0f);
            }

            if (_groomTransform != null && (_groomBump == null || !_groomBump.IsKnockedDown))
            {
                _groomTransform.localPosition = -offset + new Vector3(0f, bob, 0f);
                _groomTransform.localRotation = Quaternion.Euler(0f, -_orbitAngle - 90f, 0f);
            }
        }

        public void SetDanceFloorCenter(Vector3 center)
        {
            _danceFloorCenter = center;
        }

        public void SetTransforms(Transform bride, Transform groom)
        {
            _brideTransform = bride;
            _groomTransform = groom;
        }
    }
}
