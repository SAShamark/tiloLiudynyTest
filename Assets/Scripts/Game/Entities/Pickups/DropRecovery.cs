using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace Game.Entities.Pickups
{
    /// <summary>
    /// Returns a grabbable item to where it started if it lies on the floor for a moment.
    /// </summary>
    [Serializable]
    public class DropRecovery
    {
        [SerializeField] private float _returnBelowHeight = 0.35f;
        [SerializeField] private float _returnDelay = 0.6f;

        private Rigidbody _body;
        private XRGrabInteractable _grab;
        private Vector3 _homePosition;
        private Quaternion _homeRotation;
        private float _timeBelowHeight;

        public void Init(Rigidbody body, XRGrabInteractable grab)
        {
            _body = body;
            _grab = grab;
            _homePosition = body.position;
            _homeRotation = body.rotation;
        }

        /// <returns>True on the step the item was sent home.</returns>
        public bool FixedTick(float deltaTime)
        {
            // Parked items (on a tray, in a slot) are held in place on purpose, not dropped.
            if (_grab.isSelected || _body.isKinematic || _body.position.y >= _returnBelowHeight)
            {
                _timeBelowHeight = 0f;
                return false;
            }

            _timeBelowHeight += deltaTime;
            if (_timeBelowHeight < _returnDelay)
                return false;

            _body.linearVelocity = Vector3.zero;
            _body.angularVelocity = Vector3.zero;
            _body.position = _homePosition;
            _body.rotation = _homeRotation;
            _timeBelowHeight = 0f;
            return true;
        }

        public void SetHome(Vector3 position, Quaternion rotation)
        {
            _homePosition = position;
            _homeRotation = rotation;
        }

        public void Reset() => _timeBelowHeight = 0f;
    }
}
