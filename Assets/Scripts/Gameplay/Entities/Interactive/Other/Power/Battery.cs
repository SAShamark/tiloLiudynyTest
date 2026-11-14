using Gameplay.Effects;
using Gameplay.Entities.Interactive.BaseLogic.Items;
using UnityEngine;

namespace Gameplay.Entities.Interactive.Other.Power
{
    public sealed class Battery : ReturningItem
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        [SerializeField] private Color _glowColor = new(0.2f, 0.85f, 1f);
        [SerializeField, Min(0.1f)] private float _pulseSpeed = 2.4f;
        [SerializeField] private Vector2 _emissionRange = new(0.4f, 3f);
        [SerializeField] private Light _glowLight;
        [SerializeField] private float _lightIntensity = 0.8f;

        [Header("Effects")]
        [Tooltip("Looping charge aura (glow, arcs, motes) shown while the battery waits to be found.")]
        [SerializeField] private ParticleSystem _aura;
        [Tooltip("One-shot spark burst played when the battery is picked up.")]
        [SerializeField] private ParticleSystem _grabBurst;

        private Renderer[] _renderers;
        private Color[] _baseColors;
        private bool _highlighted;
        private readonly RendererTint _tint = new();

        protected override void Awake()
        {
            base.Awake();
            _renderers = System.Array.FindAll(GetComponentsInChildren<Renderer>(), renderer => renderer is not ParticleSystemRenderer);
            _baseColors = new Color[_renderers.Length];
            for (int i = 0; i < _renderers.Length; i++)
            {
                var material = _renderers[i].sharedMaterial;
                _baseColors[i] = material.HasProperty(BaseColorId)
                    ? material.GetColor(BaseColorId)
                    : material.HasProperty(ColorId) ? material.GetColor(ColorId) : Color.white;
            }
        }

        private void Update()
        {
            bool highlight = !IsHeld && !IsSocketed;
            if (highlight != _highlighted)
                SetHighlighted(highlight);

            float pulse = highlight ? 0.5f + 0.5f * Mathf.Sin(Time.time * _pulseSpeed) : 0f;
            float emission = highlight ? Mathf.Lerp(_emissionRange.x, _emissionRange.y, pulse) : _emissionRange.x;
            SetGlow(_glowColor * emission, highlight ? Mathf.Lerp(0.18f, 0.72f, pulse) : 0f);
            _glowLight.enabled = highlight;
            _glowLight.intensity = _lightIntensity * Mathf.Lerp(0.4f, 1f, pulse);
        }

        private void SetHighlighted(bool highlighted)
        {
            _highlighted = highlighted;
            if (highlighted)
                _aura.Play(true);
            else
                _aura.Stop(true, ParticleSystemStopBehavior.StopEmitting);


            if (!highlighted && IsHeld)
                _grabBurst.Play(true);
        }

        private void SetGlow(Color emission, float tint)
        {
            for (int i = 0; i < _renderers.Length; i++)
            {
                Color color = Color.Lerp(_baseColors[i], _glowColor, tint);
                _tint.SetBaseAndEmission(_renderers[i], color, emission);
            }
        }
    }
}
