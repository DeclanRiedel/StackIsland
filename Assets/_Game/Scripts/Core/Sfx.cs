using System.Collections.Generic;
using UnityEngine;

namespace StackIsland
{
    /// <summary>All sounds are synthesised at startup (no audio files): pops, coins, swings, hits, chimes, waves.</summary>
    public class Sfx : MonoBehaviour
    {
        public static Sfx I;
        const int Rate = 44100;

        public AudioClip Pop, Drop, Coin, Swing, Hit, Buy, Tick;
        AudioClip _waves;
        readonly List<AudioSource> _src = new List<AudioSource>();
        AudioSource _ambient;
        int _next;
        public float Volume = 1f;
        readonly Dictionary<AudioClip, float> _last = new Dictionary<AudioClip, float>();

        void Awake()
        {
            I = this;
            Pop = Synth("pop", 0.08f, t => Env(t, 0.002f, 0.07f) * Mathf.Sin(2 * Mathf.PI * (520f - 2400f * t) * t) * 0.6f);
            Drop = Synth("drop", 0.09f, t => Env(t, 0.002f, 0.08f) * Mathf.Sin(2 * Mathf.PI * (380f + 1500f * t) * t) * 0.55f);
            Coin = Synth("coin", 0.14f, t => Env(t, 0.004f, 0.13f) * Sq(t * (t < 0.05f ? 1318f : 1760f)) * 0.22f);
            Tick = Synth("tick", 0.05f, t => Env(t, 0.001f, 0.045f) * Mathf.Sin(2 * Mathf.PI * 880f * t) * 0.4f);
            Buy = Synth("buy", 0.6f, t => Chord(t, new[] { 523.25f, 659.25f, 783.99f, 1046.5f }, 0.07f) * Env(t, 0.005f, 0.58f) * 0.35f);
            var rng = new System.Random(3);
            Swing = Synth("swing", 0.14f, t => ((float)rng.NextDouble() * 2 - 1) * Env(t, 0.03f, 0.1f) * 0.35f * Mathf.Sin(t / 0.14f * Mathf.PI));
            Hit = Synth("hit", 0.12f, t => (Mathf.Sin(2 * Mathf.PI * (180f - 600f * t) * t) * 0.7f + ((float)rng.NextDouble() * 2 - 1) * 0.3f) * Env(t, 0.002f, 0.11f) * 0.6f);
            float brown = 0;
            _waves = Synth("waves", 6f, t =>
            {
                brown = Mathf.Clamp(brown + ((float)rng.NextDouble() * 2 - 1) * 0.02f, -1, 1);
                float swell = 0.5f + 0.5f * Mathf.Sin(2 * Mathf.PI * t / 3f);
                return brown * 0.5f * (0.3f + 0.7f * swell * swell);
            });
            for (int i = 0; i < 12; i++)
            {
                var s = gameObject.AddComponent<AudioSource>();
                s.playOnAwake = false;
                _src.Add(s);
            }
            _ambient = gameObject.AddComponent<AudioSource>();
            _ambient.clip = _waves;
            _ambient.loop = true;
            _ambient.volume = 0.18f;
            _ambient.Play();
        }

        public void Play(AudioClip clip, float vol, float pitch)
        {
            if (clip == null) return;
            // don't stack the same sound dozens of times per frame
            if (_last.TryGetValue(clip, out var t) && Time.unscaledTime - t < 0.03f) return;
            _last[clip] = Time.unscaledTime;
            var s = _src[_next];
            _next = (_next + 1) % _src.Count;
            s.pitch = pitch;
            s.PlayOneShot(clip, vol * Volume);
        }

        void Update() => _ambient.volume = 0.18f * Volume;

        static AudioClip Synth(string name, float seconds, System.Func<float, float> f)
        {
            int n = Mathf.CeilToInt(seconds * Rate);
            var data = new float[n];
            for (int i = 0; i < n; i++) data[i] = Mathf.Clamp(f(i / (float)Rate), -1f, 1f);
            var clip = AudioClip.Create(name, n, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        static float Env(float t, float attack, float decay)
        {
            if (t < attack) return t / attack;
            float k = 1f - (t - attack) / decay;
            return k <= 0 ? 0 : k * k;
        }

        static float Sq(float phase) => (phase % 1f) < 0.5f ? 1f : -1f;

        static float Chord(float t, float[] notes, float spread)
        {
            float s = 0;
            for (int i = 0; i < notes.Length; i++)
            {
                float st = t - i * spread;
                if (st < 0) continue;
                s += Mathf.Sin(2 * Mathf.PI * notes[i] * st) * Mathf.Exp(-st * 3f);
            }
            return s / notes.Length * 2f;
        }
    }
}
