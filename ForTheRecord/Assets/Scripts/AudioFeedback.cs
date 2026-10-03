using UnityEngine;

namespace CameraCoop
{
    [RequireComponent(typeof(AudioSource))]
    public class AudioFeedback : MonoBehaviour
    {
        public static AudioFeedback Instance { get; private set; }

        private AudioSource _audioSource;
        private AudioClip _shutterClip;
        private AudioClip _flashClip;
        private AudioClip _successChime;
        private AudioClip _failBuzz;
        private AudioClip _alignTick;
        private AudioClip _cardSlideClip;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            _audioSource = GetComponent<AudioSource>();
            _audioSource.playOnAwake = false;
            _audioSource.spatialBlend = 0f; // 2D clean stereo

            GenerateAudioClips();
        }

        private void GenerateAudioClips()
        {
            _shutterClip = CreateShutterClickClip();
            _flashClip = CreateFlashPopClip();
            _successChime = CreateSuccessChimeClip();
            _failBuzz = CreateFailBuzzClip();
            _alignTick = CreateAlignTickClip();
            _cardSlideClip = CreateCardSlideClip();
        }

        public void PlayShutterClick()
        {
            if (_audioSource != null && _shutterClip != null)
                _audioSource.PlayOneShot(_shutterClip, 0.9f);
        }

        public void PlayFlashPop()
        {
            if (_audioSource != null && _flashClip != null)
                _audioSource.PlayOneShot(_flashClip, 1.0f);
        }

        public void PlaySuccessChime(int stars = 3)
        {
            if (_audioSource != null && _successChime != null)
            {
                float pitch = stars switch
                {
                    1 => 0.9f,
                    2 => 1.05f,
                    _ => 1.2f
                };
                _audioSource.pitch = pitch;
                _audioSource.PlayOneShot(_successChime, 0.85f);
                _audioSource.pitch = 1.0f;
            }
        }

        public void PlayFailBuzz()
        {
            if (_audioSource != null && _failBuzz != null)
                _audioSource.PlayOneShot(_failBuzz, 0.8f);
        }

        public void PlayAlignTick()
        {
            if (_audioSource != null && _alignTick != null)
                _audioSource.PlayOneShot(_alignTick, 0.5f);
        }

        public void PlayCardSlide()
        {
            if (_audioSource != null && _cardSlideClip != null)
                _audioSource.PlayOneShot(_cardSlideClip, 0.65f);
        }

        // --- Procedural synthesis helpers ---

