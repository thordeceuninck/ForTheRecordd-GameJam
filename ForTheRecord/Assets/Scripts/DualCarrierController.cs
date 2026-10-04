using UnityEngine;

namespace CameraCoop
{
    [RequireComponent(typeof(CharacterController))]
    public class DualCarrierController : MonoBehaviour
    {
        [Header("Movement Tuning")]
        [SerializeField] private float _moveSpeed = 3.6f;
        [SerializeField] private float _turnSpeed = 85f;
        [SerializeField] private float _handleHalfDistance = 1.05f; // Distance of each player from center

        [Header("References")]
        [SerializeField] private GiantCamera _giantCamera;
        [SerializeField] private Transform _player1Transform;
        [SerializeField] private Transform _player2Transform;
        [SerializeField] private Transform _cameraVisualTransform;

        [Header("Tension & Visuals")]
        [SerializeField] private float _bobFrequency = 11f;
        [SerializeField] private float _bobAmount = 0.04f;
        [SerializeField] private float _tiltAmount = 3.5f;

        [Header("Carrier Joystick Lean & Jiggle")]
        [SerializeField] private float _joystickOffsetAmount = 0.22f; // Max displacement in joystick direction
        [SerializeField] private float _joystickOffsetSpeed = 12f;    // Responsiveness of the joystick displacement
        [SerializeField] private float _jiggleAmount = 0.035f;        // Intensity of position jiggle while moving
        [SerializeField] private float _jiggleFrequency = 22f;       // Rapid jiggle oscillation frequency
        [SerializeField] private float _leanTiltAmount = 7f;         // Degrees carrier leans in joystick direction
        [SerializeField] private float _rotJiggleAmount = 3f;        // Degrees of rotational wobble jiggle

        private CharacterController _characterController;
        private Vector3 _p1OriginalLocalPos;
        private Vector3 _p2OriginalLocalPos;
        private Quaternion _p1OriginalLocalRot;
        private Quaternion _p2OriginalLocalRot;
        private Vector3 _p1CurrentOffset = Vector3.zero;
        private Vector3 _p2CurrentOffset = Vector3.zero;
        private float _p1JigglePhase = 0f;
        private float _p2JigglePhase = 1.8f;
        private Quaternion _camOriginalLocalRot;
        private float _walkPhase = 0f;
        private float _currentTilt = 0f;
        private Vector3 _knockbackVelocity = Vector3.zero;

        public float LastCoopSync { get; private set; } = 95f;

        private void Awake()
        {
            _characterController = GetComponent<CharacterController>();
            if (_giantCamera == null) _giantCamera = GetComponentInChildren<GiantCamera>();

            if (_player1Transform == null)
            {
                var p1 = transform.Find("Player1_Marigold");
                if (p1 != null) _player1Transform = p1;
            }
            if (_player2Transform == null)
            {
                var p2 = transform.Find("Player2_Teal");
                if (p2 != null) _player2Transform = p2;
            }

            if (_player1Transform != null)
            {
                _p1OriginalLocalPos = _player1Transform.localPosition;
                _p1OriginalLocalRot = _player1Transform.localRotation;
            }
            if (_player2Transform != null)
            {
                _p2OriginalLocalPos = _player2Transform.localPosition;
                _p2OriginalLocalRot = _player2Transform.localRotation;
            }
            if (_cameraVisualTransform != null) _camOriginalLocalRot = _cameraVisualTransform.localRotation;
        }

        private void Update()
        {
            HandleMovement();
            HandleShutterInput();
        }

