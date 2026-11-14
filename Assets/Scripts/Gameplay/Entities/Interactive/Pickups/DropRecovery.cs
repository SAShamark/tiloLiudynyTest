using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace Gameplay.Entities.Interactive.Pickups
{
    [Serializable]
    public class DropRecovery
    {
        [Tooltip("Off: the item stays wherever it falls.")]
        [SerializeField] private bool _enabled = true;
        [SerializeField] private float _returnBelowHeight = 0.35f;
        [SerializeField] private float _returnDelay = 0.6f;

        private Rigidbody _body;
        private XRGrabInteractable _grab;
        private Vector3 _homePosition;
        private Quaternion _homeRotation;
        private float _timeBelowHeight;

        public void Initialize(Rigidbody body, XRGrabInteractable grab)
        {
            _body = body;
            _grab = grab;
            _homePosition = body.position;
            _homeRotation = body.rotation;
        }

        public bool FixedTick(float deltaTime)
        {
            if (!_enabled || _grab.isSelected || _body.isKinematic || _body.position.y >= _returnBelowHeight)
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
