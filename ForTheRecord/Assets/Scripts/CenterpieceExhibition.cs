using UnityEngine;

namespace CameraCoop
{
    public class CenterpieceExhibition : MonoBehaviour
    {
        [SerializeField] private Transform _figurineTransform;
        [SerializeField] private float _spinSpeed = 25f;
        [SerializeField] private float _bobSpeed = 2f;
        [SerializeField] private float _bobAmount = 0.05f;

        private Vector3 _figurineBasePos;

        private void Start()
        {
            if (_figurineTransform == null && transform.childCount > 0)
            {
                _figurineTransform = transform.GetChild(0);
            }
            if (_figurineTransform != null)
            {
                _figurineBasePos = _figurineTransform.localPosition;
            }
        }

        private void Update()
        {
            if (_figurineTransform != null)
            {
                _figurineTransform.Rotate(Vector3.up, _spinSpeed * Time.deltaTime, Space.World);
                float bob = Mathf.Sin(Time.time * _bobSpeed) * _bobAmount;
                _figurineTransform.localPosition = _figurineBasePos + new Vector3(0f, bob, 0f);
            }
        }
    }
}
