using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;

namespace Game.Entities.Player
{
    /// <summary>
    /// Thin facade over the XR Origin: head pose, tracking space, controller haptics and recentering.
    /// </summary>
    public sealed class PlayerRig
    {
        private readonly XROrigin _origin;
        private readonly HapticImpulsePlayer[] _haptics;

        public PlayerRig(XROrigin origin)
        {
            _origin = origin;
            _haptics = origin.GetComponentsInChildren<HapticImpulsePlayer>(true);
        }

        public Camera Camera => _origin.Camera;
        public Transform Head => _origin.Camera.transform;
        public Transform TrackingSpace => _origin.CameraFloorOffsetObject.transform;
        public IReadOnlyList<HapticImpulsePlayer> Haptics => _haptics;

        public Vector3 FlatForward
        {
            get
            {
                Transform head = Head;
                Vector3 forward = Vector3.ProjectOnPlane(head.forward, Vector3.up);
                if (forward.sqrMagnitude < 0.001f)
                    forward = Vector3.Cross(head.right, Vector3.up);

                return forward.normalized;
            }
        }

        public T FindInRig<T>() where T : Component => _origin.GetComponentInChildren<T>(true);

        public T[] FindAllInRig<T>() where T : Component => _origin.GetComponentsInChildren<T>(true);

        public Vector3 FlatOffsetTo(Vector3 worldPosition)
        {
            Vector3 offset = worldPosition - Head.position;
            offset.y = 0f;
            return offset;
        }

        public bool IsOutsidePlayArea(float halfSize, float minHeight)
        {
            Vector3 head = Head.position;
            return head.y < minHeight || Mathf.Abs(head.x) > halfSize || Mathf.Abs(head.z) > halfSize;
        }

        public void ReturnToStart()
        {
            _origin.MatchOriginUpCameraForward(Vector3.up, Vector3.forward);
            _origin.MoveCameraToWorldLocation(Vector3.up * _origin.CameraInOriginSpaceHeight);
        }

        public void SendHaptics(float amplitude, float duration)
        {
            foreach (var controller in _haptics)
                controller.SendHapticImpulse(amplitude, duration);
        }
    }
}
