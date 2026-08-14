using UnityEngine;

namespace KombiRush.Game
{
    /// <summary>
    /// Every sound is synthesised at startup, so the APK ships no audio files. Cheap on size,
    /// and the engine note can be driven straight from the kombi's speed.
    /// </summary>
    public static class AudioKit
    {
        private const int SampleRate = 22050;

        private static AudioClip _engine;
        private static AudioClip _coin;
        private static AudioClip _hit;
        private static AudioClip _payout;
        private static AudioClip _fuel;
        private static AudioClip _board;

        public static AudioClip Engine => _engine ??= BuildEngine();
        public static AudioClip Coin => _coin ??= BuildCoin();
        public static AudioClip Hit => _hit ??= BuildHit();
        public static AudioClip Payout => _payout ??= BuildPayout();
        public static AudioClip Fuel => _fuel ??= BuildFuel();
        public static AudioClip Board => _board ??= BuildBoard();

        private static AudioClip Make(string name, float seconds, System.Func<float, float> wave)
        {
            int count = Mathf.Max(1, (int)(seconds * SampleRate));
            var data = new float[count];
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)SampleRate;
                data[i] = Mathf.Clamp(wave(t), -1f, 1f);
            }
            AudioClip clip = AudioClip.Create(name, count, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip BuildEngine()
        {
            // one second of a rough four-cylinder idle, loopable: harmonics plus a little grit
            const float baseHz = 62f;
            var rng = new System.Random(7);
            float grit = 0f;
            return Make("engine", 1f, t =>
            {
                float v = 0f;
                v += Saw(baseHz * t) * 0.42f;
                v += Saw(baseHz * 2f * t) * 0.22f;
                v += Saw(baseHz * 3.02f * t) * 0.12f;
                v += Mathf.Sin(baseHz * 0.5f * t * Mathf.PI * 2f) * 0.16f;
                grit = grit * 0.86f + (float)(rng.NextDouble() * 2.0 - 1.0) * 0.14f;
                v += grit * 0.18f;
                return v * 0.5f;
            });
        }

        private static AudioClip BuildCoin() => Make("coin", 0.18f, t =>
        {
            float env = Mathf.Exp(-t * 26f);
            float hz = t < 0.05f ? 980f : 1460f;
            return Mathf.Sin(hz * t * Mathf.PI * 2f) * env * 0.55f;
        });

        private static AudioClip BuildBoard() => Make("board", 0.22f, t =>
        {
            float env = Mathf.Exp(-t * 14f);
            float hz = Mathf.Lerp(420f, 720f, Mathf.Clamp01(t / 0.22f));
            return (Mathf.Sin(hz * t * Mathf.PI * 2f) * 0.6f + Mathf.Sin(hz * 2f * t * Mathf.PI * 2f) * 0.2f) * env * 0.5f;
        });

        private static AudioClip BuildHit()
        {
            var rng = new System.Random(19);
            return Make("hit", 0.34f, t =>
            {
                float env = Mathf.Exp(-t * 12f);
                float noise = (float)(rng.NextDouble() * 2.0 - 1.0);
                float thump = Mathf.Sin(Mathf.Lerp(150f, 48f, Mathf.Clamp01(t / 0.34f)) * t * Mathf.PI * 2f);
                return (noise * 0.35f + thump * 0.75f) * env * 0.7f;
            });
        }

        private static AudioClip BuildPayout() => Make("payout", 0.5f, t =>
        {
            // three note arpeggio: the sound of getting paid
            float[] notes = { 523.25f, 659.25f, 783.99f };
            int step = Mathf.Clamp((int)(t / 0.14f), 0, notes.Length - 1);
            float local = t - step * 0.14f;
            float env = Mathf.Exp(-local * 9f);
            return (Mathf.Sin(notes[step] * t * Mathf.PI * 2f) * 0.6f
                    + Mathf.Sin(notes[step] * 2f * t * Mathf.PI * 2f) * 0.15f) * env * 0.5f;
        });

        private static AudioClip BuildFuel() => Make("fuel", 0.34f, t =>
        {
            float env = Mathf.Exp(-t * 7f);
            float hz = Mathf.Lerp(280f, 900f, Mathf.Clamp01(t / 0.34f));
            return Mathf.Sin(hz * t * Mathf.PI * 2f) * env * 0.45f;
        });

        private static float Saw(float phase)
        {
            float f = phase - Mathf.Floor(phase);
            return f * 2f - 1f;
        }
    }
}
