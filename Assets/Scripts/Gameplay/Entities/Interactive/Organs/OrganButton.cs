using Gameplay.Effects;
using Gameplay.Entities.Interactive.Base.Buttons;
using UnityEngine;

namespace Gameplay.Entities.Interactive.Organs
{
    public sealed class OrganButton : PressButton
    {

        [SerializeField] private OrganConfig _organ;
        [SerializeField] private Renderer _icon;
        [SerializeField] private Color _idleGlow = new(0.15f, 0.7f, 0.85f);
        [SerializeField] private Color _selectedGlow = new(0.4f, 2.2f, 2.6f);
        [SerializeField, Min(0.1f)] private float _pulseSpeed = 2.2f;
        [SerializeField] private Color _pulseGlow = new(0.3f, 1.4f, 1.7f);

        private readonly RendererTint _tint = new();
        private bool _selected;

        public OrganConfig Organ => _organ;

        protected override void Awake()
        {
            base.Awake();
            SetGlow(_idleGlow);
        }

        protected override void Update()
        {
            base.Update();
            if (_selected)
            {
                return;
            }

            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * _pulseSpeed);
            SetGlow(Color.Lerp(_idleGlow, _pulseGlow, pulse));
        }

        public void SetSelected(bool selected)
        {
            _selected = selected;
            SetGlow(selected ? _selectedGlow : _idleGlow);
        }

        private void SetGlow(Color glow)
        {
            _tint.SetEmission(_icon, glow);
        }
    }
}
