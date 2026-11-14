using UnityEngine;

namespace Game.Entities.Station
{
    [RequireComponent(typeof(LineRenderer))]
    public sealed class StationCable : MonoBehaviour
    {
        private const int Segments = 24;
        private readonly Vector3[] _points = new Vector3[Segments + 1];
        private readonly Vector3[] _previous = new Vector3[Segments + 1];
        private Transform _plug;
        private LineRenderer _line;

        public void Init(Transform plug, Material material)
        {
            _plug = plug;
            _line = GetComponent<LineRenderer>();
            _line.sharedMaterial = material;
            _line.useWorldSpace = true;
            _line.positionCount = _points.Length;
            _line.widthMultiplier = 0.023f;
            _line.numCornerVertices = 3;
            _line.numCapVertices = 3;
            _line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ResetCable();
        }

        private Vector3 PlugEnd => _plug.TransformPoint(0f, -0.95f, 0f);

        private void ResetCable()
        {
            for (int i = 0; i <= Segments; i++)
            {
                float t = (float)i / Segments;
                Vector3 point = Vector3.Lerp(transform.position, PlugEnd, t);
                point.y = Mathf.Max(0.025f, point.y - Mathf.Sin(t * Mathf.PI) * 0.46f);
                _previous[i] = _points[i] = point;
            }
        }

        private void FixedUpdate()
        {
            if (_plug == null)
                return;

            Vector3 end = PlugEnd;
            if ((end - _points[Segments]).sqrMagnitude > 0.85f * 0.85f)
                ResetCable();

            float step = Time.fixedDeltaTime;
            for (int i = 1; i < Segments; i++)
            {
                Vector3 current = _points[i];
                _points[i] += (current - _previous[i]) * 0.985f + Physics.gravity * (step * step);
                _previous[i] = current;
            }

            float segmentLength = Mathf.Max(1.65f, Vector3.Distance(transform.position, end)) / Segments;
            for (int pass = 0; pass < 10; pass++)
            {
                _points[0] = transform.position;
                _points[Segments] = end;
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

                for (int i = 1; i < Segments; i++)
                    _points[i].y = Mathf.Max(0.025f, _points[i].y);
            }
        }

        private void LateUpdate()
        {
            if (_plug == null)
                return;

            _points[0] = transform.position;
            _points[Segments] = PlugEnd;
            _line.SetPositions(_points);
        }
    }
}
