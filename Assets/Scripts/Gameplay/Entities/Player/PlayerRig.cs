using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;

namespace Gameplay.Entities.Player
{
    public sealed class PlayerRig
    {
        private readonly XROrigin _origin;
        private readonly HapticImpulsePlayer _leftHaptics;
        private readonly HapticImpulsePlayer _rightHaptics;
        private readonly Vector3 _startPosition;
        private readonly Vector3 _startForward;

        public PlayerRig(XROrigin origin, HapticImpulsePlayer leftHaptics, HapticImpulsePlayer rightHaptics)
        {
            _origin = origin;
            _leftHaptics = leftHaptics;
            _rightHaptics = rightHaptics;
            _startPosition = origin.transform.position;
            _startForward = Vector3.ProjectOnPlane(origin.transform.forward, Vector3.up).normalized;
        }

        public Camera Camera => _origin.Camera;
        public Transform Head => _origin.Camera.transform;
        public Transform TrackingSpace => _origin.CameraFloorOffsetObject.transform;

        public Vector3 FlatForward
        {
            get
            {
                Transform head = Head;
                Vector3 forward = Vector3.ProjectOnPlane(head.forward, Vector3.up);
                if (forward.sqrMagnitude < 0.001f)
                {
                    forward = Vector3.Cross(head.right, Vector3.up);
                }

                return forward.normalized;
            }
        }

        public Vector3 FlatOffsetTo(Vector3 worldPosition)
        {
            Vector3 offset = worldPosition - Head.position;
            offset.y = 0f;
            return offset;
        }

        public bool IsOutsidePlayArea(Rect area, float minHeight)
        {
            Vector3 head = Head.position;
            return head.y < minHeight || !area.Contains(new Vector2(head.x, head.z));
        }

        public void ReturnToStart()
        {
            _origin.MatchOriginUpCameraForward(Vector3.up, _startForward);
            _origin.MoveCameraToWorldLocation(_startPosition + Vector3.up * _origin.CameraInOriginSpaceHeight);
        }

        public void SendHaptics(float left, float right, float duration)
        {
            if (left > 0.01f)
            {
                _leftHaptics.SendHapticImpulse(left, duration);
            }

            if (right > 0.01f)
            {
                _rightHaptics.SendHapticImpulse(right, duration);
            }
        }
    }
}