        private void HandleMovement()
        {
            if (CoOpInputManager.Instance == null) return;

            Vector2 input1 = CoOpInputManager.Instance.GetP1Movement();
            Vector2 input2 = CoOpInputManager.Instance.GetP2Movement();

            // Convert 2D input to world vectors
            Vector3 v1 = new Vector3(input1.x, 0f, input1.y);
            Vector3 v2 = new Vector3(input2.x, 0f, input2.y);

            // Compute co-op sync
            if (v1.sqrMagnitude > 0.05f && v2.sqrMagnitude > 0.05f)
            {
                float dot = Vector3.Dot(v1.normalized, v2.normalized);
                LastCoopSync = Mathf.Clamp(Mathf.Round((dot + 1f) * 45f + 10f), 10f, 100f);
            }

            Vector3 fwd = transform.forward;
            Vector3 right = transform.right;

            // Forward forces from each player
            float fwd1 = Vector3.Dot(v1, fwd);
            float fwd2 = Vector3.Dot(v2, fwd);

            // Strafe forces from each player
            float str1 = Vector3.Dot(v1, right);
            float str2 = Vector3.Dot(v2, right);

            // Net translation velocity
            float avgFwd = (fwd1 + fwd2) * 0.5f;
            float avgStr = (str1 + str2) * 0.5f;
            Vector3 moveVelocity = (fwd * avgFwd + right * avgStr) * _moveSpeed;

            // Angular velocity from forward differential: P1 on left (fwd1 pushes clockwise / right), P2 on right (fwd2 pushes counter-clockwise / left)
            float turnRate = (fwd1 - fwd2) * _turnSpeed;

            // Also allow direct rotation if one pushes sideways in opposite direction
            turnRate += (str2 - str1) * (_turnSpeed * 0.35f);

            // Check if either player is near a wall to prevent spinning into walls
            CheckHandleCollision(ref moveVelocity, ref turnRate);

            // Apply rotation
            transform.Rotate(Vector3.up, turnRate * Time.deltaTime);

            // Apply knockback if active
            if (_knockbackVelocity.sqrMagnitude > 0.01f)
            {
                moveVelocity += _knockbackVelocity;
                _knockbackVelocity = Vector3.MoveTowards(_knockbackVelocity, Vector3.zero, Time.deltaTime * 18f);
            }

            // Apply gravity
            moveVelocity.y = -9.81f;

            // Apply movement via CharacterController
            _characterController.Move(moveVelocity * Time.deltaTime);

            // Target offset in the direction of each player's joystick (converted to rig local space)
            Vector3 p1LocalInputDir = new Vector3(str1, 0f, fwd1);
            Vector3 p2LocalInputDir = new Vector3(str2, 0f, fwd2);

            Vector3 p1TargetOffset = Vector3.ClampMagnitude(p1LocalInputDir, 1f) * _joystickOffsetAmount;
            Vector3 p2TargetOffset = Vector3.ClampMagnitude(p2LocalInputDir, 1f) * _joystickOffsetAmount;

            _p1CurrentOffset = Vector3.Lerp(_p1CurrentOffset, p1TargetOffset, Time.deltaTime * _joystickOffsetSpeed);
            _p2CurrentOffset = Vector3.Lerp(_p2CurrentOffset, p2TargetOffset, Time.deltaTime * _joystickOffsetSpeed);

            // Walking bobbing and chassis tilt
            bool isMoving = (v1.sqrMagnitude > 0.01f || v2.sqrMagnitude > 0.01f || _knockbackVelocity.sqrMagnitude > 0.05f);
            if (isMoving)
            {
                _walkPhase += Time.deltaTime * _bobFrequency;
            }

            float p1Bob = Mathf.Sin(_walkPhase) * _bobAmount;
            float p2Bob = Mathf.Sin(_walkPhase + Mathf.PI) * _bobAmount;

            // Activity level for dynamic jiggle
            float netSpeedRatio = Mathf.Clamp01(new Vector2(moveVelocity.x, moveVelocity.z).magnitude / Mathf.Max(_moveSpeed, 0.01f));
            float p1Activity = Mathf.Clamp01(Mathf.Max(v1.magnitude, netSpeedRatio * 0.5f));
            float p2Activity = Mathf.Clamp01(Mathf.Max(v2.magnitude, netSpeedRatio * 0.5f));

            if (p1Activity > 0.02f) _p1JigglePhase += Time.deltaTime * _jiggleFrequency;
            if (p2Activity > 0.02f) _p2JigglePhase += Time.deltaTime * _jiggleFrequency;

            // Position jiggle (rapid vibration/wobble while moving)
            float jiggleX1 = Mathf.Sin(_p1JigglePhase) * _jiggleAmount * p1Activity;
            float jiggleZ1 = Mathf.Cos(_p1JigglePhase * 1.33f) * (_jiggleAmount * 0.75f) * p1Activity;
            float jiggleY1 = Mathf.Abs(Mathf.Sin(_p1JigglePhase * 1.6f)) * (_jiggleAmount * 0.5f) * p1Activity;
            Vector3 p1Jiggle = new Vector3(jiggleX1, jiggleY1, jiggleZ1);

            float jiggleX2 = Mathf.Sin(_p2JigglePhase) * _jiggleAmount * p2Activity;
            float jiggleZ2 = Mathf.Cos(_p2JigglePhase * 1.33f) * (_jiggleAmount * 0.75f) * p2Activity;
            float jiggleY2 = Mathf.Abs(Mathf.Sin(_p2JigglePhase * 1.6f)) * (_jiggleAmount * 0.5f) * p2Activity;
            Vector3 p2Jiggle = new Vector3(jiggleX2, jiggleY2, jiggleZ2);

            // Update carrier local positions (stay anchored near camera, with joystick displacement + bob + jiggle)
            if (_player1Transform != null)
            {
                _player1Transform.localPosition = _p1OriginalLocalPos + _p1CurrentOffset + new Vector3(0f, p1Bob, 0f) + p1Jiggle;
            }
            if (_player2Transform != null)
            {
                _player2Transform.localPosition = _p2OriginalLocalPos + _p2CurrentOffset + new Vector3(0f, p2Bob, 0f) + p2Jiggle;
            }

            // Carrier lean tilt in movement direction + rotational wobble jiggle
            if (_player1Transform != null)
            {
                float normDist = Mathf.Max(_joystickOffsetAmount, 0.001f);
                float pitch1 = (_p1CurrentOffset.z / normDist) * _leanTiltAmount;
                float roll1 = -(_p1CurrentOffset.x / normDist) * _leanTiltAmount;
                float rotJiggle1 = Mathf.Sin(_p1JigglePhase * 1.15f) * _rotJiggleAmount * p1Activity;

                Quaternion targetRot1 = _p1OriginalLocalRot * Quaternion.Euler(pitch1 + rotJiggle1, 0f, roll1 - rotJiggle1 * 0.6f);
                _player1Transform.localRotation = Quaternion.Slerp(_player1Transform.localRotation, targetRot1, Time.deltaTime * 14f);
            }

            if (_player2Transform != null)
            {
                float normDist = Mathf.Max(_joystickOffsetAmount, 0.001f);
                float pitch2 = (_p2CurrentOffset.z / normDist) * _leanTiltAmount;
                float roll2 = -(_p2CurrentOffset.x / normDist) * _leanTiltAmount;
                float rotJiggle2 = Mathf.Sin(_p2JigglePhase * 1.15f) * _rotJiggleAmount * p2Activity;

                Quaternion targetRot2 = _p2OriginalLocalRot * Quaternion.Euler(pitch2 + rotJiggle2, 0f, roll2 - rotJiggle2 * 0.6f);
                _player2Transform.localRotation = Quaternion.Slerp(_player2Transform.localRotation, targetRot2, Time.deltaTime * 14f);
            }

            // Chassis sway/tilt into turns
            float targetTilt = Mathf.Clamp(turnRate * 0.04f, -_tiltAmount, _tiltAmount);
            _currentTilt = Mathf.Lerp(_currentTilt, targetTilt, Time.deltaTime * 6f);

            if (_cameraVisualTransform != null)
            {
                _cameraVisualTransform.localRotation = _camOriginalLocalRot * Quaternion.Euler(0f, 0f, -_currentTilt);
            }
        }

