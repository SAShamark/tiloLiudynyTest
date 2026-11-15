using System;
using Gameplay.Entities.Player;
using UnityEngine;

namespace Gameplay.Entities.Participant.Proximity
{
    [Serializable]
    public sealed class ProximityWarningEffects
    {
        private const int RingSegments = 48;

        private static readonly int StrengthId = Shader.PropertyToID("_Strength");
        private static readonly int DirectionId = Shader.PropertyToID("_Direction");
        private static readonly int FocusId = Shader.PropertyToID("_Focus");
        private static readonly int PulseId = Shader.PropertyToID("_Pulse");

        [SerializeField] private AudioSource _alarm;
        [SerializeField, Range(0f, 1f)] private float _alarmQuietVolume = 0.2f;
        [SerializeField, Range(0f, 1f)] private float _alarmLoudVolume = 1f;
        [SerializeField] private Renderer _vignette;
        [SerializeField] private LineRenderer _ring;
        [SerializeField] private Color _nearbyColor = new(1f, 0.75f, 0.2f);
        [SerializeField] private Color _stopColor = new(1f, 0.12f, 0.08f);

        private PlayerRig _rig;
        private MaterialPropertyBlock _vignetteBlock;
        private float _nextHaptic;

        public Color NearbyColor => _nearbyColor;
        public Color StopColor => _stopColor;

        public void Initialize(PlayerRig rig, float zoneRadius)
        {
            _rig = rig;
            _vignetteBlock = new MaterialPropertyBlock();
            _vignette.gameObject.SetActive(false);

            _ring.useWorldSpace = false;
            _ring.loop = true;
            _ring.positionCount = RingSegments;
            for (int i = 0; i < RingSegments; i++)
            {
                float angle = i * Mathf.PI * 2f / RingSegments;
                _ring.SetPosition(i, new Vector3(Mathf.Cos(angle) * zoneRadius, Mathf.Sin(angle) * zoneRadius, 0f));
            }

            _ring.enabled = false;
        }

        public void Apply(float intensity, float angle, bool stop)
        {
            bool active = intensity > 0.005f;
            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * (stop ? 14f : 7f));

            ApplyAlarm(intensity, active, stop);
            ApplyRing(intensity, stop, pulse);
            ApplyVignette(intensity, angle, active, stop, pulse);
            ApplyHaptics(intensity, angle);
        }

        public void Reset()
        {
            _nextHaptic = 0f;
            Apply(0f, 0f, false);
        }

        private void ApplyAlarm(float intensity, bool active, bool stop)
        {
            _alarm.volume = Mathf.Lerp(_alarmQuietVolume, _alarmLoudVolume, intensity);
            _alarm.pitch = stop ? 1.12f : 1f;
            if (active && !_alarm.isPlaying)
            {
                _alarm.Play();
            }
            else if (!active && _alarm.isPlaying)
            {
                _alarm.Stop();
            }
        }

        private void ApplyRing(float intensity, bool stop, float pulse)
        {
            bool visible = intensity > 0.01f;
            _ring.enabled = visible;
            if (!visible)
            {
                return;
            }

            Color color = stop ? _stopColor : _nearbyColor;
            color.a = Mathf.Clamp01(intensity * 1.5f) * (stop ? Mathf.Lerp(0.55f, 1f, pulse) : 0.7f);
            _ring.startColor = color;
            _ring.endColor = color;
            _ring.widthMultiplier = stop ? Mathf.Lerp(0.035f, 0.06f, pulse) : 0.03f;
        }

        private void ApplyVignette(float intensity, float angle, bool active, bool stop, float pulse)
        {
            _vignette.gameObject.SetActive(active);
            if (!active)
            {
                return;
            }

            float radians = angle * Mathf.Deg2Rad;
            float behind = Mathf.InverseLerp(120f, 170f, Mathf.Abs(angle));
            float ahead = 1f - Mathf.InverseLerp(10f, 35f, Mathf.Abs(angle));
            _vignette.GetPropertyBlock(_vignetteBlock);
            _vignetteBlock.SetFloat(StrengthId, intensity);
            _vignetteBlock.SetVector(DirectionId, new Vector4(Mathf.Sin(radians), 0f, 0f, 0f));
            _vignetteBlock.SetFloat(FocusId, Mathf.Clamp01(1f - behind - ahead * 0.6f));
            _vignetteBlock.SetFloat(PulseId, stop ? pulse : 0f);
            _vignette.SetPropertyBlock(_vignetteBlock);
        }

        private void ApplyHaptics(float intensity, float angle)
        {
            if (intensity <= 0.08f || Time.unscaledTime < _nextHaptic)
            {
                return;
            }

            float interval = Mathf.Lerp(0.33f, 0.12f, intensity);
            float side = Mathf.Sin(angle * Mathf.Deg2Rad);
            float right = Mathf.Lerp(0.08f, 1f, Mathf.Pow(Mathf.Clamp01(0.5f + 0.5f * side), 1.5f));
            float left = Mathf.Lerp(0.08f, 1f, Mathf.Pow(Mathf.Clamp01(0.5f - 0.5f * side), 1.5f));
            float duration = interval * Mathf.Lerp(0.35f, 1f, intensity);
            _rig.SendHaptics(intensity * left, intensity * right, duration);
            _nextHaptic = Time.unscaledTime + interval;
        }
    }
}
