using Game.Entities.Pickups;
using UnityEngine;

namespace Game.Entities.Lab
{
    /// <summary>
    /// One defibrillator paddle. Hangs in its holster, follows the hand while held and is pulled
    /// back by its cable shortly after being let go. Draws a sagging cable to the unit.
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public sealed class DefibPaddle : Pickup
    {
        [SerializeField] private Transform _holster;
        [SerializeField] private Transform _cableStart;
        [Tooltip("Point on the paddle where the cable enters.")]
        [SerializeField] private Transform _cableEnd;
        [Tooltip("Contact face of the paddle; distance to the patient is measured from here.")]
        [SerializeField] private Transform _contact;
        [SerializeField, Min(0f)] private float _returnDelay = 0.4f;
        [SerializeField, Min(0.1f)] private float _returnSpeed = 6f;
        [SerializeField, Min(0f)] private float _cableSag = 0.25f;
        [SerializeField, Range(4, 32)] private int _cableSegments = 16;

        private LineRenderer _cable;
        private float _releasedFor;

        public Vector3 ContactPoint => _contact != null ? _contact.position : transform.position;

        protected override void Awake()
        {
            base.Awake();
            Body.isKinematic = true;
            _cable = GetComponent<LineRenderer>();
            _cable.useWorldSpace = true;
            _cable.positionCount = _cableSegments + 1;
        }

        protected override void FixedUpdate()
        {
            if (IsHeld || _holster == null)
            {
                _releasedFor = 0f;
                return;
            }

            _releasedFor += Time.fixedDeltaTime;
            if (_releasedFor < _returnDelay)
                return;

            float t = 1f - Mathf.Exp(-_returnSpeed * Time.fixedDeltaTime);
            Body.MovePosition(Vector3.Lerp(Body.position, _holster.position, t));
            Body.MoveRotation(Quaternion.Slerp(Body.rotation, _holster.rotation, t));
        }

        private void LateUpdate()
        {
            if (_cableStart == null)
                return;

            Vector3 start = _cableStart.position;
            Vector3 end = _cableEnd != null ? _cableEnd.position : transform.position;
            float sag = _cableSag * Mathf.Clamp01(1.2f - Vector3.Distance(start, end) * 0.6f);
            for (int i = 0; i <= _cableSegments; i++)
            {
                float t = (float)i / _cableSegments;
                Vector3 point = Vector3.Lerp(start, end, t) + Vector3.down * (sag * 4f * t * (1f - t));
                _cable.SetPosition(i, point);
            }
        }
    }
}
