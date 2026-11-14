using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace Gameplay.Entities.Interactive.Organs
{
    public sealed class FlaskButtonPanel : MonoBehaviour, IOrganSelector
    {
        private static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");

        [Serializable]
        private sealed class OrganButton
        {
            public OrganConfig Organ;
            public XRSimpleInteractable Interactable;
            public Transform Cap;
            public Renderer Icon;
            [NonSerialized] public Vector3 RestPosition;
        }

        [SerializeField] private OrganButton[] _buttons;
        [SerializeField] private float _pressDepth = 0.008f;
        [SerializeField] private Color _idleGlow = new(0.15f, 0.7f, 0.85f);
        [SerializeField] private Color _selectedGlow = new(0.4f, 2.2f, 2.6f);
        [Tooltip("Unselected buttons breathe so it is obvious they can be pressed.")]
        [SerializeField, Min(0.1f)] private float _pulseSpeed = 2.2f;
        [SerializeField] private Color _pulseGlow = new(0.3f, 1.4f, 1.7f);

        private OrganConfig _selected;

        private MaterialPropertyBlock _block;

        public event Action<OrganConfig> OrganSelected;

        private void Awake()
        {
            _block = new MaterialPropertyBlock();
            foreach (var button in _buttons)
            {
                button.RestPosition = button.Cap.localPosition;
                var captured = button;
                button.Interactable.selectEntered.AddListener(_ => HandlePressed(captured));
                button.Interactable.selectExited.AddListener(_ => SetPressed(captured, false));
                SetGlow(button, false);
            }
        }

        private void OnDestroy()
        {
            foreach (var button in _buttons)
            {
                button.Interactable.selectEntered.RemoveAllListeners();
                button.Interactable.selectExited.RemoveAllListeners();
            }
        }

        public void Highlight(OrganConfig organ)
        {
            _selected = organ;
            foreach (var button in _buttons)
                SetGlow(button, button.Organ == organ);
        }

        private void Update()
        {
            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * _pulseSpeed);
            Color glow = Color.Lerp(_idleGlow, _pulseGlow, pulse);
            foreach (var button in _buttons)
            {
                if (button.Organ == _selected)
                    continue;

                button.Icon.GetPropertyBlock(_block);
                _block.SetColor(EmissionId, glow);
                button.Icon.SetPropertyBlock(_block);
            }
        }

        private void HandlePressed(OrganButton button)
        {
            SetPressed(button, true);
            OrganSelected?.Invoke(button.Organ);
        }

        private void SetPressed(OrganButton button, bool pressed) =>
            button.Cap.localPosition = button.RestPosition + (pressed ? Vector3.forward * _pressDepth : Vector3.zero);

        private void SetGlow(OrganButton button, bool selected)
        {
            button.Icon.GetPropertyBlock(_block);
            _block.SetColor(EmissionId, selected ? _selectedGlow : _idleGlow);
            button.Icon.SetPropertyBlock(_block);
        }
    }
}
