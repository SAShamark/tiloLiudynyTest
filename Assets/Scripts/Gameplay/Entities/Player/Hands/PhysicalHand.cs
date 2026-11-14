using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace Gameplay.Entities.Player.Hands
{
    public sealed class PhysicalHand : MonoBehaviour
    {
        private const float LockDistance = 0.08f;

        [SerializeField] private XRNode _node;
        [SerializeField] private Rigidbody _body;
        [SerializeField] private GameObject _visual;
        [SerializeField] private Transform _wrist;
        [Tooltip("Hand colliders; the first one is the palm.")]
        [SerializeField] private Collider[] _colliders;
        [SerializeField] private HandFinger[] _fingers;

        private readonly List<InputDevice> _devices = new();
        private PhysicalHandSettings _settings;
        private Transform _controller;
        private XRBaseInteractor[] _interactors;
        private InputDevice _device;
        private float _grip;
        private float _trigger;

        private XRGrabInteractable _held;
        private Collider[] _heldColliders = Array.Empty<Collider>();
        private Vector3 _heldLocalPosition;
        private Quaternion _heldLocalRotation;
        private Collider[] _releasedColliders = Array.Empty<Collider>();
        private float _releaseTimer;
        private bool _locked;

        public Transform Wrist => _wrist;
        public bool IsVisible => _visual.activeInHierarchy;
        public IReadOnlyList<Collider> Colliders => _colliders;

        public void Initialize(PhysicalHandSettings settings, Transform controller, XRBaseInteractor[] interactors)
        {
            _settings = settings;
            _controller = controller;
            _interactors = interactors;
            _body.transform.SetPositionAndRotation(controller.position, controller.rotation);
            foreach (var finger in _fingers)
            {
                finger.Initialize();
            }

            foreach (var interactor in _interactors)
            {
                interactor.selectEntered.AddListener(HandleSelectEntered);
                interactor.selectExited.AddListener(HandleSelectExited);
            }
        }

        public void Dispose()
        {
            foreach (var interactor in _interactors)
            {
                interactor.selectEntered.RemoveListener(HandleSelectEntered);
                interactor.selectExited.RemoveListener(HandleSelectExited);
            }
        }

        public void IgnoreOtherHand(PhysicalHand other)
        {
            foreach (var mine in _colliders)
            foreach (var theirs in other.Colliders)
                Physics.IgnoreCollision(mine, theirs, true);
        }

        public void FixedTick(float deltaTime)
        {
            if (_locked || !_visual.activeSelf)
                return;

            Vector3 target = _controller.position;
            Quaternion targetRotation = _controller.rotation;
            if ((target - _body.position).sqrMagnitude > _settings.SnapDistance * _settings.SnapDistance)
            {
                _body.position = target;
                _body.rotation = targetRotation;
                _body.linearVelocity = Vector3.zero;
                _body.angularVelocity = Vector3.zero;
                return;
            }

            float strength = _settings.FollowStrength;
            _body.linearVelocity = (target - _body.position) * (strength / deltaTime);

            Quaternion delta = targetRotation * Quaternion.Inverse(_body.rotation);
            delta.ToAngleAxis(out float angle, out Vector3 axis);
            if (angle > 180f)
                angle -= 360f;
            if (Mathf.Abs(angle) > 0.01f && !float.IsNaN(axis.x))
                _body.angularVelocity = axis * (angle * Mathf.Deg2Rad * strength / deltaTime);
            else
                _body.angularVelocity = Vector3.zero;
        }

        public void LateTick(float deltaTime)
        {
            UpdateDevice();
            bool tracked = _device.isValid &&
                _device.TryGetFeatureValue(CommonUsages.isTracked, out bool isTracked) && isTracked;
            if (_visual.activeSelf != tracked)
            {
                _visual.SetActive(tracked);
                SetCollidersEnabled(tracked);
                if (tracked)
                    _body.position = _controller.position;
            }

            if (_releaseTimer > 0f)
            {
                _releaseTimer -= deltaTime;
                if (_releaseTimer <= 0f)
                    SetIgnore(_releasedColliders, false);
            }

            if (!tracked)
                return;

            _device.TryGetFeatureValue(CommonUsages.grip, out float grip);
            _device.TryGetFeatureValue(CommonUsages.trigger, out float trigger);
            float blend = 1f - Mathf.Exp(-20f * deltaTime);
            _grip = Mathf.Lerp(_grip, grip, blend);
            _trigger = Mathf.Lerp(_trigger, trigger, blend);

            TryLock();
            foreach (var finger in _fingers)
                finger.Tick(_locked, _grip, _trigger, blend);

            FollowHeldItem();
        }

        public void FollowHeldItem()
        {
            if (_held == null || !_locked)
                return;

            Transform item = _held.transform;
            _body.position = item.TransformPoint(_heldLocalPosition);
            _body.rotation = item.rotation * _heldLocalRotation;
            _body.transform.SetPositionAndRotation(_body.position, _body.rotation);
        }

        private void HandleSelectEntered(SelectEnterEventArgs args)
        {
            if (args.interactableObject is not XRGrabInteractable grab)
                return;

            _held = grab;
            _heldColliders = grab.colliders.ToArray();
            SetIgnore(_heldColliders, true);
            _locked = false;
            TryLock();
        }

        private void TryLock()
        {
            if (_held == null || _locked || DistanceToItem() > LockDistance)
                return;

            Transform item = _held.transform;
            _locked = true;
            _body.isKinematic = true;
            _heldLocalPosition = item.InverseTransformPoint(_body.position);
            _heldLocalRotation = Quaternion.Inverse(item.rotation) * _body.rotation;
            foreach (var finger in _fingers)
                finger.FitTo(_heldColliders, _settings.FingerRadius);
        }

        private float DistanceToItem()
        {
            Vector3 palm = _colliders.Length > 0 ? _colliders[0].bounds.center : _body.position;
            float best = float.MaxValue;
            foreach (var collider in _heldColliders)
            {
                if (!collider.enabled)
                    continue;
                Vector3 point = collider is MeshCollider { convex: false } ? collider.bounds.ClosestPoint(palm) : collider.ClosestPoint(palm);
                best = Mathf.Min(best, Vector3.Distance(point, palm));
            }

            return best;
        }

        private void HandleSelectExited(SelectExitEventArgs args)
        {
            if (args.interactableObject is not XRGrabInteractable grab || grab != _held)
                return;

            _held = null;
            _locked = false;
            _body.isKinematic = false;
            _releasedColliders = _heldColliders;
            _heldColliders = Array.Empty<Collider>();
            _releaseTimer = _settings.ReleaseGrace;
        }

        private void SetIgnore(Collider[] others, bool ignore)
        {
            foreach (var hand in _colliders)
            foreach (var other in others)
            {
                Physics.IgnoreCollision(hand, other, ignore);
            }
        }

        private void SetCollidersEnabled(bool enabled)
        {
            foreach (var collider in _colliders)
                collider.enabled = enabled;
        }

        private void UpdateDevice()
        {
            if (_device.isValid)
                return;

            var side = _node == XRNode.LeftHand ? InputDeviceCharacteristics.Left : InputDeviceCharacteristics.Right;
            InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.Controller | side, _devices);
            if (_devices.Count > 0)
                _device = _devices[0];
        }
    }
}
