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
        [SerializeField] private float _minRange = 1.0f;
        [SerializeField] private LayerMask _obstacleMask = ~0; // Will be set to Obstacle layer or default

        [Header("Flash & Recoil")]
        [SerializeField] private Light _flashLight;
        [SerializeField] private Transform _bellowsTransform;
        [SerializeField] private Transform _flashBulbTransform;

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
                _lensCaptureCamera.enabled = false; // Render on demand
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
            UpdateOpticalEvaluation();
            UpdateFrustumVisuals();
        }

        public void SetTarget(Transform target)
        {
            _targetTransform = target;
        }

        public void UpdateOpticalEvaluation()
        {
            if (_lensTransform == null || _targetTransform == null)
            {
                IsTargetInCone = false;
                IsLineOfSightClear = false;
                IsBlockedByWall = false;
                return;
            }

            Vector3 lensPos = _lensTransform.position;
            Vector3 targetPos = _targetTransform.position;

            // Project to horizontal plane
            Vector3 toTarget = targetPos - lensPos;
            Vector3 flatToTarget = new Vector3(toTarget.x, 0f, toTarget.z);
            CurrentTargetDistance = flatToTarget.magnitude;

            Vector3 flatForward = new Vector3(_lensTransform.forward.x, 0f, _lensTransform.forward.z).normalized;
            CurrentAngleOffset = Vector3.Angle(flatForward, flatToTarget.normalized);

            // Within FOV cone and distance range?
            bool inConeAngle = CurrentAngleOffset <= (_fovAngle * 0.5f);
            bool inRange = CurrentTargetDistance >= _minRange && CurrentTargetDistance <= _maxRange;

            IsTargetInCone = inConeAngle && inRange;

            if (IsTargetInCone)
            {
                // Test line-of-sight with center and lateral rays to target
                Vector3 targetCenter = targetPos + Vector3.up * 0.8f;
                Vector3 targetLeft = targetCenter - _targetTransform.right * 0.4f;
                Vector3 targetRight = targetCenter + _targetTransform.right * 0.4f;

                Vector3[] testPoints = { targetCenter, targetLeft, targetRight };
                int blockedRays = 0;

                foreach (var pt in testPoints)
                {
                    Vector3 rayDir = (pt - lensPos).normalized;
                    float testDist = Vector3.Distance(lensPos, pt);

                    RaycastHit[] hits = Physics.RaycastAll(lensPos, rayDir, testDist);
                    System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

                    bool hitWall = false;
                    foreach (var hit in hits)
                    {
                        if (hit.collider.transform.IsChildOf(transform.root)) continue;

                        if (hit.collider.CompareTag("Obstacle") || hit.collider.CompareTag("Wall") || hit.collider.gameObject.name.Contains("Wall"))
                        {
                            hitWall = true;
                            break;
                        }
                    }

                    if (hitWall) blockedRays++;
                }

                if (blockedRays >= testPoints.Length)
                {
                    IsBlockedByWall = true;
                    IsLineOfSightClear = false;
                }
                else
                {
                    IsBlockedByWall = false;
                    IsLineOfSightClear = true;
                }
            }
            else
            {
                IsBlockedByWall = false;
                IsLineOfSightClear = false;
            }
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

            if (!IsTargetInCone)
            {
                eval.isFramed = false;
                eval.isBlocked = false;
                eval.score = 0;
                eval.stars = 0;
                eval.verdict = "MISSED! TARGET NOT IN FRAME";
                eval.details = "Aim the camera cone towards the centerpiece!";
            }
            else if (IsBlockedByWall)
            {
                eval.isFramed = true;
                eval.isBlocked = true;
                eval.score = 150;
                eval.stars = 0;
                eval.verdict = "BLOCKED BY WALL!";
                eval.details = "A gallery partition obstructed the lens line-of-sight.";
            }
            else
            {
                eval.isFramed = true;
                eval.isBlocked = false;

                // Angle accuracy: 0 deg = 1.0, halfFov = 0.0
                float angleFactor = Mathf.Clamp01(1f - (CurrentAngleOffset / (_fovAngle * 0.5f)));

                // Ideal focal distance: 3.5m
                float distFactor = Mathf.Clamp01(1f - (Mathf.Abs(CurrentTargetDistance - 3.5f) / 3.5f));

                int baseScore = 300;
                int angleScore = Mathf.RoundToInt(angleFactor * 450f);
                int distScore = Mathf.RoundToInt(distFactor * 250f);

                eval.score = baseScore + angleScore + distScore;

                if (eval.score >= 820)
                {
                    eval.stars = 3;
                    eval.verdict = "PERFECT SHOT! [ * * * ]";
                    eval.details = $"Bullseye framing (+{angleScore}) & prime focal distance (+{distScore})";
                }
                else if (eval.score >= 600)
                {
                    eval.stars = 2;
                    eval.verdict = "GREAT SHOT! [ * * - ]";
                    eval.details = $"Good framing (+{angleScore}) & clear sightline (+{distScore})";
                }
                else
                {
                    eval.stars = 1;
                    eval.verdict = "FAIR SHOT! [ * - - ]";
                    eval.details = $"On target (+{angleScore}), try centering closer to 3.5m";
                }
            }

            return eval;
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
