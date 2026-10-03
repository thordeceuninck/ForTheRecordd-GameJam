using UnityEngine;

namespace CameraCoop
{
    [System.Serializable]
    public struct PhotoEvaluation
    {
        public bool isFramed;
        public bool isBlocked;
        public bool isNoLighting;
        public bool isTooClose;
        public bool isTooFar;
        public bool isTooSimilar;

        public float distance;
        public float angleOffset;
        public int score;
        public int stars;

        public string verdict;      // "GOOD", "NO LIGHTING", "OUT OF VIEW", "TOO CLOSE", "TOO FAR", "TOO SIMILAR"
        public string details;
        public float syncScore;

        public SubjectType subjectType;
        public string subjectId;
        public string subjectName;
    }
}

