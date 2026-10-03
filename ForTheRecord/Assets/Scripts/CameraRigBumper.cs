using UnityEngine;

namespace CameraCoop
{
    public class CameraRigBumper : MonoBehaviour
    {
        private void OnTriggerEnter(Collider other)
        {
            var bumpable = other.GetComponentInParent<BumpableTarget>();
            if (bumpable != null)
            {
                Vector3 hitDir = other.transform.position - transform.position;
                hitDir.y = 0.1f;
                bumpable.TryRegisterBump(hitDir, 2.5f);
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            var bumpable = collision.collider.GetComponentInParent<BumpableTarget>();
            if (bumpable != null)
            {
                Vector3 hitDir = collision.transform.position - transform.position;
                hitDir.y = 0.1f;
                bumpable.TryRegisterBump(hitDir, 2.5f);
            }
        }
    }
}
