using UnityEngine;

namespace Gameplay.Entities.Environment.Background
{
    [ExecuteAlways]
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public sealed class VesselTunnel : MonoBehaviour
    {
        [SerializeField, Min(2f)] private float _length = 30f;
        [SerializeField, Min(0.5f)] private float _startRadius = 4.2f;
        [SerializeField, Min(0.5f)] private float _endRadius = 2.4f;
        [Tooltip("Sideways offset of the far end, in metres.")]
        [SerializeField] private float _bend = 7f;
        [Tooltip("Vertical offset of the far end, in metres.")]
        [SerializeField] private float _rise = 1.2f;
        [Tooltip("Relative bumpiness of the wall.")]
        [SerializeField, Range(0f, 0.3f)] private float _wobble = 0.08f;
        [SerializeField, Range(8, 128)] private int _lengthSegments = 56;
        [SerializeField, Range(8, 96)] private int _radialSegments = 40;

        private Mesh _mesh;
        private bool _dirty = true;

        public float Length => _length;

        public Vector3 GetPoint(float alongTunnel) => transform.TransformPoint(LocalPoint(alongTunnel));

        public float GetRadius(float alongTunnel) => Mathf.Lerp(_startRadius, _endRadius, alongTunnel) * transform.lossyScale.x;

        public void GetFrame(float alongTunnel, out Vector3 tangent, out Vector3 right, out Vector3 up)
        {
            LocalFrame(alongTunnel, out tangent, out right, out up);
            tangent = transform.TransformDirection(tangent);
            right = transform.TransformDirection(right);
            up = transform.TransformDirection(up);
        }

        private void OnEnable() => _dirty = true;

        private void OnValidate() => _dirty = true;

        private void Update()
        {
            if (_dirty)
                Rebuild();
        }

        private void OnDestroy()
        {
            if (_mesh == null)
                return;

            if (Application.isPlaying)
                Destroy(_mesh);
            else
                DestroyImmediate(_mesh);
        }

        private Vector3 LocalPoint(float alongTunnel) => new(_bend * alongTunnel * alongTunnel, _rise * alongTunnel * alongTunnel, _length * alongTunnel);

        private void LocalFrame(float alongTunnel, out Vector3 tangent, out Vector3 right, out Vector3 up)
        {
            tangent = new Vector3(2f * _bend * alongTunnel, 2f * _rise * alongTunnel, _length).normalized;
            right = Vector3.Cross(Vector3.up, tangent).normalized;
            up = Vector3.Cross(tangent, right);
        }

        private void Rebuild()
        {
            _dirty = false;
            if (_mesh == null)
            {
                _mesh = new Mesh { name = "Vessel Tunnel" };
                _mesh.hideFlags = HideFlags.DontSave;
                GetComponent<MeshFilter>().sharedMesh = _mesh;
            }

            int rings = _lengthSegments + 1;
            int columns = _radialSegments + 1;
            int capCenter = rings * columns;
            var vertices = new Vector3[capCenter + 1 + columns];
            var normals = new Vector3[vertices.Length];
            var uvs = new Vector2[vertices.Length];
            var triangles = new int[_lengthSegments * _radialSegments * 6 + _radialSegments * 3];

            for (int i = 0; i < rings; i++)
            {
                float alongTunnel = (float)i / _lengthSegments;
                Vector3 center = LocalPoint(alongTunnel);
                LocalFrame(alongTunnel, out _, out Vector3 right, out Vector3 up);
                float radius = Mathf.Lerp(_startRadius, _endRadius, alongTunnel);

                for (int j = 0; j < columns; j++)
                {
                    float aroundRing = (float)j / _radialSegments;
                    float angle = aroundRing * Mathf.PI * 2f;
                    Vector3 direction = right * Mathf.Cos(angle) + up * Mathf.Sin(angle);
                    float bump = 1f + _wobble * (Mathf.Sin(alongTunnel * 23f + angle * 3f) * 0.6f + Mathf.Sin(alongTunnel * 51f - angle * 5f) * 0.4f);
                    int index = i * columns + j;
                    vertices[index] = center + direction * radius * bump;
                    normals[index] = -direction;
                    uvs[index] = new Vector2(aroundRing, alongTunnel * _length);
                }
            }

            int triangleIndex = 0;
            for (int i = 0; i < _lengthSegments; i++)
            {
                for (int j = 0; j < _radialSegments; j++)
                {
                    int vertex = i * columns + j;
                    int nextRingVertex = vertex + columns;
                    triangles[triangleIndex++] = vertex;
                    triangles[triangleIndex++] = vertex + 1;
                    triangles[triangleIndex++] = nextRingVertex;
                    triangles[triangleIndex++] = nextRingVertex;
                    triangles[triangleIndex++] = vertex + 1;
                    triangles[triangleIndex++] = nextRingVertex + 1;
                }
            }

            LocalFrame(1f, out Vector3 endTangent, out _, out _);
            vertices[capCenter] = LocalPoint(1f);
            normals[capCenter] = -endTangent;
            uvs[capCenter] = new Vector2(0.5f, _length);
            int lastRing = (rings - 1) * columns;
            for (int j = 0; j < columns; j++)
            {
                vertices[capCenter + 1 + j] = vertices[lastRing + j];
                normals[capCenter + 1 + j] = -endTangent;
                uvs[capCenter + 1 + j] = uvs[lastRing + j];
            }

            for (int j = 0; j < _radialSegments; j++)
            {
                triangles[triangleIndex++] = capCenter;
                triangles[triangleIndex++] = capCenter + 1 + j;
                triangles[triangleIndex++] = capCenter + 2 + j;
            }

            _mesh.Clear();
            _mesh.vertices = vertices;
            _mesh.normals = normals;
            _mesh.uv = uvs;
            _mesh.triangles = triangles;
            _mesh.RecalculateBounds();
        }
    }
}
