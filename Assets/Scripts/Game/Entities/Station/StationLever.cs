using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace Game.Entities.Station
{
    /// <summary>
    /// Lever that slides between two authored positions and latches to the station state.
    /// </summary>
    public sealed class StationLever : MonoBehaviour
    {
        [SerializeField] private Transform _pivot;
        [SerializeField] private XRSimpleInteractable _knob;
        [SerializeField] private Renderer[] _readyRenderers;
        [SerializeField] private Material _idleMaterial;
        [SerializeField] private Material _readyMaterial;
        [SerializeField] private Renderer _stateLamp;

        [Header("Motion")]
        [SerializeField] private Transform _startPoint;
        [SerializeField] private Transform _endPoint;
        [SerializeField, Range(0.1f, 1f)] private float _triggerAt = 0.85f;
        [SerializeField, Min(0.1f)] private float _slideSpeed = 1.8f;

        private Func<bool> _canPull;
        private Func<bool> _isActive;
        private Action _onPull;
        private Vector3 _grabStart;
        private bool _fired;
        private bool _ready;
        private bool _grabbedActive;
        private float _progress;
        private bool? _lampActive;
        private MaterialPropertyBlock _lampProperties;
        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

        public void Init(Func<bool> canPull, Func<bool> isActive, Action onPull)
        {
            _canPull = canPull;
            _isActive = isActive;
            _onPull = onPull;
            _lampProperties = new MaterialPropertyBlock();
            _knob.selectEntered.AddListener(HandleGrabbed);
            ApplyReady(false);
            ApplyLamp(_isActive());
        }

        private void OnDestroy()
        {
            if (_knob != null)
                _knob.selectEntered.RemoveListener(HandleGrabbed);
        }

        private void LateUpdate()
        {
            if (_canPull == null)
                return;

            bool ready = _canPull();
            if (ready != _ready)
                ApplyReady(ready);

            bool active = _isActive();
            Vector3 path = _endPoint.position - _startPoint.position;
            float travel = path.magnitude;
            float target = active ? 1f : 0f;
            if (travel > 0.001f && ready && !_fired && active == _grabbedActive && _knob.isSelected)
            {
                var hand = _knob.interactorsSelecting[0].GetAttachTransform(_knob);
                float alongPath = Vector3.Dot(hand.position - _grabStart, path) / path.sqrMagnitude;
                float pull = Mathf.Clamp01(_grabbedActive ? -alongPath : alongPath);
                target = _grabbedActive ? 1f - pull : pull;
                if (pull >= _triggerAt)
                {
                    _fired = true;
                    _onPull();
                    target = _isActive() ? 1f : 0f;
                }
            }

            _progress = Mathf.MoveTowards(_progress, target, _slideSpeed * Time.deltaTime / Mathf.Max(travel, 0.001f));
            _pivot.position = Vector3.Lerp(_startPoint.position, _endPoint.position, _progress);
            ApplyLamp(_isActive());
        }

        private void HandleGrabbed(SelectEnterEventArgs args)
        {
            _grabStart = args.interactorObject.GetAttachTransform(_knob).position;
            _fired = false;
            _grabbedActive = _isActive();
        }

        private void ApplyLamp(bool active)
        {
            if (_lampActive == active)
                return;

            _lampActive = active;
            Color color = active ? new Color(0.05f, 0.85f, 0.2f) : new Color(0.85f, 0.035f, 0.025f);
            _stateLamp.GetPropertyBlock(_lampProperties);
            _lampProperties.SetColor(BaseColor, color);
            _lampProperties.SetColor(EmissionColor, color * 0.8f);
            _stateLamp.SetPropertyBlock(_lampProperties);
        }

        private void ApplyReady(bool ready)
        {
            _ready = ready;
            var material = ready ? _readyMaterial : _idleMaterial;
            foreach (var target in _readyRenderers)
            {
                if (target != null)
                    target.sharedMaterial = material;
            }
        }
    }
}
