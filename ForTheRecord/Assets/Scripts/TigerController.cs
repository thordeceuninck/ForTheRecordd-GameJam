using System.Collections;
using UnityEngine;

namespace CameraCoop
{
    public class TigerController : MonoBehaviour
    {
        [Header("Tiger Body & Anchor")]
        [SerializeField] private Transform _tigerBody;
        [SerializeField] private Transform _anchorPost;
        [SerializeField] private Transform _neckCollar;
        [SerializeField] private LineRenderer _chainRenderer;
        [SerializeField] private float _chainLength = 2.8f;

        [Header("Attack Settings")]
        [SerializeField] private float _detectionRange = 4.0f;
        [SerializeField] private float _knockbackForce = 10.0f;
        [SerializeField] private float _attackCooldown = 2.4f;

        [Header("Animation Parts")]
        [SerializeField] private Transform _tail;
        [SerializeField] private Transform _head;
        [SerializeField] private Transform _frontLeftPaw;
        [SerializeField] private Transform _frontRightPaw;

        private Vector3 _homePosition;
        private Quaternion _homeRotation;
        private float _lastAttackTime = -10f;
        private bool _isAttacking = false;
        private DualCarrierController _targetCarrier;

        private Transform Body => _tigerBody != null ? _tigerBody : transform;

        private void Start()
        {
            if (_tigerBody == null)
            {
                _tigerBody = transform.Find("TigerModel");
            }

            _homePosition = Body.position;
            _homeRotation = Body.rotation;

            _targetCarrier = Object.FindFirstObjectByType<DualCarrierController>();

            // Setup procedural chain visual if line renderer present
            if (_chainRenderer == null)
            {
                _chainRenderer = GetComponent<LineRenderer>();
            }

            if (_chainRenderer != null)
            {
                _chainRenderer.positionCount = 12;
                _chainRenderer.startWidth = 0.08f;
                _chainRenderer.endWidth = 0.08f;
            }
        }

        private void Update()
        {
            UpdateChainVisual();

            if (_targetCarrier == null)
            {
                _targetCarrier = Object.FindFirstObjectByType<DualCarrierController>();
                if (_targetCarrier == null) return;
            }

            Vector3 toPlayer = _targetCarrier.transform.position - Body.position;
            float distToPlayer = new Vector2(toPlayer.x, toPlayer.z).magnitude;

            if (!_isAttacking)
            {
                // Idle breathing and tail swish
                float tailSwish = Mathf.Sin(Time.time * 3.5f) * 22f;
                if (_tail != null)
                {
                    _tail.localRotation = Quaternion.Euler(0f, tailSwish, 0f);
                }

                // Look at player if in alert range (< 8m)
                if (distToPlayer < 8.0f)
                {
                    Vector3 lookDir = new Vector3(toPlayer.x, 0f, toPlayer.z);
                    if (lookDir.sqrMagnitude > 0.01f)
                    {
                        Quaternion targetRot = Quaternion.LookRotation(lookDir, Vector3.up);
                        Body.rotation = Quaternion.Slerp(Body.rotation, targetRot, Time.deltaTime * 3.5f);
                    }
                }
                else
                {
                    Body.rotation = Quaternion.Slerp(Body.rotation, _homeRotation, Time.deltaTime * 2f);
                }

                // Check attack trigger
                if (distToPlayer <= _detectionRange && Time.time - _lastAttackTime >= _attackCooldown)
                {
                    StartCoroutine(AttackRoutine(_targetCarrier));
                }
            }
        }

        private IEnumerator AttackRoutine(DualCarrierController carrier)
        {
            _isAttacking = true;
            _lastAttackTime = Time.time;

            Vector3 startPos = Body.position;
            Vector3 toTarget = carrier.transform.position - Body.position;
            toTarget.y = 0f;
            Vector3 lungeTarget = Body.position + toTarget.normalized * Mathf.Min(toTarget.magnitude * 0.75f, _chainLength);

            // Audio & roar
            if (AudioFeedback.Instance != null)
            {
                AudioFeedback.Instance.PlayBumpSound();
            }

            // Quick lunge forward (0.18s)
            float elapsed = 0f;
            float lungeDuration = 0.18f;
            while (elapsed < lungeDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / lungeDuration);
                Body.position = Vector3.Lerp(startPos, lungeTarget, t);
                yield return null;
            }

            // Strike: Knock back players and spawn sad face popup
            Vector3 knockbackDir = (carrier.transform.position - Body.position).normalized;
            carrier.BumpBack(knockbackDir, _knockbackForce);

            // Sad smiley popup above players
            Vector3 popupPos = carrier.transform.position + Vector3.up * 1.85f;
            SadSmileyPopup.Spawn(popupPos);

            // Hold lunge posture briefly
            yield return new WaitForSeconds(0.25f);

            // Return to home position restrained by chain (0.45s)
            elapsed = 0f;
            float returnDuration = 0.45f;
            Vector3 currentPos = Body.position;
            while (elapsed < returnDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / returnDuration));
                Body.position = Vector3.Lerp(currentPos, _homePosition, t);
                yield return null;
            }

            Body.position = _homePosition;
            _isAttacking = false;
        }

        private void UpdateChainVisual()
        {
            if (_chainRenderer == null || _anchorPost == null) return;

            Vector3 collarPos = _neckCollar != null ? _neckCollar.position : Body.position + Vector3.up * 0.7f;
            Vector3 anchorPos = _anchorPost.position + Vector3.up * 0.5f;

            int count = _chainRenderer.positionCount;
            for (int i = 0; i < count; i++)
            {
                float t = (float)i / (count - 1);
                Vector3 pos = Vector3.Lerp(anchorPos, collarPos, t);
                // Catenary chain sag in middle
                float sag = Mathf.Sin(t * Mathf.PI) * 0.35f;
                pos.y -= sag;
                _chainRenderer.SetPosition(i, pos);
            }
        }

        public void SetupComponents(Transform anchor, Transform collar, Transform tail, Transform head, Transform leftPaw, Transform rightPaw, Transform body = null)
        {
            _anchorPost = anchor;
            _neckCollar = collar;
            _tail = tail;
            _head = head;
            _frontLeftPaw = leftPaw;
            _frontRightPaw = rightPaw;
            if (body != null) _tigerBody = body;
        }
    }
}
