using UnityEngine;

namespace Services.Audio
{
    public static class ProceduralTones
    {
        private const int SampleRate = 24000;
        private const float PulsePeriod = 0.45f;

        public static AudioClip CreateTwoToneAlarm(string clipName)
        {
            var samples = new float[21600];
            for (int i = 0; i < samples.Length; i++)
            {
                float time = (float)i / SampleRate;
                float pulseTime = time % PulsePeriod;
                float envelope = Mathf.SmoothStep(0f, 1f, pulseTime / 0.04f)
                    * (1f - Mathf.SmoothStep(0f, 1f, (pulseTime - 0.18f) / 0.14f));
                float phase = time * (time < PulsePeriod ? 290f : 221f) * 2f * Mathf.PI;
                samples[i] = envelope * (0.62f * Mathf.Sin(phase) + 0.19f * Mathf.Sin(phase * 2f));
            }

            var clip = AudioClip.Create(clipName, samples.Length, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        /// <summary>Low "lub-dub" thump, about 0.45 s long.</summary>
        public static AudioClip CreateHeartbeat(string clipName)
        {
            var samples = new float[(int)(SampleRate * 0.45f)];
            for (int i = 0; i < samples.Length; i++)
            {
                float time = (float)i / SampleRate;
                samples[i] = Thump(time, 0f, 58f, 1f) + Thump(time, 0.2f, 48f, 0.7f);
            }

            var clip = AudioClip.Create(clipName, samples.Length, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        /// <summary>Rising electric whine with a soft click at the start, about 0.9 s long.</summary>
        public static AudioClip CreatePowerUp(string clipName)
        {
            var samples = new float[(int)(SampleRate * 0.9f)];
            float phase = 0f;
            for (int i = 0; i < samples.Length; i++)
            {
                float time = (float)i / SampleRate;
                float progress = time / 0.9f;
                float frequency = Mathf.Lerp(110f, 520f, progress * progress);
                phase += 2f * Mathf.PI * frequency / SampleRate;
                float envelope = Mathf.Min(1f, time / 0.05f) * (1f - Mathf.SmoothStep(0.75f, 1f, progress));
                float click = Mathf.Exp(-time * 120f) * 0.6f;
                samples[i] = 0.35f * envelope * (Mathf.Sin(phase) + 0.3f * Mathf.Sin(phase * 3f)) + click * Mathf.Sin(time * 2400f);
            }

            var clip = AudioClip.Create(clipName, samples.Length, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static float Thump(float time, float start, float frequency, float gain)
        {
            float local = time - start;
            if (local < 0f)
                return 0f;

            float envelope = Mathf.Min(1f, local / 0.008f) * Mathf.Exp(-local * 22f);
            float pitchDrop = frequency * (1f + 0.6f * Mathf.Exp(-local * 40f));
            return gain * 0.9f * envelope * Mathf.Sin(2f * Mathf.PI * pitchDrop * local);
        }
    }
}
