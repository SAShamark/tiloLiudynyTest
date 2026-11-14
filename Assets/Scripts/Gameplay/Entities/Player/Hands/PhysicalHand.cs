using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

using Object = UnityEngine.Object;

namespace Gameplay.Entities.Player.Hands
{
    internal readonly struct PhysicalHandSettings
    {
        public readonly float FollowStrength;
        public readonly float SnapDistance;
        public readonly float FingerRadius;
        public readonly float ReleaseGrace;

        public PhysicalHandSettings(float followStrength, float snapDistance, float fingerRadius, float releaseGrace)
        {
            FollowStrength = followStrength;
            SnapDistance = snapDistance;
            FingerRadius = fingerRadius;
            ReleaseGrace = releaseGrace;
        }
    }

    internal sealed class PhysicalHand
    {
        private readonly PhysicalHandSettings _settings;
        private readonly XRNode _node;
        private readonly Transform _controller;
        private readonly Rigidbody _body;
        private readonly GameObject _visual;
        private readonly HandFinger[] _fingers;
        private readonly Collider[] _colliders;
        private readonly XRBaseInteractor[] _interactors;
        private readonly List<InputDevice> _devices = new List<InputDevice>();
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
        private const float LockDistance = 0.08f;

        public Transform Wrist { get; set; }
        public bool IsVisible => _visual.activeInHierarchy;

        public PhysicalHand(PhysicalHandSettings settings, XRNode node, Transform controller, Rigidbody body, GameObject visual,
            HandFinger[] fingers, Collider[] colliders, XRBaseInteractor[] interactors)
        {
            _settings = settings;
            _node = node;
            _controller = controller;
            _body = body;
            _visual = visual;
            _fingers = fingers;
            _colliders = colliders;
            _interactors = interactors;
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

            Object.Destroy(_body.gameObject);
        }

        public void IgnoreOtherHand(PhysicalHand other)
        {
            foreach (var mine in _colliders)
            foreach (var theirs in other._colliders)
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
