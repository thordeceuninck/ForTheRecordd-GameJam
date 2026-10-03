using System.Collections;
using UnityEngine;

namespace CameraCoop
{
    public class GiantCamera : MonoBehaviour
    {
        public static GiantCamera Instance { get; private set; }

        [Header("Optical Settings")]
        [SerializeField] private Transform _lensTransform;
        [SerializeField] private Transform _targetTransform;
        [SerializeField] private float _fovAngle = 55f;
        [SerializeField] private float _maxRange = 7.5f;
        [SerializeField] private float _minRange = 0.8f;
        [SerializeField] private LayerMask _obstacleMask = ~0;

        [Header("Flash & Recoil")]
        [SerializeField] private Light _flashLight;
        [SerializeField] private Transform _bellowsTransform;
        [SerializeField] private Transform _flashBulbTransform;
        [SerializeField] private MeshRenderer _flashBulbRenderer;
        [SerializeField] private float _flashDuration = 0.5f;

        [Header("Lens Capture Camera")]
        [SerializeField] private Camera _lensCaptureCamera;
        [SerializeField] private RenderTexture _photoRenderTexture;

        [Header("Viewfinder Frustum Projection")]
        [SerializeField] private MeshFilter _frustumMeshFilter;
        [SerializeField] private MeshRenderer _frustumMeshRenderer;
        [SerializeField] private LineRenderer _frustumLineRenderer;

        // Current status
        public bool IsTargetInCone { get; private set; }
        public bool IsLineOfSightClear { get; private set; }
        public bool IsBlockedByWall { get; private set; }
        public float CurrentTargetDistance { get; private set; }
        public float CurrentAngleOffset { get; private set; }
        public PhotoSubject FocusedSubject { get; private set; }

        public bool IsFlashActive => _flashTimer > 0f;
        public float FlashTimeRemaining => Mathf.Max(0f, _flashTimer);

        private float _flashTimer = 0f;
        private Mesh _frustumMesh;
        private Material _frustumMaterial;
        private Color _colorSearching = new Color(0.9f, 0.86f, 0.78f, 0.28f); // Ivory
        private Color _colorFramed = new Color(0.41f, 0.83f, 0.57f, 0.55f);    // Soft Jade Green
        private Color _colorBlocked = new Color(0.9f, 0.25f, 0.25f, 0.55f);   // Terracotta Vermillion

        private Vector3 _originalBellowsScale = Vector3.one;
        private bool _isSnapping = false;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (_bellowsTransform != null)
            {
                _originalBellowsScale = _bellowsTransform.localScale;
            }

            if (_flashLight != null)
            {
                _flashLight.enabled = false;
            }

            if (_flashBulbTransform != null && _flashBulbRenderer == null)
            {
                _flashBulbRenderer = _flashBulbTransform.GetComponent<MeshRenderer>();
            }

            SetupRenderTexture();
            SetupFrustumMesh();
        }

        private void Start()
        {
            if (_targetTransform == null)
            {
                GameObject t = GameObject.FindWithTag("Target");
                if (t != null) _targetTransform = t.transform;
            }
        }

        private void SetupRenderTexture()
        {
            if (_photoRenderTexture == null)
            {
                _photoRenderTexture = new RenderTexture(512, 512, 16, RenderTextureFormat.ARGB32);
                _photoRenderTexture.name = "PhotoSnapshotRT";
            }

            if (_lensCaptureCamera != null)
            {
                _lensCaptureCamera.targetTexture = _photoRenderTexture;
                _lensCaptureCamera.enabled = false;
            }
        }

        private void SetupFrustumMesh()
        {
            if (_frustumMeshFilter != null)
            {
                _frustumMesh = new Mesh();
                _frustumMesh.name = "DynamicFrustumMesh";
                _frustumMeshFilter.mesh = _frustumMesh;
            }

            if (_frustumMeshRenderer != null)
            {
                _frustumMaterial = _frustumMeshRenderer.material;
            }

            if (_frustumLineRenderer != null)
            {
                _frustumLineRenderer.useWorldSpace = true;
                _frustumLineRenderer.startWidth = 0.08f;
                _frustumLineRenderer.endWidth = 0.08f;
            }
        }

        private void Update()
        {
            UpdateFlashState();
            UpdateOpticalEvaluation();
            UpdateFrustumVisuals();
        }

        public void ActivateFlash()
        {
            _flashTimer = _flashDuration;

            if (_flashLight != null)
            {
                _flashLight.enabled = true;
                _flashLight.intensity = 15f;
            }

            if (_flashBulbRenderer != null && _flashBulbRenderer.material != null)
            {
                _flashBulbRenderer.material.color = new Color(1f, 0.95f, 0.6f);
            }

            if (AudioFeedback.Instance != null)
            {
                AudioFeedback.Instance.PlayFlashCharge();
            }
        }

