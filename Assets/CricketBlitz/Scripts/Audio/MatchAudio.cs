using UnityEngine;

namespace CricketBlitz.Audio
{
    /// <summary>
    /// Short procedural clips so the match has bat, stumps and crowd
    /// before any audio files are imported. Drop real clips on the fields later.
    /// </summary>
    public sealed class MatchAudio : MonoBehaviour
    {
        public static MatchAudio Instance { get; private set; }

        public AudioClip BatClip;
        public AudioClip CheerClip;
        public AudioClip StumpClip;
        public AudioClip ClickClip;
        public AudioClip CrowdClip;

        AudioSource sfx;
        AudioSource crowd;
        float music = 0.55f;
        float volume = 0.9f;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            sfx = gameObject.AddComponent<AudioSource>();
            sfx.playOnAwake = false;
            sfx.spatialBlend = 0f;
            crowd = gameObject.AddComponent<AudioSource>();
            crowd.playOnAwake = false;
            crowd.loop = true;
            crowd.spatialBlend = 0f;
            EnsureClips();
        }

        void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void ApplyVolumes(float musicVolume, float sfxVolume)
        {
            music = Mathf.Clamp01(musicVolume);
            volume = Mathf.Clamp01(sfxVolume);
            if (crowd != null)
            {
                crowd.volume = music * 0.28f;
            }
        }

        public void StartCrowd()
        {
            EnsureClips();
            if (crowd.isPlaying || CrowdClip == null)
            {
                crowd.volume = music * 0.28f;
                return;
            }

            crowd.clip = CrowdClip;
            crowd.volume = music * 0.28f;
            crowd.Play();
        }

        public void StopCrowd()
        {
            if (crowd != null)
            {
                crowd.Stop();
            }
        }

        public void PlayBat()
        {
            Play(BatClip, 0.8f);
        }

        public void PlayCheer()
        {
            Play(CheerClip, 0.9f);
        }

        public void PlayStumps()
        {
            Play(StumpClip, 1f);
        }

        public void PlayClick()
        {
            Play(ClickClip, 0.45f);
        }

        void Play(AudioClip clip, float scale)
        {
            if (clip == null || sfx == null || volume <= 0.01f)
            {
                return;
            }

            sfx.PlayOneShot(clip, volume * scale);
        }

        void EnsureClips()
        {
            if (BatClip == null)
            {
                BatClip = Noise(0.07f, 1800f, 0.8f, 90f);
            }

            if (CheerClip == null)
            {
                CheerClip = Noise(0.55f, 600f, 0.45f, 8f);
            }

            if (StumpClip == null)
            {
                StumpClip = Noise(0.16f, 180f, 1f, 40f);
            }

            if (ClickClip == null)
            {
                ClickClip = Noise(0.04f, 900f, 0.35f, 20f);
            }

            if (CrowdClip == null)
            {
                CrowdClip = Noise(1.2f, 420f, 0.22f, 6f);
            }
        }

        static AudioClip Noise(float seconds, float frequency, float amplitude, float grit)
        {
            int rate = 22050;
            int samples = Mathf.Max(1, Mathf.RoundToInt(seconds * rate));
            float[] data = new float[samples];
            float phase = 0f;
            uint noise = 123456789u;
            for (int i = 0; i < samples; i++)
            {
                float t = i / (float)samples;
                float envelope = Mathf.Sin(t * Mathf.PI);
                phase += frequency / rate;
                noise = 1664525u * noise + 1013904223u;
                float white = ((noise >> 8) & 255) / 255f * 2f - 1f;
                float tone = Mathf.Sin(phase * Mathf.PI * 2f);
                data[i] = (tone * 0.65f + white * (grit / 100f)) * envelope * amplitude;
            }

            AudioClip clip = AudioClip.Create("blitz-tone", samples, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
