using UnityEngine;

namespace CameraCoop
{
    [System.Serializable]
    public struct PhotoEvaluation
    {
        public bool isFramed;
        public bool isBlocked;
        public float distance;
        public float angleOffset;
        public int score;
        public int stars;
        public string verdict;
        public string details;
        public float syncScore; // Cooperative sync percentage
    }
}