        private void UpdateFlashState()
        {
            if (_flashTimer > 0f)
            {
                _flashTimer -= Time.deltaTime;
                if (_flashTimer <= 0f)
                {
                    _flashTimer = 0f;
                    if (!_isSnapping && _flashLight != null)
                    {
                        _flashLight.enabled = false;
                    }
                    if (_flashBulbRenderer != null && _flashBulbRenderer.material != null)
                    {
                        _flashBulbRenderer.material.color = new Color(0.8f, 0.8f, 0.8f);
                    }
                }
            }
        }

        public void SetTarget(Transform target)
        {
            _targetTransform = target;
        }

        public void UpdateOpticalEvaluation()
        {
            if (_lensTransform == null)
            {
                IsTargetInCone = false;
                IsLineOfSightClear = false;
                IsBlockedByWall = false;
                FocusedSubject = null;
                return;
            }

            Vector3 lensPos = _lensTransform.position;
            Vector3 flatForward = new Vector3(_lensTransform.forward.x, 0f, _lensTransform.forward.z).normalized;

            PhotoSubject bestSubject = null;
            float bestAngle = float.MaxValue;
            float bestDist = 0f;
            bool bestBlocked = false;

            // Search through registered wedding subjects
            var subjects = PhotoSubject.AllSubjects;
            if (subjects.Count > 0)
            {
                foreach (var s in subjects)
                {
                    if (s == null || !s.gameObject.activeInHierarchy) continue;

                    Vector3 targetPos = s.FocusTransform.position;
                    Vector3 toTarget = targetPos - lensPos;
                    Vector3 flatToTarget = new Vector3(toTarget.x, 0f, toTarget.z);
                    float dist = flatToTarget.magnitude;

                    if (dist < _minRange || dist > _maxRange) continue;

                    float angle = Vector3.Angle(flatForward, flatToTarget.normalized);
                    if (angle > (_fovAngle * 0.5f)) continue;

                    // Line of sight check
                    bool blocked = CheckSightlineBlocked(lensPos, targetPos, s.transform);

                    // Pick the most centered target
                    if (angle < bestAngle)
                    {
                        bestAngle = angle;
                        bestDist = dist;
                        bestSubject = s;
                        bestBlocked = blocked;
                    }
                }
            }
            else if (_targetTransform != null)
            {
                // Fallback to legacy single target
                Vector3 targetPos = _targetTransform.position;
                Vector3 toTarget = targetPos - lensPos;
                Vector3 flatToTarget = new Vector3(toTarget.x, 0f, toTarget.z);
                float dist = flatToTarget.magnitude;
                float angle = Vector3.Angle(flatForward, flatToTarget.normalized);

                if (angle <= (_fovAngle * 0.5f) && dist <= _maxRange)
                {
                    bestAngle = angle;
                    bestDist = dist;
                    bestBlocked = CheckSightlineBlocked(lensPos, targetPos, _targetTransform);
                }
            }

            FocusedSubject = bestSubject;
            if (bestSubject != null || (subjects.Count == 0 && _targetTransform != null && bestAngle <= _fovAngle * 0.5f))
            {
                IsTargetInCone = true;
                CurrentAngleOffset = bestAngle;
                CurrentTargetDistance = bestDist;
                IsBlockedByWall = bestBlocked;
                IsLineOfSightClear = !bestBlocked;
            }
            else
            {
                IsTargetInCone = false;
                IsLineOfSightClear = false;
                IsBlockedByWall = false;
                CurrentAngleOffset = 0f;
                CurrentTargetDistance = 0f;
            }
        }

        private bool CheckSightlineBlocked(Vector3 lensPos, Vector3 targetPos, Transform targetTransform)
        {
            Vector3 targetCenter = targetPos + Vector3.up * 0.6f;
            Vector3 rayDir = (targetCenter - lensPos).normalized;
            float testDist = Vector3.Distance(lensPos, targetCenter);

            RaycastHit[] hits = Physics.RaycastAll(lensPos, rayDir, testDist);
            foreach (var hit in hits)
            {
                if (hit.collider.transform.IsChildOf(transform.root)) continue;
                if (hit.collider.transform.IsChildOf(targetTransform)) continue;

                if (hit.collider.CompareTag("Obstacle") || hit.collider.CompareTag("Wall") || hit.collider.gameObject.name.Contains("Wall") || hit.collider.gameObject.name.Contains("Partition"))
                {
                    return true;
                }
            }
            return false;
        }

