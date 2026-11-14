using Game.Entities.Pickups;
using UnityEngine;

namespace Game.Entities.Power
{
    /// <summary>
    /// Power cell hidden among the crates. Glows and pulses until it is picked up or socketed,
    /// so it is easy to spot.
    /// </summary>
    public sealed class Battery : Pickup
    {
        private static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");

        [SerializeField] private Color _glowColor = new(0.2f, 0.85f, 1f);
        [SerializeField, Min(0.1f)] private float _pulseSpeed = 2.4f;
        [SerializeField] private Vector2 _emissionRange = new(0.4f, 3f);
        [SerializeField] private Light _glowLight;
        [SerializeField] private float _lightIntensity = 0.8f;

        private Renderer[] _renderers;
        private MaterialPropertyBlock _block;

        protected override void Awake()
        {
            base.Awake();
            _renderers = GetComponentsInChildren<Renderer>();
            _block = new MaterialPropertyBlock();
        }

        private void Update()
        {
            bool highlight = !IsHeld && !IsSocketed;
            float pulse = highlight ? 0.5f + 0.5f * Mathf.Sin(Time.time * _pulseSpeed) : 0f;
            float emission = highlight ? Mathf.Lerp(_emissionRange.x, _emissionRange.y, pulse) : _emissionRange.x;
            SetEmission(_glowColor * emission);

            if (_glowLight == null)
                return;

            _glowLight.enabled = highlight;
            _glowLight.intensity = _lightIntensity * Mathf.Lerp(0.4f, 1f, pulse);
        }

        private void SetEmission(Color color)
        {
            foreach (var target in _renderers)
            {
                target.GetPropertyBlock(_block);
                _block.SetColor(EmissionId, color);
                target.SetPropertyBlock(_block);
            }
        }
    }
}
