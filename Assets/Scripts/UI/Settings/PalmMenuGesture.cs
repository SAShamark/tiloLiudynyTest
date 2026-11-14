using System;
using System.Collections.Generic;
using Gameplay.Entities.Player;
using UnityEngine;
using UnityEngine.XR.Hands;

namespace UI.Settings
{
    [Serializable]
    public sealed class PalmMenuGesture
    {
        [SerializeField, Min(0f)] private float _holdDuration = 0.55f;
        [SerializeField, Min(0f)] private float _minHandLength = 0.114f;
        [SerializeField, Min(0f)] private float _minThumbSpread = 0.06f;
        [SerializeField, Min(0f)] private float _maxPalmDistance = 0.7f;
        [SerializeField, Range(0f, 1f)] private float _fingersUp = 0.55f;
        [SerializeField, Range(0f, 1f)] private float _palmInView = 0.65f;
        [SerializeField, Range(0f, 1f)] private float _palmFacing = 0.65f;

        private readonly List<XRHandSubsystem> _subsystems = new();
        private PlayerRig _rig;
        private XRHandSubsystem _hands;
        private float _heldTime;
        private bool _triggered;

        public void Initialize(PlayerRig rig) => _rig = rig;

        public bool Tick(float deltaTime)
        {
            if (_hands == null || !_hands.running)
            {
                SubsystemManager.GetSubsystems(_subsystems);
                _hands = _subsystems.Find(subsystem => subsystem.running);
            }

            if (_hands == null || !IsPalmRaised(_hands.leftHand))
            {
                _heldTime = 0f;
                _triggered = false;
                return false;
            }

            _heldTime += deltaTime;
            if (_triggered || _heldTime < _holdDuration)
            {
                return false;
            }

            _triggered = true;
            return true;
        }

        private bool IsPalmRaised(XRHand hand)
        {
            if (!hand.isTracked ||
                !hand.GetJoint(XRHandJointID.Wrist).TryGetPose(out Pose wrist) ||
                !hand.GetJoint(XRHandJointID.IndexTip).TryGetPose(out Pose index) ||
                !hand.GetJoint(XRHandJointID.MiddleTip).TryGetPose(out Pose middle) ||
                !hand.GetJoint(XRHandJointID.LittleTip).TryGetPose(out Pose little) ||
                !hand.GetJoint(XRHandJointID.ThumbTip).TryGetPose(out Pose thumb) ||
                !hand.GetJoint(XRHandJointID.Palm).TryGetPose(out Pose palm))
            {
                return false;
            }

            Transform trackingSpace = _rig.TrackingSpace;
            Transform head = _rig.Head;
            Vector3 toPalm = trackingSpace.TransformPoint(palm.position) - head.position;
            Vector3 fingers = middle.position - wrist.position;
            Vector3 normal = Vector3.Cross(index.position - little.position, fingers).normalized;
            return fingers.sqrMagnitude > _minHandLength * _minHandLength
                && (index.position - thumb.position).sqrMagnitude > _minThumbSpread * _minThumbSpread
                && toPalm.sqrMagnitude < _maxPalmDistance * _maxPalmDistance
                && Vector3.Dot(trackingSpace.TransformDirection(fingers.normalized), Vector3.up) > _fingersUp
                && Vector3.Dot(head.forward, toPalm.normalized) > _palmInView
                && Mathf.Abs(Vector3.Dot(trackingSpace.TransformDirection(normal), toPalm.normalized)) > _palmFacing;
        }
    }
}
