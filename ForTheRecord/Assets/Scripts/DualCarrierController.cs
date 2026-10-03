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

        private CharacterController _characterController;
        private Vector3 _p1OriginalLocalPos;
        private Vector3 _p2OriginalLocalPos;
        private Quaternion _camOriginalLocalRot;
        private float _walkPhase = 0f;
        private float _currentTilt = 0f;

        public float LastCoopSync { get; private set; } = 95f;

        private void Awake()
        {
            _characterController = GetComponent<CharacterController>();
            if (_giantCamera == null) _giantCamera = GetComponentInChildren<GiantCamera>();

            if (_player1Transform != null) _p1OriginalLocalPos = _player1Transform.localPosition;
            if (_player2Transform != null) _p2OriginalLocalPos = _player2Transform.localPosition;
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

            // Apply gravity
            moveVelocity.y = -9.81f;

            // Apply movement via CharacterController
            _characterController.Move(moveVelocity * Time.deltaTime);

            // Walking bobbing and chassis tilt
            bool isMoving = (v1.sqrMagnitude > 0.01f || v2.sqrMagnitude > 0.01f);
            if (isMoving)
            {
                _walkPhase += Time.deltaTime * _bobFrequency;
            }

            float p1Bob = Mathf.Sin(_walkPhase) * _bobAmount;
            float p2Bob = Mathf.Sin(_walkPhase + Mathf.PI) * _bobAmount;

            if (_player1Transform != null)
            {
                _player1Transform.localPosition = _p1OriginalLocalPos + new Vector3(0f, p1Bob, 0f);
            }
            if (_player2Transform != null)
            {
                _player2Transform.localPosition = _p2OriginalLocalPos + new Vector3(0f, p2Bob, 0f);
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

            if (CoOpInputManager.Instance.WasAnyActionPressed())
            {
                if (PhotoReviewUI.Instance != null && PhotoReviewUI.Instance.IsReviewOpen)
                {
                    PhotoReviewUI.Instance.DismissReview();
                }
                else
                {
                    SnapPhoto();
                }
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
    }
}
