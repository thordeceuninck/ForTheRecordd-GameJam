using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace CameraCoop
{
    public class WeddingHUD : MonoBehaviour
    {
        public static WeddingHUD Instance { get; private set; }

        [Header("Todo List Panel (Top-Left)")]
        [SerializeField] private RectTransform _todoPanel;
        [SerializeField] private TextMeshProUGUI _todoTitleText;
        [SerializeField] private TextMeshProUGUI _foodItemText;
        [SerializeField] private TextMeshProUGUI _guestsItemText;
        [SerializeField] private TextMeshProUGUI _brideItemText;
        [SerializeField] private TextMeshProUGUI _champagneItemText;
        [SerializeField] private TextMeshProUGUI _redItemText;
        [SerializeField] private TextMeshProUGUI _bandItemText;
        [SerializeField] private TextMeshProUGUI _picnicItemText;
        [SerializeField] private TextMeshProUGUI _tigerItemText;

        [Header("Status & Timer Bar")]
        [SerializeField] private TextMeshProUGUI _timerText;
        [SerializeField] private TextMeshProUGUI _scoreText;
        [SerializeField] private TextMeshProUGUI _flashBadgeText;
        [SerializeField] private Image _flashBadgeBg;

        [Header("Game Over / Results Panel")]
        [SerializeField] private GameObject _gameOverPanel;
        [SerializeField] private TextMeshProUGUI _finalScoreText;
        [SerializeField] private TextMeshProUGUI _scoreBreakdownText;
        [SerializeField] private TextMeshProUGUI _missionResultsText;
        [SerializeField] private Button _restartButton;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (_gameOverPanel != null)
            {
                _gameOverPanel.SetActive(false);
            }

            if (_restartButton != null)
            {
                _restartButton.onClick.AddListener(OnRestartClicked);
            }
        }

        private void Start()
        {
            if (WeddingGameManager.Instance != null)
            {
                WeddingGameManager.Instance.OnStateChanged += RefreshHUD;
                WeddingGameManager.Instance.OnGameOver += ShowGameOver;
            }
            RefreshHUD();
        }

        private void OnDestroy()
        {
            if (WeddingGameManager.Instance != null)
            {
                WeddingGameManager.Instance.OnStateChanged -= RefreshHUD;
                WeddingGameManager.Instance.OnGameOver -= ShowGameOver;
            }
        }

        private void Update()
        {
            UpdateLiveHUD();
        }

        private void UpdateLiveHUD()
        {
            if (WeddingGameManager.Instance == null) return;

            // Timer
            if (_timerText != null)
            {
                int seconds = Mathf.CeilToInt(WeddingGameManager.Instance.TimeRemaining);
                _timerText.text = $"TIME: {seconds:D2}s";
                if (seconds <= 10)
                {
                    _timerText.color = new Color(1f, 0.35f, 0.3f);
                }
                else
                {
                    _timerText.color = new Color(1f, 0.95f, 0.85f);
                }
            }

            // Flash Badge
            if (GiantCamera.Instance != null && _flashBadgeText != null)
            {
                if (GiantCamera.Instance.IsFlashActive)
                {
                    float rem = GiantCamera.Instance.FlashTimeRemaining;
                    _flashBadgeText.text = $"FLASH ON: {rem:F1}s [P2 SHOOT!]";
                    _flashBadgeText.color = new Color(1f, 0.95f, 0.2f);
                    if (_flashBadgeBg != null) _flashBadgeBg.color = new Color(0.9f, 0.7f, 0.1f, 0.85f);
                }
                else
                {
                    _flashBadgeText.text = "P1: PRESS [A] FOR FLASH (0.5s)";
                    _flashBadgeText.color = new Color(0.9f, 0.88f, 0.82f);
                    if (_flashBadgeBg != null) _flashBadgeBg.color = new Color(0.18f, 0.16f, 0.15f, 0.75f);
                }
            }
        }

        public void RefreshHUD()
        {
            if (WeddingGameManager.Instance == null) return;

            var gm = WeddingGameManager.Instance;

            // Format Todo checklist items
            UpdateTodoLine(_foodItemText, "Food", gm.FoodCount, gm.TargetFood);
            UpdateTodoLine(_guestsItemText, "Different guests", gm.GuestCount, gm.TargetGuests);
            UpdateTodoLine(_brideItemText, "bride dancing", gm.BrideCount, gm.TargetBride);
            UpdateTodoLine(_champagneItemText, "champagne", gm.ChampagneCount, gm.TargetChampagne);

            UpdateTodoLine(_redItemText, "find something red", gm.SomethingRedCount, gm.TargetSomethingRed);
            UpdateTodoLine(_bandItemText, "music band", gm.MusicBandCount, gm.TargetMusicBand);
            UpdateTodoLine(_picnicItemText, "picknickplace", gm.PicnicCount, gm.TargetPicnic);
            UpdateTodoLine(_tigerItemText, "tiger", gm.TigerCount, gm.TargetTiger);

            // Net score
            if (_scoreText != null)
            {
                _scoreText.text = $"SCORE: {gm.NetScore} PTS   (BUMPS: -{gm.BumpPenalties})";
            }
        }

        private void UpdateTodoLine(TextMeshProUGUI label, string taskName, int current, int target)
        {
            if (label == null) return;
            bool done = current >= target;
            
            string colorHex = done ? "#52D372" : "#F6EEDF";
            label.text = $"<color={colorHex}>{current}/{target} {taskName}</color>";
        }

        private void ShowGameOver()
        {
            if (_gameOverPanel == null) return;
            _gameOverPanel.SetActive(true);

            if (WeddingGameManager.Instance == null) return;
            var gm = WeddingGameManager.Instance;

            if (_finalScoreText != null)
            {
                _finalScoreText.text = $"FINAL SCORE\n<size=54>{gm.NetScore}</size> PTS";
            }

            if (_scoreBreakdownText != null)
            {
                _scoreBreakdownText.text = $"Photos Score: +{gm.PhotoScore} pts\nBump Penalties: -{gm.BumpPenalties} pts";
            }

            if (_missionResultsText != null)
            {
                _missionResultsText.text =
                    $"Food: {gm.FoodCount}/{gm.TargetFood}\n" +
                    $"Different Guests: {gm.GuestCount}/{gm.TargetGuests}\n" +
                    $"Bride Dancing: {gm.BrideCount}/{gm.TargetBride}\n" +
                    $"Champagne: {gm.ChampagneCount}/{gm.TargetChampagne}\n" +
                    $"Something Red: {gm.SomethingRedCount}/{gm.TargetSomethingRed}\n" +
                    $"Music Band: {gm.MusicBandCount}/{gm.TargetMusicBand}\n" +
                    $"Picnic Place: {gm.PicnicCount}/{gm.TargetPicnic}\n" +
                    $"Tiger: {gm.TigerCount}/{gm.TargetTiger}\n\n" +
                    $"<b>Missions Finished: {gm.TotalMissionsCompleted}/8</b>";
            }
        }

        private void OnRestartClicked()
        {
            if (WeddingGameManager.Instance != null)
            {
                WeddingGameManager.Instance.RestartGame();
            }
        }
    }
}
