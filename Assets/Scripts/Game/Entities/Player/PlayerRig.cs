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
        private readonly Vector3 _startPosition;
        private readonly Vector3 _startForward;

        public PlayerRig(XROrigin origin)
        {
            _origin = origin;
            _haptics = origin.GetComponentsInChildren<HapticImpulsePlayer>(true);
            _startPosition = origin.transform.position;
            _startForward = Vector3.ProjectOnPlane(origin.transform.forward, Vector3.up).normalized;
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

        /// <param name="area">Allowed floor rectangle in world XZ (x = world X, y = world Z).</param>
        public bool IsOutsidePlayArea(Rect area, float minHeight)
        {
            Vector3 head = Head.position;
            return head.y < minHeight || !area.Contains(new Vector2(head.x, head.z));
        }

        /// <summary>Puts the player back where the session started, facing the same way.</summary>
        public void ReturnToStart()
        {
            _origin.MatchOriginUpCameraForward(Vector3.up, _startForward);
            _origin.MoveCameraToWorldLocation(_startPosition + Vector3.up * _origin.CameraInOriginSpaceHeight);
        }

        public void SendHaptics(float amplitude, float duration)
        {
            foreach (var controller in _haptics)
                controller.SendHapticImpulse(amplitude, duration);
        }

        /// <summary>Vibrates each hand with its own strength, e.g. stronger on the side a danger is on.</summary>
        public void SendHaptics(float left, float right, float duration)
        {
            foreach (var controller in _haptics)
            {
                bool isLeft = IsLeft(controller.transform);
                float amplitude = isLeft ? left : right;
                if (amplitude > 0.01f)
                    controller.SendHapticImpulse(amplitude, duration);
            }
        }

        private static bool IsLeft(Transform controller)
        {
            for (var t = controller; t != null; t = t.parent)
            {
                if (t.name.Contains("Left"))
                    return true;
                if (t.name.Contains("Right"))
                    return false;
            }

            return false;
        }
    }
}