        public PhotoEvaluation SnapPhoto(float coopSyncPercentage = 95f)
        {
            if (_isSnapping && Application.isPlaying) return default;
            if (Application.isPlaying)
            {
                StartCoroutine(SnapRoutine());
            }

            PhotoEvaluation eval = new PhotoEvaluation();
            eval.distance = CurrentTargetDistance;
            eval.angleOffset = CurrentAngleOffset;
            eval.syncScore = coopSyncPercentage;

            // 1. Lighting check (Flash active within 0.5s)
            if (!IsFlashActive)
            {
                eval.isNoLighting = true;
                eval.verdict = "NO LIGHTING";
                eval.details = "Flash inactive! P1 must activate flash first.";
                eval.score = 0;
                eval.stars = 0;
                return eval;
            }

            // 2. View check
            if (!IsTargetInCone || IsBlockedByWall || FocusedSubject == null)
            {
                eval.isFramed = false;
                eval.isBlocked = IsBlockedByWall;
                eval.verdict = "OUT OF VIEW";
                eval.details = "Target out of camera view or blocked by wall.";
                eval.score = 0;
                eval.stars = 0;
                return eval;
            }

            eval.isFramed = true;
            eval.subjectType = FocusedSubject.Type;
            eval.subjectId = FocusedSubject.SubjectId;
            eval.subjectName = FocusedSubject.DisplayName;

            // 3. Distance checks
            if (CurrentTargetDistance < 1.4f)
            {
                eval.isTooClose = true;
                eval.verdict = "TOO CLOSE";
                eval.details = "Too close! Step back to frame the subject.";
                eval.score = 0;
                eval.stars = 0;
                return eval;
            }

            if (CurrentTargetDistance > 6.0f)
            {
                eval.isTooFar = true;
                eval.verdict = "TOO FAR";
                eval.details = "Too far! Move closer for a clear view.";
                eval.score = 0;
                eval.stars = 0;
                return eval;
            }

            // 4. Similarity check
            if (WeddingGameManager.Instance != null &&
                WeddingGameManager.Instance.CheckIsTooSimilar(FocusedSubject.Type, FocusedSubject.SubjectId, _lensTransform.position, _lensTransform.forward))
            {
                eval.isTooSimilar = true;
                eval.verdict = "TOO SIMILAR";
                eval.details = "Too similar to a prior shot! Change angle or position.";
                eval.score = 0;
                eval.stars = 0;
                return eval;
            }

            // 5. Success!
            float angleFactor = Mathf.Clamp01(1f - (CurrentAngleOffset / (_fovAngle * 0.5f)));
            float distFactor = Mathf.Clamp01(1f - (Mathf.Abs(CurrentTargetDistance - 3.2f) / 3.0f));

            int baseScore = 400;
            int angleScore = Mathf.RoundToInt(angleFactor * 350f);
            int distScore = Mathf.RoundToInt(distFactor * 250f);
            eval.score = baseScore + angleScore + distScore;

            if (eval.score >= 880)
            {
                eval.stars = 3;
                eval.verdict = "GOOD"; // User requested: "just show if it is good, out of view, too far, too close or no lighting"
            }
            else if (eval.score >= 680)
            {
                eval.stars = 2;
                eval.verdict = "GOOD";
            }
            else
            {
                eval.stars = 1;
                eval.verdict = "GOOD";
            }

            eval.details = $"+{eval.score} pts  |  {FocusedSubject.DisplayName}";

            // Record photo in WeddingGameManager
            if (WeddingGameManager.Instance != null)
            {
                WeddingGameManager.Instance.RecordSuccessfulPhoto(
                    FocusedSubject.Type,
                    FocusedSubject.SubjectId,
                    _lensTransform.position,
                    _lensTransform.forward,
                    eval.score);
            }

            return eval;
        }

