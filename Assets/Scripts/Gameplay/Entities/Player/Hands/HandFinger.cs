using System;
using UnityEngine;

namespace Gameplay.Entities.Player.Hands
{
    [Serializable]
    public sealed class HandFinger
    {
        private const int FitSteps = 24;

        [SerializeField] private Transform[] _joints;
        [SerializeField] private Transform _tip;
        [SerializeField] private Vector3[] _axes;
        [SerializeField] private float[] _maxCurl;
        [SerializeField] private bool _isIndex;

        private Quaternion[] _rest;
        private float _amount;
        private float _fitted = 1f;

        public void Initialize()
        {
            _rest = new Quaternion[_joints.Length];
            for (int i = 0; i < _joints.Length; i++)
            {
                _rest[i] = _joints[i].localRotation;
            }
        }

        public void FitTo(Collider[] item, float radius)
        {
            _fitted = 1f;
            for (int step = 0; step <= FitSteps; step++)
            {
                float amount = (float)step / FitSteps;
                Pose(amount);
                if (!Touches(item, radius))
                    continue;

                _fitted = Mathf.Max(0f, amount - 0.5f / FitSteps);
                break;
            }

            Pose(_amount);
        }

        public void Tick(bool holding, float grip, float trigger, float blend)
        {
            float target = holding ? _fitted : _isIndex ? Mathf.Max(grip, trigger) : grip;
            _amount = Mathf.Lerp(_amount, target, holding ? Mathf.Max(blend, 0.35f) : blend);
            Pose(_amount);
        }

        private void Pose(float amount)
        {
            for (int i = 0; i < _joints.Length; i++)
                _joints[i].localRotation = _rest[i] * Quaternion.AngleAxis(_maxCurl[i] * amount, _axes[i]);
        }

        private bool Touches(Collider[] item, float radius)
        {
            for (int i = 0; i < _joints.Length; i++)
            {
                Vector3 start = _joints[i].position;
                Vector3 end = i + 1 < _joints.Length ? _joints[i + 1].position : TipPosition();
                for (int sample = 1; sample <= 2; sample++)
                {
                    Vector3 point = Vector3.Lerp(start, end, sample * 0.5f);
                    foreach (var collider in item)
                    {
                        if (!collider.enabled || collider.isTrigger || !SupportsClosestPoint(collider))
                            continue;
                        if ((collider.ClosestPoint(point) - point).sqrMagnitude <= radius * radius)
                            return true;
                    }
                }
            }

            return false;
        }

        private Vector3 TipPosition()
        {
            if (_tip != null)
                return _tip.position;

            int last = _joints.Length - 1;
            Vector3 previous = last > 0 ? _joints[last - 1].position : _joints[last].parent.position;
            return _joints[last].position + (_joints[last].position - previous) * 0.75f;
        }

        private static bool SupportsClosestPoint(Collider collider) =>
            collider is not MeshCollider mesh || mesh.convex;
    }
}