        private void CheckHandleCollision(ref Vector3 moveVelocity, ref float turnRate)
        {
            // SphereCast around P1 and P2 handle positions
            float checkRadius = 0.35f;
            Vector3 p1Pos = transform.position - transform.right * _handleHalfDistance + Vector3.up * 0.5f;
            Vector3 p2Pos = transform.position + transform.right * _handleHalfDistance + Vector3.up * 0.5f;

            Collider[] hits1 = Physics.OverlapSphere(p1Pos, checkRadius);
            foreach (var col in hits1)
            {
                if (col.CompareTag("Obstacle") || col.CompareTag("Wall") || col.gameObject.name.Contains("Wall"))
                {
                    // P1 is near a wall, dampen clockwise turn and left strafe
                    if (turnRate > 0) turnRate *= 0.1f;
                    break;
                }
            }

            Collider[] hits2 = Physics.OverlapSphere(p2Pos, checkRadius);
            foreach (var col in hits2)
            {
                if (col.CompareTag("Obstacle") || col.CompareTag("Wall") || col.gameObject.name.Contains("Wall"))
                {
                    // P2 is near a wall, dampen counter-clockwise turn and right strafe
                    if (turnRate < 0) turnRate *= 0.1f;
                    break;
                }
            }
        }

        private void HandleShutterInput()
        {
            if (CoOpInputManager.Instance == null) return;

            // If photo review card is open, any action dismisses it
            if (PhotoReviewUI.Instance != null && PhotoReviewUI.Instance.IsReviewOpen)
            {
                if (CoOpInputManager.Instance.WasAnyActionPressed())
                {
                    PhotoReviewUI.Instance.DismissReview();
                }
                return;
            }

            // P1 activates flash (lasts 0.5s)
            if (CoOpInputManager.Instance.WasP1ActionPressed())
            {
                if (_giantCamera != null)
                {
                    _giantCamera.ActivateFlash();
                }
            }

            // P2 takes the photo
            if (CoOpInputManager.Instance.WasP2ActionPressed())
            {
                SnapPhoto();
            }
        }

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            if (hit.collider == null) return;

