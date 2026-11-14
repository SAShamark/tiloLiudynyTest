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
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        [SerializeField] private Color _glowColor = new(0.2f, 0.85f, 1f);
        [SerializeField, Min(0.1f)] private float _pulseSpeed = 2.4f;
        [SerializeField] private Vector2 _emissionRange = new(0.4f, 3f);
        [SerializeField] private Light _glowLight;
        [SerializeField] private float _lightIntensity = 0.8f;

        private Renderer[] _renderers;
        private MaterialPropertyBlock _block;
        private Color[] _baseColors;

        protected override void Awake()
        {
            base.Awake();
            _renderers = GetComponentsInChildren<Renderer>();
            _block = new MaterialPropertyBlock();
            _baseColors = new Color[_renderers.Length];
            for (int i = 0; i < _renderers.Length; i++)
            {
                var material = _renderers[i].sharedMaterial;
                _baseColors[i] = material != null && material.HasProperty(BaseColorId)
                    ? material.GetColor(BaseColorId)
                    : material != null && material.HasProperty(ColorId) ? material.GetColor(ColorId) : Color.white;
            }
        }

        private void Update()
        {
            bool highlight = !IsHeld && !IsSocketed;
            float pulse = highlight ? 0.5f + 0.5f * Mathf.Sin(Time.time * _pulseSpeed) : 0f;
            float emission = highlight ? Mathf.Lerp(_emissionRange.x, _emissionRange.y, pulse) : _emissionRange.x;
            SetGlow(_glowColor * emission, highlight ? Mathf.Lerp(0.18f, 0.72f, pulse) : 0f);

            if (_glowLight == null)
                return;

            _glowLight.enabled = highlight;
            _glowLight.intensity = _lightIntensity * Mathf.Lerp(0.4f, 1f, pulse);
        }

        private void SetGlow(Color emission, float tint)
        {
            for (int i = 0; i < _renderers.Length; i++)
            {
                var target = _renderers[i];
                target.GetPropertyBlock(_block);
                Color color = Color.Lerp(_baseColors[i], _glowColor, tint);
                _block.SetColor(BaseColorId, color);
                _block.SetColor(ColorId, color);
                _block.SetColor(EmissionId, emission);
                target.SetPropertyBlock(_block);
            }
        }
    }
}
