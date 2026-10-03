using UnityEngine;

namespace CameraCoop
{
    public class SadSmileyPopup : MonoBehaviour
    {
        [Header("Animation Settings")]
        [SerializeField] private float _duration = 1.0f;
        [SerializeField] private float _targetScale = 0.75f;
        [SerializeField] private float _hoverRiseSpeed = 0.35f;
        [SerializeField] private float _popInDuration = 0.15f;
        [SerializeField] private float _popOutDuration = 0.15f;

        private float _elapsed = 0f;
        private Vector3 _startPosition;
        private Material _instancedMat;
        private MeshRenderer _renderer;

        public static SadSmileyPopup Spawn(Vector3 worldPosition)
        {
            GameObject prefab = Resources.Load<GameObject>("SadSmileyPopup");
            GameObject go;
            if (prefab != null)
            {
                go = Object.Instantiate(prefab, worldPosition, Quaternion.identity);
            }
            else
            {
                // Fallback procedural creation of Quad plane
                go = GameObject.CreatePrimitive(PrimitiveType.Quad);
                go.name = "SadSmileyPopup";
                var col = go.GetComponent<Collider>();
                if (col != null) Object.Destroy(col);

                Material mat = Resources.Load<Material>("SadSmileyMat");
#if UNITY_EDITOR
                if (mat == null)
                {
                    mat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/SadSmileyMat.mat");
                }
#endif
                var mr = go.GetComponent<MeshRenderer>();
                if (mr != null && mat != null)
                {
                    mr.material = mat;
                }
                go.transform.position = worldPosition;
                go.AddComponent<SadSmileyPopup>();
            }

            var popup = go.GetComponent<SadSmileyPopup>();
            if (popup == null) popup = go.AddComponent<SadSmileyPopup>();
            popup.Initialize(worldPosition);
            return popup;
        }

        public void Initialize(Vector3 worldPosition)
        {
            _startPosition = worldPosition;
            transform.position = worldPosition;
            transform.localScale = Vector3.zero;

            _renderer = GetComponent<MeshRenderer>();
            if (_renderer != null)
            {
                _instancedMat = _renderer.material;
            }

            FaceCamera();
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;

            // Hover upward gently
            transform.position = _startPosition + Vector3.up * (_hoverRiseSpeed * _elapsed);

            // Scale animation: Pop in (0 -> 1) with bounce, hold, pop out (1 -> 0)
            float scaleFactor = 1f;
            if (_elapsed < _popInDuration)
            {
                float t = Mathf.Clamp01(_elapsed / _popInDuration);
                // Overshoot bounce easing
                scaleFactor = Mathf.Sin(t * Mathf.PI * 0.5f) * 1.15f;
                if (t > 0.8f)
                {
                    scaleFactor = Mathf.Lerp(1.15f, 1f, (t - 0.8f) / 0.2f);
                }
            }
            else if (_elapsed > _duration - _popOutDuration)
            {
                float t = Mathf.Clamp01((_elapsed - (_duration - _popOutDuration)) / _popOutDuration);
                scaleFactor = Mathf.Lerp(1f, 0f, t);
            }

            transform.localScale = Vector3.one * (_targetScale * Mathf.Max(0f, scaleFactor));

            if (_elapsed >= _duration)
            {
                Destroy(gameObject);
            }
        }

        private void LateUpdate()
        {
            FaceCamera();
        }

        private void FaceCamera()
        {
            Camera cam = Camera.main;
            if (cam != null)
            {
                transform.rotation = cam.transform.rotation;
            }
        }

        private void OnDestroy()
        {
            if (_instancedMat != null)
            {
                Destroy(_instancedMat);
            }
        }
    }
}
