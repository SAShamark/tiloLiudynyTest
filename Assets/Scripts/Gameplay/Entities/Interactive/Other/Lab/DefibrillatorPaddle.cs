using Gameplay.Entities.Interactive.BaseLogic.Items;
using UnityEngine;

namespace Gameplay.Entities.Interactive.Other.Lab
{
    [RequireComponent(typeof(LineRenderer))]
    public sealed class DefibrillatorPaddle : GrabItem
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

        public Vector3 ContactPoint => _contact.position;

        protected override void Awake()
        {
            base.Awake();
            Body.isKinematic = true;
            _cable = GetComponent<LineRenderer>();
            _cable.useWorldSpace = true;
            _cable.positionCount = _cableSegments + 1;
        }

        private void FixedUpdate()
        {
            if (IsHeld)
            {
                _releasedFor = 0f;
                return;
            }

            _releasedFor += Time.fixedDeltaTime;
            if (_releasedFor < _returnDelay)
                return;

            float followBlend = 1f - Mathf.Exp(-_returnSpeed * Time.fixedDeltaTime);
            Body.MovePosition(Vector3.Lerp(Body.position, _holster.position, followBlend));
            Body.MoveRotation(Quaternion.Slerp(Body.rotation, _holster.rotation, followBlend));
        }

        private void LateUpdate()
        {
            Vector3 start = _cableStart.position;
            Vector3 end = _cableEnd.position;
            float sag = _cableSag * Mathf.Clamp01(1.2f - Vector3.Distance(start, end) * 0.6f);
            for (int i = 0; i <= _cableSegments; i++)
            {
                float alongCable = (float)i / _cableSegments;
                Vector3 point = Vector3.Lerp(start, end, alongCable) + Vector3.down * (sag * 4f * alongCable * (1f - alongCable));
                _cable.SetPosition(i, point);
            }
        }
    }
}
