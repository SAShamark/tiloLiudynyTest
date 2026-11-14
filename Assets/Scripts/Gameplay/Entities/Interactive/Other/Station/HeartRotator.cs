using Gameplay.Entities.Interactive.BaseLogic.Controls;
using UnityEngine;

namespace Gameplay.Entities.Interactive.Other.Station
{
    public sealed class HeartRotator : HandleControl
    {
        [SerializeField, Min(0.1f)] private float _sensitivity = 1.6f;
        [SerializeField, Min(0f)] private float _returnDelay = 2f;
        [SerializeField, Min(0.1f)] private float _returnSpeed = 2.5f;

        private Quaternion _restRotation;
        private Quaternion _grabRotation;
        private Vector3 _grabDirection;
        private float _releasedTime;

        private void Awake()
        {
            _restRotation = transform.localRotation;
        }

        private void LateUpdate()
        {
            if (IsGrabbed)
            {
                Rotate();
                _releasedTime = 0f;
                return;
            }

            _releasedTime += Time.deltaTime;
            if (_releasedTime < _returnDelay)
            {
                return;
            }

            float blend = 1f - Mathf.Exp(-_returnSpeed * Time.deltaTime);
            transform.localRotation = Quaternion.Slerp(transform.localRotation, _restRotation, blend);
        }

        protected override void OnGrabbed()
        {
            _grabRotation = transform.rotation;
            _grabDirection = DirectionTo(GrabPosition);
        }

        private void Rotate()
        {
            Vector3 current = DirectionTo(HandPosition);
            if (current.sqrMagnitude < 0.0001f || _grabDirection.sqrMagnitude < 0.0001f)
            {
                return;
            }

            Quaternion delta = Quaternion.FromToRotation(_grabDirection, current);
            Quaternion amplified = Quaternion.SlerpUnclamped(Quaternion.identity, delta, _sensitivity);
            transform.rotation = amplified * _grabRotation;
        }

        private Vector3 DirectionTo(Vector3 hand) => (hand - transform.position).normalized;
    }
}
