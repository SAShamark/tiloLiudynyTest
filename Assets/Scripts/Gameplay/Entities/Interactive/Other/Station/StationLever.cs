using Gameplay.Effects;
using System;
using Gameplay.Entities.Interactive.BaseLogic.Controls;
using UnityEngine;

namespace Gameplay.Entities.Interactive.Other.Station
{
    public sealed class StationLever : HandleControl
    {

        [SerializeField] private Transform _pivot;
        [SerializeField] private Renderer[] _readyRenderers;
        [SerializeField] private Material _idleMaterial;
        [SerializeField] private Material _readyMaterial;
        [SerializeField] private Renderer _stateLamp;

        [Header("Motion")]
        [SerializeField] private Transform _startPoint;
        [SerializeField] private Transform _endPoint;
        [SerializeField, Range(0.1f, 1f)] private float _triggerAt = 0.85f;
        [SerializeField, Min(0.1f)] private float _slideSpeed = 1.8f;

        private bool _on;
        private bool _locked;
        private bool _fired;
        private bool _ready;
        private bool _grabbedActive;
        private float _progress;
        private bool? _lampActive;
        private readonly RendererTint _tint = new();

        public event Action<bool> Toggled;

        private void Awake()
        {
            ApplyReady(false);
            ApplyLamp(false);
        }

        public void SetLocked(bool locked) => _locked = locked;

        public void ShowIndicators(bool highlight, bool running)
        {
            if (highlight != _ready)
            {
                ApplyReady(highlight);
            }

            ApplyLamp(running);
        }

        private void LateUpdate()
        {
            bool active = _on;
            Vector3 path = _endPoint.position - _startPoint.position;
            float travel = path.magnitude;
            float target = active ? 1f : 0f;
            if (travel > 0.001f && !_locked && !_fired && active == _grabbedActive && IsGrabbed)
            {
                float alongPath = Vector3.Dot(HandPosition - GrabPosition, path) / path.sqrMagnitude;
                float pull = Mathf.Clamp01(_grabbedActive ? -alongPath : alongPath);
                target = _grabbedActive ? 1f - pull : pull;
                if (pull >= _triggerAt)
                {
                    _fired = true;
                    _on = !_on;
                    Toggled?.Invoke(_on);
                    target = _on ? 1f : 0f;
                }
            }

            _progress = Mathf.MoveTowards(_progress, target, _slideSpeed * Time.deltaTime / Mathf.Max(travel, 0.001f));
            _pivot.position = Vector3.Lerp(_startPoint.position, _endPoint.position, _progress);
        }

        protected override void OnGrabbed()
        {
            _fired = false;
            _grabbedActive = _on;
        }

        private void ApplyLamp(bool active)
        {
            if (_lampActive == active)
                return;

            _lampActive = active;
            Color color = active ? new Color(0.05f, 0.85f, 0.2f) : new Color(0.85f, 0.035f, 0.025f);
            _tint.SetBaseAndEmission(_stateLamp, color, color * 0.8f);
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
    }
}
