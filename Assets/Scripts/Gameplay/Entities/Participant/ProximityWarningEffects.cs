using System;
using Services.Audio;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Gameplay.Entities.Participant
{
    [Serializable]
    public sealed class ProximityWarningEffects
    {
        private static readonly int StrengthId = Shader.PropertyToID("_Strength");
        private static readonly int DirectionId = Shader.PropertyToID("_Direction");
        private static readonly int FocusId = Shader.PropertyToID("_Focus");
        private static readonly int PulseId = Shader.PropertyToID("_Pulse");

        [SerializeField] private AudioSource _alarm;
        [SerializeField] private Renderer _vignette;
        [SerializeField] private ProximityZoneRing _zoneRing;

        private Material _vignetteMaterial;
        private AudioClip _alarmClip;

        public void Initialize(float zoneRadius)
        {
            _alarmClip = ProceduralTones.CreateTwoToneAlarm("Participant proximity alarm");
            _alarm.clip = _alarmClip;
            _vignetteMaterial = _vignette.material;
            _vignette.gameObject.SetActive(false);
            _zoneRing.Initialize(zoneRadius);
        }

        public void Apply(float intensity, float angle, bool stop)
        {
            bool active = intensity > 0.005f;
            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * (stop ? 14f : 7f));

            _alarm.volume = 0.85f * intensity;
            _alarm.pitch = stop ? 1.12f : 1f;
            if (active && !_alarm.isPlaying)
            {
                _alarm.Play();
            }
            else if (!active && _alarm.isPlaying)
            {
                _alarm.Stop();
            }

            _zoneRing.Show(intensity, stop, pulse);
            _vignette.gameObject.SetActive(active);
            if (!active)
            {
                return;
            }

            float radians = angle * Mathf.Deg2Rad;
            float behind = Mathf.InverseLerp(120f, 170f, Mathf.Abs(angle));
            float ahead = 1f - Mathf.InverseLerp(10f, 35f, Mathf.Abs(angle));
            _vignetteMaterial.SetFloat(StrengthId, intensity);
            _vignetteMaterial.SetVector(DirectionId, new Vector4(Mathf.Sin(radians), 0f, 0f, 0f));
            _vignetteMaterial.SetFloat(FocusId, Mathf.Clamp01(1f - behind - ahead * 0.6f));
            _vignetteMaterial.SetFloat(PulseId, stop ? pulse : 0f);
        }

        public void Dispose()
        {
            Object.Destroy(_vignetteMaterial);
            Object.Destroy(_alarmClip);
        }
    }
}
