using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace CameraCoop
{
    public class PhotoReviewUI : MonoBehaviour
    {
        public static PhotoReviewUI Instance { get; private set; }

        [Header("Screen Flash")]
        [SerializeField] private Image _screenFlashImage;

        [Header("In-Game Live HUD")]
        [SerializeField] private TextMeshProUGUI _liveStatusText;
        [SerializeField] private Image _liveStatusBackground;
        [SerializeField] private GameObject _actionPromptPill;
        [SerializeField] private TextMeshProUGUI _controlsPromptText;

        [Header("Polaroid Review Card")]
        [SerializeField] private RectTransform _cardRoot;
        [SerializeField] private RawImage _photoRawImage;
        [SerializeField] private TextMeshProUGUI _cardTitleText;
        [SerializeField] private TextMeshProUGUI _verdictText;
        [SerializeField] private TextMeshProUGUI _starsText;
        [SerializeField] private TextMeshProUGUI _detailsText;
        [SerializeField] private TextMeshProUGUI _metadataText;
        [SerializeField] private TextMeshProUGUI _dismissPromptText;

        public bool IsReviewOpen { get; private set; }

        private int _snapshotCount = 0;
        private Coroutine _cardAnimCoroutine;
        private Coroutine _flashCoroutine;

        private Vector2 _cardHiddenPos = new Vector2(500f, -400f);
        private Vector2 _cardVisiblePos = new Vector2(-40f, 40f);

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (_screenFlashImage != null)
            {
                _screenFlashImage.gameObject.SetActive(true);
                Color c = _screenFlashImage.color;
                c.a = 0f;
                _screenFlashImage.color = c;
            }

            if (_cardRoot != null)
            {
                _cardRoot.anchoredPosition = _cardHiddenPos;
                _cardRoot.localEulerAngles = new Vector3(0f, 0f, 15f);
                _cardRoot.gameObject.SetActive(false);
            }

            IsReviewOpen = false;
        }

        private void Update()
        {
            UpdateLiveHUD();
        }

        private void UpdateLiveHUD()
        {
            if (IsReviewOpen)
            {
                if (_liveStatusText != null) _liveStatusText.gameObject.SetActive(false);
                if (_liveStatusBackground != null) _liveStatusBackground.gameObject.SetActive(false);
                if (_actionPromptPill != null) _actionPromptPill.SetActive(false);
                return;
            }

            if (GiantCamera.Instance == null) return;

            if (_liveStatusText != null) _liveStatusText.gameObject.SetActive(true);
            if (_liveStatusBackground != null) _liveStatusBackground.gameObject.SetActive(true);

            if (GiantCamera.Instance.IsLineOfSightClear)
            {
                if (_liveStatusText != null)
                {
                    _liveStatusText.text = ">>> TARGET FRAMED & IN SIGHT! PRESS [A] / SPACE TO SNAP! <<<";
                    _liveStatusText.color = new Color(1f, 0.85f, 0.25f); // Vibrant gold
                }
                if (_liveStatusBackground != null)
                {
                    _liveStatusBackground.color = new Color(0.12f, 0.22f, 0.15f, 0.85f);
                }
                if (_actionPromptPill != null) _actionPromptPill.SetActive(true);
            }
            else if (GiantCamera.Instance.IsBlockedByWall)
            {
                if (_liveStatusText != null)
                {
                    _liveStatusText.text = "!! SIGHTLINE BLOCKED BY WALL! REPOSITION CAMERA! !!";
                    _liveStatusText.color = new Color(1f, 0.35f, 0.3f); // Terracotta red
                }
                if (_liveStatusBackground != null)
                {
                    _liveStatusBackground.color = new Color(0.28f, 0.10f, 0.10f, 0.85f);
                }
                if (_actionPromptPill != null) _actionPromptPill.SetActive(false);
            }
            else
            {
                if (_liveStatusText != null)
                {
                    _liveStatusText.text = "COORDINATE MOVEMENT TO AIM AT CENTERPIECE EXHIBIT";
                    _liveStatusText.color = new Color(0.9f, 0.86f, 0.78f); // Ivory
                }
                if (_liveStatusBackground != null)
                {
                    _liveStatusBackground.color = new Color(0.15f, 0.13f, 0.12f, 0.75f);
                }
                if (_actionPromptPill != null) _actionPromptPill.SetActive(false);
            }
        }

        public void TriggerScreenFlash()
        {
            if (_flashCoroutine != null) StopCoroutine(_flashCoroutine);
            _flashCoroutine = StartCoroutine(ScreenFlashRoutine());
        }

        private IEnumerator ScreenFlashRoutine()
        {
            if (_screenFlashImage == null) yield break;

            _screenFlashImage.color = new Color(1f, 1f, 1f, 1f);
            yield return new WaitForSecondsRealtime(0.05f);

            float elapsed = 0f;
            float duration = 0.25f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / duration;
                _screenFlashImage.color = new Color(1f, 0.98f, 0.9f, Mathf.Lerp(1f, 0f, t * t));
                yield return null;
            }

            _screenFlashImage.color = new Color(1f, 1f, 1f, 0f);
        }

        public void ShowReview(PhotoEvaluation eval, RenderTexture photoRT)
        {
            _snapshotCount++;
            TriggerScreenFlash();

            if (AudioFeedback.Instance != null)
            {
                AudioFeedback.Instance.PlayCardSlide();
            }

            if (_photoRawImage != null && photoRT != null)
            {
                _photoRawImage.texture = photoRT;
            }

            if (_cardTitleText != null)
            {
                _cardTitleText.text = $"POLAROID EXPOSURE #{_snapshotCount:D2}";
            }

            if (_verdictText != null)
            {
                _verdictText.text = eval.verdict;
                if (eval.stars == 3) _verdictText.color = new Color(0.25f, 0.75f, 0.35f);
                else if (eval.stars >= 1) _verdictText.color = new Color(0.9f, 0.7f, 0.15f);
                else _verdictText.color = new Color(0.85f, 0.25f, 0.25f);
            }

            if (_starsText != null)
            {
                _starsText.text = eval.stars switch
                {
                    3 => "[ * * * ]  PERFECT!",
                    2 => "[ * * - ]  GREAT!",
                    1 => "[ * - - ]  FAIR",
                    _ => "[ - - - ]  MISS"
                };
            }

            if (_detailsText != null)
            {
                _detailsText.text = $"{eval.details}\nScore: {eval.score} pts  |  Dist: {eval.distance:F1}m  |  Angle: {eval.angleOffset:F0}°";
            }

            if (_metadataText != null)
            {
                _metadataText.text = $"f/2.8   1/250s   ISO 100   CO-OP SYNC: {eval.syncScore:F0}%";
            }

            if (_dismissPromptText != null)
            {
                _dismissPromptText.text = "Press [A] on Gamepad or Space/Enter to Continue";
            }

            if (_cardAnimCoroutine != null) StopCoroutine(_cardAnimCoroutine);
            _cardAnimCoroutine = StartCoroutine(SlideInCardRoutine());

            IsReviewOpen = true;
        }

        public void DismissReview()
        {
            if (!IsReviewOpen) return;
            if (_cardAnimCoroutine != null) StopCoroutine(_cardAnimCoroutine);
            _cardAnimCoroutine = StartCoroutine(SlideOutCardRoutine());
            IsReviewOpen = false;
        }

        private IEnumerator SlideInCardRoutine()
        {
            if (_cardRoot == null) yield break;

            _cardRoot.gameObject.SetActive(true);
            _cardRoot.anchoredPosition = _cardHiddenPos;
            _cardRoot.localEulerAngles = new Vector3(0f, 0f, 15f);

            float elapsed = 0f;
            float duration = 0.4f;
            Vector2 targetPos = _cardVisiblePos;
            Vector2 overshootPos = targetPos + new Vector2(-25f, 25f);
            float targetTilt = -3.5f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                // Overshoot cubic curve
                Vector2 currentPos;
                if (t < 0.7f)
                {
                    float p = t / 0.7f;
                    currentPos = Vector2.Lerp(_cardHiddenPos, overshootPos, Mathf.SmoothStep(0f, 1f, p));
                }
                else
                {
                    float p = (t - 0.7f) / 0.3f;
                    currentPos = Vector2.Lerp(overshootPos, targetPos, Mathf.SmoothStep(0f, 1f, p));
                }

                _cardRoot.anchoredPosition = currentPos;
                _cardRoot.localEulerAngles = new Vector3(0f, 0f, Mathf.Lerp(15f, targetTilt, t));
                yield return null;
            }

            _cardRoot.anchoredPosition = targetPos;
            _cardRoot.localEulerAngles = new Vector3(0f, 0f, targetTilt);
        }

        private IEnumerator SlideOutCardRoutine()
        {
            if (_cardRoot == null) yield break;

            float elapsed = 0f;
            float duration = 0.25f;
            Vector2 startPos = _cardRoot.anchoredPosition;
            float startTilt = _cardRoot.localEulerAngles.z;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                _cardRoot.anchoredPosition = Vector2.Lerp(startPos, _cardHiddenPos, t * t);
                _cardRoot.localEulerAngles = new Vector3(0f, 0f, Mathf.Lerp(startTilt, 15f, t));
                yield return null;
            }

            _cardRoot.gameObject.SetActive(false);
        }
    }
}
