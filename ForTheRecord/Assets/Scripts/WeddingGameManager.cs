using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CameraCoop
{
    public class WeddingGameManager : MonoBehaviour
    {
        public static WeddingGameManager Instance { get; private set; }

        [Header("Mission Requirements")]
        [SerializeField] private int _targetFood = 2;
        [SerializeField] private int _targetGuests = 3;
        [SerializeField] private int _targetBride = 2;
        [SerializeField] private int _targetChampagne = 1;

        [Header("Match Settings")]
        [SerializeField] private float _gameDuration = 60f;

        // Current Mission Progress
        public int FoodCount { get; private set; } = 0;
        public int GuestCount { get; private set; } = 0;
        public int BrideCount { get; private set; } = 0;
        public int ChampagneCount { get; private set; } = 0;

        public int TargetFood => _targetFood;
        public int TargetGuests => _targetGuests;
        public int TargetBride => _targetBride;
        public int TargetChampagne => _targetChampagne;

        public int TotalMissionsCompleted =>
            (FoodCount >= _targetFood ? 1 : 0) +
            (GuestCount >= _targetGuests ? 1 : 0) +
            (BrideCount >= _targetBride ? 1 : 0) +
            (ChampagneCount >= _targetChampagne ? 1 : 0);

        // Scoring & Penalties
        [Header("Scoring & Penalties")]
        [SerializeField] private int _bumpPenalty = 500;

        public int PhotoScore { get; private set; } = 0;
        public int BumpPenalties { get; private set; } = 0;
        public int BumpPenaltyAmount => _bumpPenalty;
        public int NetScore => PhotoScore - BumpPenalties;

        // Timer
        public float TimeRemaining { get; private set; }
        public bool IsGameOver { get; private set; } = false;

        // Similarity & Guest tracking
        public struct TakenPhotoRecord
        {
            public SubjectType type;
            public string subjectId;
            public Vector3 cameraPosition;
            public Vector3 cameraForward;
        }

        private readonly List<TakenPhotoRecord> _recordedPhotos = new List<TakenPhotoRecord>();
        private readonly HashSet<string> _photographedGuestIds = new HashSet<string>();

        public event Action OnStateChanged;
        public event Action OnGameOver;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            TimeRemaining = _gameDuration;
            IsGameOver = false;
        }

        private void Update()
        {
            if (IsGameOver)
            {
                if (CoOpInputManager.Instance != null && CoOpInputManager.Instance.WasAnyActionPressed())
                {
                    RestartGame();
                }
                return;
            }

            TimeRemaining -= Time.deltaTime;
            if (TimeRemaining <= 0f)
            {
                TimeRemaining = 0f;
                TriggerGameOver();
            }
        }

        public void RegisterBump(string bumpedObjectName)
        {
            if (IsGameOver) return;
            BumpPenalties += _bumpPenalty;
            OnStateChanged?.Invoke();
        }

        public bool CheckIsTooSimilar(SubjectType type, string subjectId, Vector3 camPos, Vector3 camForward)
        {
            foreach (var rec in _recordedPhotos)
            {
                // If it's the same subject or same subject type
                if (rec.type == type)
                {
                    if (type == SubjectType.Guest && rec.subjectId != subjectId)
                    {
                        // Different guest is not similar
                        continue;
                    }

                    float dist = Vector3.Distance(rec.cameraPosition, camPos);
                    float angle = Vector3.Angle(rec.cameraForward, camForward);

                    // If camera position is within 2m AND angle is within 28 degrees
                    if (dist < 2.0f && angle < 28f)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public bool IsGuestAlreadyPhotographed(string guestId)
        {
            return _photographedGuestIds.Contains(guestId);
        }

        public bool RecordSuccessfulPhoto(SubjectType type, string subjectId, Vector3 camPos, Vector3 camForward, int pointsAwarded)
        {
            if (IsGameOver) return false;

            // Check if this objective can still accept photos
            bool acceptedForMission = false;

            switch (type)
            {
                case SubjectType.Food:
                    if (FoodCount < _targetFood)
                    {
                        FoodCount++;
                        acceptedForMission = true;
                    }
                    break;

                case SubjectType.Guest:
                    if (GuestCount < _targetGuests && !_photographedGuestIds.Contains(subjectId))
                    {
                        GuestCount++;
                        _photographedGuestIds.Add(subjectId);
                        acceptedForMission = true;
                    }
                    break;

                case SubjectType.Bride:
                    if (BrideCount < _targetBride)
                    {
                        BrideCount++;
                        acceptedForMission = true;
                    }
                    break;

                case SubjectType.Champagne:
                    if (ChampagneCount < _targetChampagne)
                    {
                        ChampagneCount++;
                        acceptedForMission = true;
                    }
                    break;
            }

            _recordedPhotos.Add(new TakenPhotoRecord
            {
                type = type,
                subjectId = subjectId,
                cameraPosition = camPos,
                cameraForward = camForward
            });

            PhotoScore += pointsAwarded;
            OnStateChanged?.Invoke();

            return acceptedForMission;
        }

        private void TriggerGameOver()
        {
            IsGameOver = true;
            OnGameOver?.Invoke();
            OnStateChanged?.Invoke();
        }

        public void RestartGame()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }
}
