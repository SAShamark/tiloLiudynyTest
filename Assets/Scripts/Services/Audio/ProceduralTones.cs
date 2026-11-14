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

        /// <summary>Rubber-toy squeak: a quick upward pitch bend with a raspy edge, about 0.25 s.</summary>
        public static AudioClip CreateSqueak(string clipName)
        {
            var samples = new float[(int)(SampleRate * 0.25f)];
            float phase = 0f;
            for (int i = 0; i < samples.Length; i++)
            {
                float time = (float)i / SampleRate;
                float progress = time / 0.25f;
                float frequency = Mathf.Lerp(900f, 1500f, Mathf.Sin(progress * Mathf.PI * 0.8f));
                phase += 2f * Mathf.PI * frequency / SampleRate;
                float envelope = Mathf.Min(1f, time / 0.015f) * (1f - Mathf.SmoothStep(0.6f, 1f, progress));
                float rasp = Mathf.Sign(Mathf.Sin(phase)) * 0.25f;
                samples[i] = 0.4f * envelope * (Mathf.Sin(phase) + rasp + 0.2f * Mathf.Sin(phase * 2f));
            }

            return ToClip(clipName, samples);
        }

        /// <summary>Defibrillator discharge: a loud noisy crack with a low thud, about 0.5 s.</summary>
        public static AudioClip CreateZap(string clipName)
        {
            var samples = new float[(int)(SampleRate * 0.5f)];
            var random = new System.Random(7);
            float noise = 0f;
            for (int i = 0; i < samples.Length; i++)
            {
                float time = (float)i / SampleRate;
                noise = Mathf.Lerp(noise, (float)(random.NextDouble() * 2.0 - 1.0), 0.6f);
                float crack = noise * Mathf.Exp(-time * 14f) * (0.6f + 0.4f * Mathf.Sin(time * 2f * Mathf.PI * 120f));
                samples[i] = Mathf.Clamp(0.8f * crack + Thump(time, 0f, 55f, 0.9f), -1f, 1f);
            }

            return ToClip(clipName, samples);
        }

        /// <summary>Short clean beep, used for "ready" cues.</summary>
        public static AudioClip CreateBeep(string clipName, float frequency = 1320f, float duration = 0.12f)
        {
            var samples = new float[(int)(SampleRate * duration)];
            for (int i = 0; i < samples.Length; i++)
            {
                float time = (float)i / SampleRate;
                float envelope = Mathf.Min(1f, time / 0.005f) * Mathf.Min(1f, (duration - time) / 0.02f);
                samples[i] = 0.35f * envelope * Mathf.Sin(2f * Mathf.PI * frequency * time);
            }

            return ToClip(clipName, samples);
        }

        /// <summary>Looping coffee-machine pour: filtered noise with a low pump hum.</summary>
        public static AudioClip CreatePour(string clipName)
        {
            var samples = new float[SampleRate];
            var random = new System.Random(3);
            float noise = 0f;
            for (int i = 0; i < samples.Length; i++)
            {
                float time = (float)i / SampleRate;
                noise = Mathf.Lerp(noise, (float)(random.NextDouble() * 2.0 - 1.0), 0.25f);
                samples[i] = 0.25f * noise + 0.12f * Mathf.Sin(2f * Mathf.PI * 100f * time);
            }

            return ToClip(clipName, samples);
        }

        private static AudioClip ToClip(string clipName, float[] samples)
        {
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
