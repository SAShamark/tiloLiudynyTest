using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace Game.Entities.Station
{
    /// <summary>
    /// Organ projection: flickers into existence with glitches and stays a hologram until it is told to
    /// materialise (station started), then fills with colour into a solid organ. Breaks up again when hidden.
    /// Drives the Game/Hologram shader through a MaterialPropertyBlock.
    /// </summary>
    public sealed class HologramProjection : MonoBehaviour
    {
        private static readonly int GlitchId = Shader.PropertyToID("_Glitch");
        private static readonly int VisibilityId = Shader.PropertyToID("_Visibility");
        private static readonly int FillId = Shader.PropertyToID("_Fill");

        [SerializeField] private Renderer[] _renderers;
        [SerializeField, Min(0.1f)] private float _appearDuration = 1.4f;
        [SerializeField, Min(0.1f)] private float _disappearDuration = 0.45f;

        [Header("Materialise")]
        [Tooltip("Pause between the materialise request and the colour starting to fill in.")]
        [SerializeField, Min(0f)] private float _fillDelay = 0.3f;
        [SerializeField, Min(0.1f)] private float _fillDuration = 2.5f;
        [SerializeField, Min(0.1f)] private float _unfillDuration = 1f;

        [Header("Light")]
        [Tooltip("Optional light cast by the projection; follows its visibility and flickers with it.")]
        [SerializeField] private Light _light;
        [SerializeField, Min(0f)] private float _lightIntensity = 1.2f;

        [Header("Idle glitches")]
        [SerializeField] private Vector2 _idleGlitchInterval = new(3f, 7f);
        [SerializeField, Min(0.02f)] private float _idleGlitchDuration = 0.18f;
        [Tooltip("Idle glitch strength once the organ is solid, relative to the hologram.")]
        [SerializeField, Range(0f, 1f)] private float _solidGlitchScale = 0.25f;

        private MaterialPropertyBlock _block;
        private XRBaseInteractable _interactable;
        private bool _visible;
        private float _progress;
        private float _fill;
        private bool _materialized;
        private float _untilFill;
        private float _untilIdleGlitch;
        private float _idleGlitchLeft;

        public bool IsVisible => _visible;
        public bool IsHidden => !_visible && _progress <= 0f;

        /// <summary>True: fill the hologram with colour; false: fade back to a pure hologram.</summary>
        public void SetMaterialized(bool materialized)
        {
            if (materialized && !_materialized)
                _untilFill = _fillDelay;
            _materialized = materialized;
        }

        public void SetRenderers(Renderer[] renderers)
        {
            _renderers = renderers;
            _fill = 0f;
            _untilFill = _fillDelay;
            Apply(0f, _visible ? 1f : 0f);
            SetRenderersEnabled(_visible || _progress > 0f);
        }

        private void Awake()
        {
            _block = new MaterialPropertyBlock();
            _interactable = GetComponent<XRBaseInteractable>();
            Apply(0f, 1f);
            SetRenderersEnabled(false);
            SetInteractable(false);
        }

        public void Show()
        {
            if (_visible)
                return;

            _visible = true;
            _fill = 0f;
            _untilFill = _fillDelay;
            _untilIdleGlitch = Random.Range(_idleGlitchInterval.x, _idleGlitchInterval.y);
            SetRenderersEnabled(true);
            SetInteractable(true);
        }

        public void Hide()
        {
            if (!_visible)
                return;

            _visible = false;
            SetInteractable(false);
        }

        private void Update()
        {
            float target = _visible ? 1f : 0f;
            if (!Mathf.Approximately(_progress, target))
            {
                float duration = _visible ? _appearDuration : _disappearDuration;
                _progress = Mathf.MoveTowards(_progress, target, Time.deltaTime / duration);
                if (!_visible)
                    _fill = Mathf.Min(_fill, _progress);

                ApplyTransition();
                if (!_visible && _progress <= 0f)
                    SetRenderersEnabled(false);
                return;
            }

            if (!_visible)
                return;

            UpdateFill();
            UpdateIdleGlitch();
        }

        private void ApplyTransition()
        {
            // Random on/off flashes that become steadier as the projection stabilises.
            float flashChance = Mathf.SmoothStep(0f, 1f, _progress);
            bool on = Random.value < Mathf.Lerp(0.15f, 1f, flashChance);
            float visibility = on ? Mathf.Lerp(0.4f, 1f, _progress) : 0f;
            float glitch = 1f - _progress * _progress;
            Apply(glitch, visibility);
        }

        private void UpdateFill()
        {
            float target = _materialized ? 1f : 0f;
            if (Mathf.Approximately(_fill, target))
                return;

            if (_materialized && _untilFill > 0f)
            {
                _untilFill -= Time.deltaTime;
                return;
            }

            float duration = _materialized ? _fillDuration : _unfillDuration;
            _fill = Mathf.MoveTowards(_fill, target, Time.deltaTime / duration);
            if (_idleGlitchLeft <= 0f)
                Apply(0f, 1f);
        }

        private void UpdateIdleGlitch()
        {
            float strength = Mathf.Lerp(0.6f, 0.6f * _solidGlitchScale, _fill);
            if (_idleGlitchLeft > 0f)
            {
                _idleGlitchLeft -= Time.deltaTime;
                Apply(_idleGlitchLeft > 0f ? strength : 0f, 1f);
                return;
            }

            _untilIdleGlitch -= Time.deltaTime;
            if (_untilIdleGlitch > 0f)
                return;

            _idleGlitchLeft = _idleGlitchDuration;
            _untilIdleGlitch = Random.Range(_idleGlitchInterval.x, _idleGlitchInterval.y);
        }

        private void Apply(float glitch, float visibility)
        {
            if (_light != null)
            {
                _light.enabled = visibility > 0.01f;
                _light.intensity = _lightIntensity * visibility * (1f - glitch * 0.5f);
            }

            float fill = Mathf.SmoothStep(0f, 1f, _fill);
            foreach (var target in _renderers)
            {
                if (target == null)
                    continue;

                target.GetPropertyBlock(_block);
                _block.SetFloat(GlitchId, glitch);
                _block.SetFloat(VisibilityId, visibility);
                _block.SetFloat(FillId, fill);
                target.SetPropertyBlock(_block);
            }
        }

        private void SetRenderersEnabled(bool enabled)
        {
            if (_light != null && !enabled)
                _light.enabled = false;

            foreach (var target in _renderers)
            {
                if (target != null)
                    target.enabled = enabled;
            }
        }

        private void SetInteractable(bool enabled)
        {
            if (_interactable != null)
                _interactable.enabled = enabled;
        }
    }
}
