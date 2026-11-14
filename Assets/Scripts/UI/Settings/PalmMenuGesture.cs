using System.Collections.Generic;
using Gameplay.Entities.Player;
using UnityEngine;
using UnityEngine.XR.Hands;

namespace UI.Settings
{
    public sealed class PalmMenuGesture
    {
        private const float HoldDuration = 0.55f;

        private readonly List<XRHandSubsystem> _subsystems = new();
        private readonly PlayerRig _rig;
        private XRHandSubsystem _hands;
        private float _heldTime;
        private bool _triggered;

        public PalmMenuGesture(PlayerRig rig)
        {
            _rig = rig;
        }

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
            if (_triggered || _heldTime < HoldDuration)
                return false;

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
                return false;

            Transform trackingSpace = _rig.TrackingSpace;
            Transform head = _rig.Head;
            Vector3 toPalm = trackingSpace.TransformPoint(palm.position) - head.position;
            Vector3 fingers = middle.position - wrist.position;
            Vector3 normal = Vector3.Cross(index.position - little.position, fingers).normalized;
            return fingers.sqrMagnitude > 0.013f
                && (index.position - thumb.position).sqrMagnitude > 0.0036f
                && Vector3.Dot(trackingSpace.TransformDirection(fingers.normalized), Vector3.up) > 0.55f
                && Vector3.Dot(head.forward, toPalm.normalized) > 0.65f
                && Mathf.Abs(Vector3.Dot(trackingSpace.TransformDirection(normal), toPalm.normalized)) > 0.65f
                && toPalm.sqrMagnitude < 0.49f;
        }
    }
}
