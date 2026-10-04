using UnityEngine;

namespace CameraCoop
{
    public class MusicBandStage : MonoBehaviour
    {
        [Header("Musicians")]
        [SerializeField] private Transform _singer;
        [SerializeField] private Transform _guitarist;
        [SerializeField] private Transform _bassist;
        [SerializeField] private Transform _drummer;

        [Header("Drummer Props")]
        [SerializeField] private Transform _leftDrumstick;
        [SerializeField] private Transform _rightDrumstick;

        [Header("Tempo")]
        [SerializeField] private float _bpm = 110f;

        private Vector3 _singerBasePos;
        private Vector3 _guitarBasePos;
        private Vector3 _bassBasePos;

        private void Start()
        {
            if (_singer != null) _singerBasePos = _singer.localPosition;
            if (_guitarist != null) _guitarBasePos = _guitarist.localPosition;
            if (_bassist != null) _bassBasePos = _bassist.localPosition;
        }

        private void Update()
        {
            float beat = Time.time * (_bpm / 60f) * Mathf.PI * 2f;

            // Singer bobbing and swaying
            if (_singer != null)
            {
                float bob = Mathf.Abs(Mathf.Sin(beat * 0.5f)) * 0.04f;
                float sway = Mathf.Sin(beat * 0.25f) * 4f;
                _singer.localPosition = _singerBasePos + new Vector3(0f, bob, 0f);
                _singer.localRotation = Quaternion.Euler(0f, sway, 0f);
            }

            // Guitarist rocking
            if (_guitarist != null)
            {
                float bob = Mathf.Abs(Mathf.Sin(beat * 0.5f + 1f)) * 0.035f;
                float rock = Mathf.Sin(beat * 0.5f) * 6f;
                _guitarist.localPosition = _guitarBasePos + new Vector3(0f, bob, 0f);
                _guitarist.localRotation = Quaternion.Euler(rock * 0.5f, 15f + rock, 0f);
            }

            // Bassist groove
            if (_bassist != null)
            {
                float bob = Mathf.Abs(Mathf.Sin(beat * 0.5f + 2f)) * 0.035f;
                float rock = Mathf.Sin(beat * 0.25f) * 5f;
                _bassist.localPosition = _bassBasePos + new Vector3(0f, bob, 0f);
                _bassist.localRotation = Quaternion.Euler(0f, -15f + rock, 0f);
            }

            // Drummer arms beating
            if (_drummer != null)
            {
                float headBob = Mathf.Abs(Mathf.Sin(beat)) * 6f;
                _drummer.localRotation = Quaternion.Euler(headBob, 0f, 0f);
            }

            if (_leftDrumstick != null)
            {
                float tap = Mathf.Abs(Mathf.Sin(beat)) * 25f;
                _leftDrumstick.localRotation = Quaternion.Euler(tap - 15f, 0f, 0f);
            }
            if (_rightDrumstick != null)
            {
                float tap = Mathf.Abs(Mathf.Cos(beat)) * 25f;
                _rightDrumstick.localRotation = Quaternion.Euler(tap - 15f, 0f, 0f);
            }
        }

        public void SetupMusicians(Transform singer, Transform guitarist, Transform bassist, Transform drummer, Transform leftStick, Transform rightStick)
        {
            _singer = singer;
            _guitarist = guitarist;
            _bassist = bassist;
            _drummer = drummer;
            _leftDrumstick = leftStick;
            _rightDrumstick = rightStick;
            Start();
        }
    }
}