        private void UpdateFrustumVisuals()
        {
            if (_frustumMeshFilter == null || _lensTransform == null) return;

            int rayCount = 28;
            Vector3 lensPos = _lensTransform.position;
            float floorY = 0.03f;
            Vector3 originFloor = new Vector3(lensPos.x, floorY, lensPos.z);

            Vector3[] vertices = new Vector3[rayCount + 2];
            int[] triangles = new int[rayCount * 3];
            Vector2[] uvs = new Vector2[rayCount + 2];

            Vector3 localOrigin = _frustumMeshFilter.transform.InverseTransformPoint(originFloor);
            vertices[0] = localOrigin;
            uvs[0] = new Vector2(0.5f, 0f);

            Vector3[] linePoints = new Vector3[rayCount + 3];
            linePoints[0] = originFloor;

            float halfFov = _fovAngle * 0.5f;
            float angleStep = _fovAngle / rayCount;

            Vector3 flatForward = new Vector3(_lensTransform.forward.x, 0f, _lensTransform.forward.z).normalized;

            for (int i = 0; i <= rayCount; i++)
            {
                float angle = -halfFov + i * angleStep;
                Quaternion rot = Quaternion.AngleAxis(angle, Vector3.up);
                Vector3 dir = rot * flatForward;

                float hitDist = _maxRange;
                RaycastHit hit;
                // Cast ray near floor level to truncate against walls
                Vector3 rayStart = new Vector3(lensPos.x, 0.5f, lensPos.z);
                if (Physics.Raycast(rayStart, dir, out hit, _maxRange))
                {
                    if (hit.collider.CompareTag("Obstacle") || hit.collider.CompareTag("Wall") || hit.collider.gameObject.name.Contains("Wall"))
                    {
                        hitDist = Mathf.Max(0.5f, hit.distance);
                    }
                }

                Vector3 endPointWorld = originFloor + dir * hitDist;
                vertices[i + 1] = _frustumMeshFilter.transform.InverseTransformPoint(endPointWorld);
                uvs[i + 1] = new Vector2((float)i / rayCount, 1f);

                linePoints[i + 1] = endPointWorld;

                if (i < rayCount)
                {
                    triangles[i * 3] = 0;
                    triangles[i * 3 + 1] = i + 1;
                    triangles[i * 3 + 2] = i + 2;
                }
            }

            linePoints[rayCount + 2] = originFloor; // Close line loop

            if (_frustumMesh == null)
            {
                _frustumMesh = new Mesh();
                _frustumMesh.name = "FrustumMesh";
                _frustumMeshFilter.mesh = _frustumMesh;
            }

            _frustumMesh.Clear();
            _frustumMesh.vertices = vertices;
            _frustumMesh.triangles = triangles;
            _frustumMesh.uv = uvs;
            _frustumMesh.RecalculateNormals();

            // Color choice
            Color targetColor = _colorSearching;
            if (IsTargetInCone)
            {
                if (IsBlockedByWall)
                {
                    targetColor = _colorBlocked;
                }
                else if (IsLineOfSightClear)
                {
                    // Pulse gold/green
                    float pulse = (Mathf.Sin(Time.time * 6f) + 1f) * 0.5f;
                    targetColor = Color.Lerp(_colorFramed, new Color(1f, 0.78f, 0.23f, 0.7f), pulse * 0.4f);
                }
            }

            if (_frustumMeshRenderer != null && _frustumMeshRenderer.material != null)
            {
                _frustumMeshRenderer.material.color = targetColor;
            }

            if (_frustumLineRenderer != null)
            {
                _frustumLineRenderer.positionCount = linePoints.Length;
                _frustumLineRenderer.SetPositions(linePoints);
                Color borderCol = targetColor;
                borderCol.a = 0.9f;
                _frustumLineRenderer.startColor = borderCol;
                _frustumLineRenderer.endColor = borderCol;
            }
        }

        private IEnumerator SnapRoutine()
        {
            _isSnapping = true;

            // Audio
            if (AudioFeedback.Instance != null)
            {
                AudioFeedback.Instance.PlayShutterClick();
                AudioFeedback.Instance.PlayFlashPop();
            }

            // Flash light
            if (_flashLight != null)
            {
                _flashLight.enabled = true;
                _flashLight.intensity = 18f;
            }

            // Recoil transform
            Vector3 originalLocalPos = transform.localPosition;
            Vector3 recoilLocalPos = originalLocalPos - transform.forward * 0.25f;

            if (_bellowsTransform != null)
            {
                _bellowsTransform.localScale = new Vector3(_originalBellowsScale.x * 1.15f, _originalBellowsScale.y, _originalBellowsScale.z * 0.6f);
            }

            // Render capture camera
            if (_lensCaptureCamera != null)
            {
                _lensCaptureCamera.Render();
            }

            // Hitstop: brief freeze
            float originalTimeScale = Time.timeScale;
            Time.timeScale = 0.02f;
            yield return new WaitForSecondsRealtime(0.045f);
            Time.timeScale = originalTimeScale;

            // Spring return for camera & bellows
            float elapsed = 0f;
            float duration = 0.22f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                transform.localPosition = Vector3.Lerp(recoilLocalPos, originalLocalPos, Mathf.SmoothStep(0f, 1f, t));
                if (_bellowsTransform != null)
                {
                    _bellowsTransform.localScale = Vector3.Lerp(_bellowsTransform.localScale, _originalBellowsScale, t);
                }
                if (_flashLight != null)
                {
                    _flashLight.intensity = Mathf.Lerp(18f, 0f, t);
                }
                yield return null;
            }

            transform.localPosition = originalLocalPos;
            if (_bellowsTransform != null) _bellowsTransform.localScale = _originalBellowsScale;
            if (_flashLight != null) _flashLight.enabled = false;

            _isSnapping = false;
        }

        public RenderTexture GetPhotoTexture() => _photoRenderTexture;
    }
}
