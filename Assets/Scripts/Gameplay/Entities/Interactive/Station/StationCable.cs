using UnityEngine;

namespace Gameplay.Entities.Interactive.Station
{
    [RequireComponent(typeof(LineRenderer))]
    public sealed class StationCable : MonoBehaviour
    {
        private const int Segments = 24;
        private const int ConstraintPasses = 10;
        private const int CollisionPasses = 3;
        private const float CableRadius = 0.015f;
        private const float FloorHeight = 0.025f;
        private const float ConnectorLead = 0.085f;

        private readonly Vector3[] _points = new Vector3[Segments + 1];
        private readonly Vector3[] _previous = new Vector3[Segments + 1];
        private readonly Vector3[] _renderPoints = new Vector3[Segments + 3];
        private readonly Collider[] _overlaps = new Collider[8];
        private Transform _plug;
        private LineRenderer _line;
        private SphereCollider _probe;

        public void Initialize(Transform plug, Material material)
        {
            _plug = plug;
            _line = GetComponent<LineRenderer>();
            _line.sharedMaterial = material;
            _line.useWorldSpace = true;
            _line.positionCount = _renderPoints.Length;
            _line.widthMultiplier = 0.023f;
            _line.numCornerVertices = 3;
            _line.numCapVertices = 3;
            _line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            var probeObject = new GameObject("Cable collision probe");
            probeObject.transform.SetParent(transform, false);
            _probe = probeObject.AddComponent<SphereCollider>();
            _probe.isTrigger = true;
            _probe.radius = CableRadius;

            ResetCable();
        }

        private Vector3 PlugEnd => _plug.TransformPoint(0f, -0.95f, 0f);
        private Vector3 SourceLead => transform.position - transform.forward * ConnectorLead;
        private Vector3 PlugLead => PlugEnd - _plug.up * ConnectorLead;

        private void ResetCable()
        {
            for (int i = 0; i <= Segments; i++)
            {
                float alongCable = (float)i / Segments;
                Vector3 point = Vector3.Lerp(SourceLead, PlugLead, alongCable);
                point.y = Mathf.Max(FloorHeight, point.y - Mathf.Sin(alongCable * Mathf.PI) * 0.46f);
                _previous[i] = _points[i] = point;
            }
        }

        private void FixedUpdate()
        {
            Vector3 end = PlugLead;
            if ((end - _points[Segments]).sqrMagnitude > 0.85f * 0.85f)
                ResetCable();

            float step = Time.fixedDeltaTime;
            for (int i = 1; i < Segments; i++)
            {
                Vector3 current = _points[i];
                _points[i] += (current - _previous[i]) * 0.985f + Physics.gravity * (step * step);
                _previous[i] = current;
            }

            float segmentLength = Mathf.Max(1.65f - 2f * ConnectorLead, Vector3.Distance(SourceLead, end)) / Segments;
            for (int pass = 0; pass < ConstraintPasses; pass++)
            {
                _points[0] = SourceLead;
                _points[Segments] = end;
                SolveLengths(segmentLength);

                for (int i = 1; i < Segments; i++)
                {
                    _points[i].y = Mathf.Max(FloorHeight, _points[i].y);
                    if (pass >= ConstraintPasses - CollisionPasses)
                        _points[i] = PushOutOfGeometry(_points[i]);
                }
            }
        }

        private void SolveLengths(float segmentLength)
        {
            for (int i = 0; i < Segments; i++)
            {
                Vector3 delta = _points[i + 1] - _points[i];
                float distance = delta.magnitude;
                if (distance < 0.0001f)
                    continue;

                Vector3 correction = delta * ((distance - segmentLength) / distance);
                if (i > 0)
                    _points[i] += correction * (i == Segments - 1 ? 1f : 0.5f);
                if (i < Segments - 1)
                    _points[i + 1] -= correction * (i == 0 ? 1f : 0.5f);
            }
        }

        private Vector3 PushOutOfGeometry(Vector3 point)
        {
            int count = Physics.OverlapSphereNonAlloc(point, CableRadius, _overlaps, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                var other = _overlaps[i];
                if (other == _probe || other.attachedRigidbody != null)
                    continue;

                if (Physics.ComputePenetration(_probe, point, Quaternion.identity,
                        other, other.transform.position, other.transform.rotation,
                        out Vector3 direction, out float distance))
                    point += direction * distance;
            }

            return point;
        }

        private void LateUpdate()
        {
            _renderPoints[0] = transform.position;
            _renderPoints[1] = SourceLead;
            for (int i = 1; i < Segments; i++)
                _renderPoints[i + 1] = _points[i];
            _renderPoints[Segments + 1] = PlugLead;
            _renderPoints[Segments + 2] = PlugEnd;
            _line.SetPositions(_renderPoints);
        }
    }
}