        private AudioClip CreateShutterClickClip()
        {
            int sampleRate = 44100;
            float duration = 0.12f;
            int numSamples = Mathf.CeilToInt(sampleRate * duration);
            float[] samples = new float[numSamples];

            for (int i = 0; i < numSamples; i++)
            {
                float t = (float)i / sampleRate;
                // Double click: first click at t=0, second click at t=0.035
                float click1 = Mathf.Exp(-t * 220f) * Mathf.Sin(2f * Mathf.PI * 1800f * t);
                float click2 = 0f;
                if (t > 0.035f)
                {
                    float t2 = t - 0.035f;
                    click2 = Mathf.Exp(-t2 * 180f) * Mathf.Sin(2f * Mathf.PI * 1200f * t2) * 1.2f;
                }

                // Mechanical snap noise
                float noise = (Random.value * 2f - 1f) * Mathf.Exp(-t * 120f) * 0.35f;

                samples[i] = Mathf.Clamp(click1 + click2 + noise, -1f, 1f);
            }

            AudioClip clip = AudioClip.Create("ProceduralShutter", numSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip CreateFlashPopClip()
        {
            int sampleRate = 44100;
            float duration = 0.28f;
            int numSamples = Mathf.CeilToInt(sampleRate * duration);
            float[] samples = new float[numSamples];

            for (int i = 0; i < numSamples; i++)
            {
                float t = (float)i / sampleRate;
                // Explosive low burst + sizzling noise decay
                float pop = Mathf.Exp(-t * 40f) * Mathf.Sin(2f * Mathf.PI * 120f * Mathf.Exp(-t * 15f) * t);
                float sizzle = (Random.value * 2f - 1f) * Mathf.Exp(-t * 18f) * 0.5f;

                // High tone sizzle
                float highHiss = Mathf.Sin(2f * Mathf.PI * 4500f * t) * Mathf.Exp(-t * 30f) * 0.15f;

                samples[i] = Mathf.Clamp(pop * 0.7f + sizzle + highHiss, -1f, 1f);
            }

            AudioClip clip = AudioClip.Create("ProceduralFlash", numSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip CreateSuccessChimeClip()
        {
            int sampleRate = 44100;
            float duration = 0.65f;
            int numSamples = Mathf.CeilToInt(sampleRate * duration);
            float[] samples = new float[numSamples];

            // 3-note ascending arpeggio: C5 (523.25), E5 (659.25), G5 (783.99)
            float[] freqs = { 523.25f, 659.25f, 783.99f };
            float noteDelay = 0.12f;

            for (int i = 0; i < numSamples; i++)
            {
                float t = (float)i / sampleRate;
                float val = 0f;

                for (int n = 0; n < 3; n++)
                {
                    float noteStart = n * noteDelay;
                    if (t >= noteStart)
                    {
                        float tn = t - noteStart;
                        float env = Mathf.Exp(-tn * 6f);
                        // fundamental + overtone
                        float wave = Mathf.Sin(2f * Mathf.PI * freqs[n] * tn) * 0.7f +
                                     Mathf.Sin(2f * Mathf.PI * freqs[n] * 2f * tn) * 0.25f;
                        val += wave * env * 0.35f;
                    }
                }

                samples[i] = Mathf.Clamp(val, -1f, 1f);
            }

            AudioClip clip = AudioClip.Create("ProceduralSuccessChime", numSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip CreateFailBuzzClip()
        {
            int sampleRate = 44100;
            float duration = 0.3f;
            int numSamples = Mathf.CeilToInt(sampleRate * duration);
            float[] samples = new float[numSamples];

            for (int i = 0; i < numSamples; i++)
            {
                float t = (float)i / sampleRate;
                // Double dull square buzz
                float freq = 110f;
                float square = Mathf.Sign(Mathf.Sin(2f * Mathf.PI * freq * t));
                float env = Mathf.Exp(-t * 10f);
                if (t > 0.15f)
                {
                    float t2 = t - 0.15f;
                    env = Mathf.Max(env, Mathf.Exp(-t2 * 12f));
                }

                samples[i] = Mathf.Clamp(square * env * 0.4f, -1f, 1f);
            }

            AudioClip clip = AudioClip.Create("ProceduralFailBuzz", numSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip CreateAlignTickClip()
        {
            int sampleRate = 44100;
            float duration = 0.05f;
            int numSamples = Mathf.CeilToInt(sampleRate * duration);
            float[] samples = new float[numSamples];

            for (int i = 0; i < numSamples; i++)
            {
                float t = (float)i / sampleRate;
                float wave = Mathf.Sin(2f * Mathf.PI * 1400f * t) * Mathf.Exp(-t * 90f);
                samples[i] = Mathf.Clamp(wave * 0.5f, -1f, 1f);
            }

            AudioClip clip = AudioClip.Create("ProceduralAlignTick", numSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip CreateCardSlideClip()
        {
            int sampleRate = 44100;
            float duration = 0.25f;
            int numSamples = Mathf.CeilToInt(sampleRate * duration);
            float[] samples = new float[numSamples];

            for (int i = 0; i < numSamples; i++)
            {
                float t = (float)i / sampleRate;
                float noise = (Random.value * 2f - 1f) * Mathf.Sin(t / duration * Mathf.PI);
                float thump = t > 0.18f ? Mathf.Sin(2f * Mathf.PI * 80f * (t - 0.18f)) * Mathf.Exp(-(t - 0.18f) * 50f) * 0.6f : 0f;
                samples[i] = Mathf.Clamp(noise * 0.25f + thump, -1f, 1f);
            }

            AudioClip clip = AudioClip.Create("ProceduralCardSlide", numSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
