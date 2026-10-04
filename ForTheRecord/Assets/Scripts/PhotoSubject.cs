using System.Collections.Generic;
using UnityEngine;

namespace CameraCoop
{
    public enum SubjectType
    {
        Food,
        Guest,
        Bride,
        Champagne,
        SomethingRed,
        MusicBand,
        PicnicPlace,
        Tiger
    }

    public class PhotoSubject : MonoBehaviour
    {
        public static readonly List<PhotoSubject> AllSubjects = new List<PhotoSubject>();

        [Header("Subject Info")]
        [SerializeField] private SubjectType _subjectType;
        [SerializeField] private string _subjectId;
        [SerializeField] private string _displayName;
        [SerializeField] private Transform _focusPoint;
        [SerializeField] private float _focusRadius = 0.8f;

        public SubjectType Type => _subjectType;
        public string SubjectId => string.IsNullOrEmpty(_subjectId) ? gameObject.name : _subjectId;
        public string DisplayName => string.IsNullOrEmpty(_displayName) ? gameObject.name : _displayName;
        public Transform FocusTransform => _focusPoint != null ? _focusPoint : transform;
        public float FocusRadius => _focusRadius;

        public void Initialize(SubjectType type, string id, string displayName, Transform focus = null)
        {
            _subjectType = type;
            _subjectId = id;
            _displayName = displayName;
            _focusPoint = focus != null ? focus : transform;
        }

        private void OnEnable()
        {
            if (!AllSubjects.Contains(this))
            {
                AllSubjects.Add(this);
            }
        }

        private void OnDisable()
        {
            AllSubjects.Remove(this);
        }

        private void OnDestroy()
        {
            AllSubjects.Remove(this);
        }
    }
}
