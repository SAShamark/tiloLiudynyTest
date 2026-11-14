using System.Collections.Generic;
using UnityEngine;

namespace Gameplay.Entities.Player.Hands
{
    internal sealed class HandFinger
    {
        private const int FitSteps = 24;

        private readonly Transform[] _joints;
        private readonly Quaternion[] _rest;
        private readonly Vector3[] _axes;
        private readonly float[] _maxCurl;
        private readonly Transform _tip;
        private readonly bool _isIndex;
        private float _amount;
        private float _fitted = 1f;

        public HandFinger(Transform[] joints, Transform tip, Vector3 curlAxisWorld, bool isThumb, bool isIndex)
        {
            _joints = joints;
            _tip = tip;
            _isIndex = isIndex;
            _rest = new Quaternion[joints.Length];
            _axes = new Vector3[joints.Length];
            _maxCurl = new float[joints.Length];
            for (int i = 0; i < joints.Length; i++)
            {
                _rest[i] = joints[i].localRotation;
                _axes[i] = joints[i].InverseTransformDirection(curlAxisWorld);
                _maxCurl[i] = isThumb ? 28f : i == 0 ? 70f : 78f;
            }
        }

        public IEnumerable<Collider> CreateColliders(float radius, int layer)
        {
            for (int i = 0; i < _joints.Length; i++)
            {
                Vector3 end = i + 1 < _joints.Length ? _joints[i + 1].position : TipPosition();
                var holder = new GameObject(_joints[i].name + " collider");
                holder.layer = layer;
                holder.transform.SetParent(_joints[i], false);
                var capsule = holder.AddComponent<CapsuleCollider>();
                Vector3 localEnd = _joints[i].InverseTransformPoint(end);
                float length = localEnd.magnitude;
                capsule.direction = DominantAxis(localEnd);
                capsule.center = localEnd * 0.5f;
                float scale = _joints[i].lossyScale.x;
                capsule.radius = radius / Mathf.Max(scale, 0.0001f);
                capsule.height = length + capsule.radius * 2f;
                yield return capsule;
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

        private static int DominantAxis(Vector3 vector)
        {
            Vector3 absolute = new Vector3(Mathf.Abs(vector.x), Mathf.Abs(vector.y), Mathf.Abs(vector.z));
            return absolute.x >= absolute.y && absolute.x >= absolute.z ? 0 : absolute.y >= absolute.z ? 1 : 2;
        }
    }
}
