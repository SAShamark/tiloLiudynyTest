using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace Gameplay.Entities.Interactive.Station
{
    public sealed class StationLever : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");

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
        private Func<bool> _highlight;
        private Func<bool> _running;
        private Action _onPull;
        private Vector3 _grabStart;
        private bool _fired;
        private bool _ready;
        private bool _grabbedActive;
        private float _progress;
        private bool? _lampActive;
        private MaterialPropertyBlock _propertyBlock;

        public void Initialize(Func<bool> canPull, Func<bool> isActive, Action onPull)
        {
            _canPull = canPull;
            _isActive = isActive;
            _onPull = onPull;
            _knob.selectEntered.AddListener(HandleGrabbed);
            ApplyReady(false);
            ApplyLamp(_isActive());
        }

        public void SetIndicators(Func<bool> highlight, Func<bool> running)
        {
            _highlight = highlight;
            _running = running;
        }

        private void OnDestroy()
        {
            _knob.selectEntered.RemoveListener(HandleGrabbed);
        }

        private void LateUpdate()
        {
            bool ready = _canPull();
            bool highlight = _highlight();
            if (highlight != _ready)
                ApplyReady(highlight);

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
            ApplyLamp(_running());
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
            SetBaseAndEmission(_stateLamp, color, color * 0.8f);
        }

        private void ApplyReady(bool ready)
        {
            _ready = ready;
            var material = ready ? _readyMaterial : _idleMaterial;
            foreach (var target in _readyRenderers)
            {
                target.sharedMaterial = material;
            }
        }

        private void SetBaseAndEmission(Renderer target, Color baseColor, Color emission)
        {
            _propertyBlock ??= new MaterialPropertyBlock();
            target.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetColor(BaseColorId, baseColor);
            _propertyBlock.SetColor(ColorId, baseColor);
            _propertyBlock.SetColor(EmissionId, emission);
            target.SetPropertyBlock(_propertyBlock);
        }
    }
}
