using System.Collections;
using UnityEngine;

namespace CameraCoop
{
    [RequireComponent(typeof(Collider))]
    public class BumpableTarget : MonoBehaviour
    {
        [Header("Bump Settings")]
        [SerializeField] private float _cooldown = 0.8f;
        [SerializeField] private float _impactForceMultiplier = 4.0f;
        [SerializeField] private bool _stabilizeUpright = true;
        [SerializeField] private float _uprightTorque = 12f;

        [Header("NPC & Stand Up Settings")]
        [SerializeField] private bool _isNPC = true;
        [SerializeField] private float _standUpDelay = 10f;
        [SerializeField] private float _standUpDuration = 0.8f;

        private Rigidbody _rb;
        private float _lastBumpTime = -10f;
        private Vector3 _initialPosition;
        private Quaternion _initialRotation;
        private bool _isKnockedDown = false;
        private Coroutine _standUpCoroutine;
        private SadSmileyPopup _activePopup;

        public Rigidbody AttachedRigidbody => _rb;
        public bool IsKnockedDown => _isKnockedDown;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            if (_rb == null)
            {
                _rb = gameObject.AddComponent<Rigidbody>();
                _rb.mass = 18f;
                _rb.linearDamping = 1.5f;
                _rb.angularDamping = 3.0f;
            }
            _initialPosition = transform.position;
            _initialRotation = transform.rotation;
        }

        private void FixedUpdate()
        {
            // Gently pull upright unless knocked over hard or currently knocked down
            if (_stabilizeUpright && !_isKnockedDown && _rb != null && !_rb.isKinematic)
            {
                Quaternion deltaRot = _initialRotation * Quaternion.Inverse(transform.rotation);
                deltaRot.ToAngleAxis(out float angle, out Vector3 axis);
                if (angle > 180f) angle -= 360f;
                if (Mathf.Abs(angle) > 1f && Mathf.Abs(angle) < 65f)
                {
                    _rb.AddTorque(axis.normalized * (angle * Mathf.Deg2Rad * _uprightTorque), ForceMode.Acceleration);
                }
            }
        }

        public bool TryRegisterBump(Vector3 hitDirection, float hitSpeed = 2f)
        {
            if (Time.time - _lastBumpTime < _cooldown) return false;
            _lastBumpTime = Time.time;

            // Apply penalty in WeddingGameManager (-500)
            if (WeddingGameManager.Instance != null)
            {
                WeddingGameManager.Instance.RegisterBump(gameObject.name);
            }

            // Audio feedback
            if (AudioFeedback.Instance != null)
            {
                AudioFeedback.Instance.PlayBumpSound();
            }

            // NPC reaction: sad smiley popup and knock down with 10s stand up
            if (_isNPC)
            {
                ShowSadSmileyPopup();
                KnockDown();
            }

            // Physics reaction
            if (_rb != null && !_rb.isKinematic)
            {
                Vector3 impulse = (hitDirection.normalized + Vector3.up * 0.4f) * Mathf.Max(hitSpeed * _impactForceMultiplier, 5.5f);
                _rb.AddForce(impulse, ForceMode.Impulse);
                _rb.AddTorque((Random.insideUnitSphere + Vector3.Cross(Vector3.up, hitDirection)).normalized * 8f, ForceMode.Impulse);
            }

            return true;
        }

        private void ShowSadSmileyPopup()
        {
            if (_activePopup != null)
            {
                Destroy(_activePopup.gameObject);
                _activePopup = null;
            }

            Transform head = transform.Find("Head");
            Vector3 spawnPos = (head != null ? head.position : transform.position) + Vector3.up * 0.45f;
            _activePopup = SadSmileyPopup.Spawn(spawnPos);
        }

        private void KnockDown()
        {
            _isKnockedDown = true;
            if (_standUpCoroutine != null)
            {
                StopCoroutine(_standUpCoroutine);
            }
            _standUpCoroutine = StartCoroutine(StandUpRoutine());
        }

        private IEnumerator StandUpRoutine()
        {
            // Wait for 10 seconds while knocked down
            yield return new WaitForSeconds(_standUpDelay);

            Debug.Log($"[Test] StandUpRoutine starting stand-up animation for {gameObject.name} at time {Time.time}");

            // Cancel any residual physical momentum before standing up
            if (_rb != null)
            {
                _rb.linearVelocity = Vector3.zero;
                _rb.angularVelocity = Vector3.zero;
            }

            Quaternion startRot = transform.rotation;
            Quaternion targetRot = Quaternion.Euler(0f, _initialRotation.eulerAngles.y, 0f);

            Vector3 startPos = transform.position;
            float groundY = _initialPosition.y;
            if (Physics.Raycast(new Vector3(startPos.x, startPos.y + 1.5f, startPos.z), Vector3.down, out RaycastHit hit, 3.5f))
            {
                if (!hit.collider.isTrigger && !hit.collider.transform.IsChildOf(transform))
                {
                    groundY = hit.point.y;
                }
            }
            Vector3 targetPos = new Vector3(startPos.x, groundY, startPos.z);

            float elapsed = 0f;
            while (elapsed < _standUpDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / _standUpDuration);
                float smoothT = Mathf.SmoothStep(0f, 1f, t);

                transform.rotation = Quaternion.Slerp(startRot, targetRot, smoothT);
                transform.position = Vector3.Lerp(startPos, targetPos, smoothT);

                if (_rb != null)
                {
                    _rb.linearVelocity = Vector3.zero;
                    _rb.angularVelocity = Vector3.zero;
                }

                yield return null;
            }

            transform.rotation = targetRot;
            transform.position = targetPos;

            if (_rb != null)
            {
                _rb.linearVelocity = Vector3.zero;
                _rb.angularVelocity = Vector3.zero;
            }

            _isKnockedDown = false;
            _standUpCoroutine = null;
            Debug.Log($"[Test] StandUpRoutine finished for {gameObject.name} at time {Time.time}, IsKnockedDown is now {_isKnockedDown}");
        }

        private void OnDisable()
        {
            if (_standUpCoroutine != null)
            {
                StopCoroutine(_standUpCoroutine);
                _standUpCoroutine = null;
            }
            if (_activePopup != null)
            {
                Destroy(_activePopup.gameObject);
                _activePopup = null;
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (collision.collider.CompareTag("Player") || collision.collider.transform.root.name.Contains("CoOpCarrierRig"))
            {
                Vector3 dir = transform.position - collision.contacts[0].point;
                dir.y = 0.1f;
                float speed = collision.relativeVelocity.magnitude;
                TryRegisterBump(dir, speed);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player") || other.transform.root.name.Contains("CoOpCarrierRig"))
            {
                Vector3 dir = transform.position - other.transform.position;
                dir.y = 0.1f;
                TryRegisterBump(dir, 2.5f);
            }
        }
    }
}