            var bumpable = hit.collider.GetComponentInParent<BumpableTarget>();
            if (bumpable != null)
            {
                Vector3 hitDir = hit.point - transform.position;
                hitDir.y = 0.1f;
                float speed = _characterController != null ? _characterController.velocity.magnitude : 2f;
                bumpable.TryRegisterBump(hitDir, speed);
            }

            var rb = hit.collider.attachedRigidbody;
            if (rb != null && !rb.isKinematic)
            {
                Vector3 pushDir = new Vector3(hit.moveDirection.x, 0.1f, hit.moveDirection.z);
                rb.AddForce(pushDir * 6f, ForceMode.Impulse);
            }
        }

        public void SnapPhoto()
        {
            if (_giantCamera == null) return;
            PhotoEvaluation eval = _giantCamera.SnapPhoto(LastCoopSync);

            if (PhotoReviewUI.Instance != null)
            {
                PhotoReviewUI.Instance.ShowReview(eval, _giantCamera.GetPhotoTexture());
            }

            if (eval.stars > 0)
            {
                if (AudioFeedback.Instance != null)
                    AudioFeedback.Instance.PlaySuccessChime(eval.stars);
            }
            else
            {
                if (AudioFeedback.Instance != null)
                    AudioFeedback.Instance.PlayFailBuzz();
            }
        }

        public void BumpBack(Vector3 direction, float force = 8.5f)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.001f)
            {
                direction = -transform.forward;
            }
            _knockbackVelocity = direction.normalized * force;
        }
    }
}
