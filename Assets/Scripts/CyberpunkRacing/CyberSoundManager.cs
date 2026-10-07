using UnityEngine;

namespace CyberpunkRacing
{
    /// <summary>
    /// Pengelola Audio Efek Suara (SFX) & Musik Balap Cyberpunk.
    /// Dilengkapi sintesis suara otomatis bila aset audio eksternal tidak ditemukan.
    /// </summary>
    public class CyberSoundManager : MonoBehaviour
    {
        public static CyberSoundManager Instance { get; private set; }

        public AudioClip externalChimeClip;
        public AudioClip externalWinClip;

        private AudioSource _sfxSource;
        private AudioClip _synthChime;
        private AudioClip _synthWinFanfare;
        private AudioClip _synthCountdownBeep;
        private AudioClip _synthGoBeep;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            _sfxSource = gameObject.AddComponent<AudioSource>();
            _sfxSource.playOnAwake = false;

            GenerateProceduralSounds();
        }

        public void PlayDataChime()
        {
            if (externalChimeClip != null)
                _sfxSource.PlayOneShot(externalChimeClip, 0.9f);
            else if (_synthChime != null)
                _sfxSource.PlayOneShot(_synthChime, 0.85f);
        }

        public void PlayCountdownBeep(bool isGo)
        {
            if (isGo && _synthGoBeep != null)
                _sfxSource.PlayOneShot(_synthGoBeep, 1f);
            else if (_synthCountdownBeep != null)
                _sfxSource.PlayOneShot(_synthCountdownBeep, 0.9f);
        }

        public void PlayVictory()
        {
            if (externalWinClip != null)
                _sfxSource.PlayOneShot(externalWinClip, 1f);
            else if (_synthWinFanfare != null)
                _sfxSource.PlayOneShot(_synthWinFanfare, 0.95f);
        }

        private void GenerateProceduralSounds()
        {
            _synthChime = CreateTone(880f, 0.18f, true);
            _synthCountdownBeep = CreateTone(520f, 0.14f, false);
            _synthGoBeep = CreateTone(1040f, 0.35f, false);
            _synthWinFanfare = CreateChord(new float[] { 523.25f, 659.25f, 783.99f, 1046.50f }, 1.2f);
        }

        private static AudioClip CreateTone(float freq, float duration, bool harmonic)
        {
            int sampleRate = 44100;
            int samplesCount = Mathf.CeilToInt(sampleRate * duration);
            float[] data = new float[samplesCount];

            for (int i = 0; i < samplesCount; i++)
            {
                float t = (float)i / sampleRate;
                float envelope = Mathf.Clamp01(1f - (t / duration));
                float sample = Mathf.Sin(2f * Mathf.PI * freq * t);
                if (harmonic)
                {
                    sample += 0.5f * Mathf.Sin(4f * Mathf.PI * freq * t);
                }
                data[i] = sample * envelope * 0.4f;
            }

            var clip = AudioClip.Create("Tone_" + freq, samplesCount, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip CreateChord(float[] freqs, float duration)
        {
            int sampleRate = 44100;
            int samplesCount = Mathf.CeilToInt(sampleRate * duration);
            float[] data = new float[samplesCount];

            for (int i = 0; i < samplesCount; i++)
            {
                float t = (float)i / sampleRate;
                float envelope = Mathf.Clamp01(1f - (t / duration));
                float sum = 0f;
                foreach (var f in freqs)
                {
                    sum += Mathf.Sin(2f * Mathf.PI * f * t);
                }
                data[i] = (sum / freqs.Length) * envelope * 0.45f;
            }

            var clip = AudioClip.Create("FanfareChord", samplesCount, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
