using System;
using UnityEngine;

namespace Game.Entities.Participants.Parts
{
    /// <summary>
    /// Walks a transform around a closed loop of waypoints.
    /// </summary>
    [Serializable]
    public class RouteMovement
    {
        [SerializeField] private float _turnSpeed = 240f;
        [SerializeField] private float _startDelay = 2f;
        [SerializeField] private Vector3[] _route =
        {
            new(-1.3f, 0f, 0.9f),
            new(1.3f, 0f, 0.9f),
            new(1.3f, 0f, 2.1f),
            new(-1.3f, 0f, 2.1f),
        };

        private Transform _body;
        private float _speed;
        private float _delayLeft;
        private int _nextPoint;

        /// <summary>Actual speed this frame, in metres per second (0 while waiting).</summary>
        public float CurrentSpeed { get; private set; }

        public void Init(Transform body, float speed)
        {
            _body = body;
            _speed = speed;
            _delayLeft = _startDelay;
            _nextPoint = 1 % _route.Length;
            _body.SetPositionAndRotation(_route[0], Quaternion.LookRotation(_route[_nextPoint] - _route[0]));
        }

        public void Tick(float deltaTime)
        {
            if (_delayLeft > 0f)
            {
                _delayLeft -= deltaTime;
                CurrentSpeed = 0f;
                return;
            }

            Vector3 target = _route[_nextPoint];
            Vector3 direction = target - _body.position;
            if (direction.sqrMagnitude > 0.0001f)
            {
                Quaternion heading = Quaternion.LookRotation(direction);
                _body.rotation = Quaternion.RotateTowards(_body.rotation, heading, _turnSpeed * deltaTime);
            }

            Vector3 before = _body.position;
            _body.position = Vector3.MoveTowards(before, target, _speed * deltaTime);
            CurrentSpeed = deltaTime > 0f ? Vector3.Distance(before, _body.position) / deltaTime : 0f;
            if (_body.position == target)
                _nextPoint = (_nextPoint + 1) % _route.Length;
        }
    }
}
