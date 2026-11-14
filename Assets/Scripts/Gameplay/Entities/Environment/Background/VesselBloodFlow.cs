using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Gameplay.Entities.Environment.Background
{
    [RequireComponent(typeof(ParticleSystem))]
    public sealed class VesselBloodFlow : MonoBehaviour
    {
        private const int Capacity = 140;

        [SerializeField] private VesselTunnel _tunnel;
        [SerializeField] private Material _cellMaterial;
        [SerializeField, Min(0.1f)] private float _cellsPerSecond = 9f;
        [SerializeField] private Vector2 _lifetime = new(7f, 11f);
        [SerializeField] private Vector2 _sizeRange = new(0.22f, 0.75f);
        [SerializeField] private Vector2 _spinSpeed = new(25f, 110f);
        [Tooltip("How far from the tunnel axis cells may travel, relative to the tunnel radius.")]
        [SerializeField, Range(0.1f, 0.95f)] private float _laneSpread = 0.8f;
        [Tooltip("Where along the tunnel cells are born (0 = window plane, negative = behind the capsule).")]
        [SerializeField, Range(-0.4f, 0.1f)] private float _startOffset;
        [Tooltip("Half size of the window opening around the tunnel axis; cells start outside it (hidden by the front wall) and drift into view.")]
        [SerializeField] private Vector2 _capsuleHalfExtents = new(2.8f, 1.7f);
        [Tooltip("Tunnel fraction after the window over which cells drift from the capsule edge into their lane.")]
        [SerializeField, Range(0.01f, 0.3f)] private float _entryLength = 0.12f;
        [SerializeField] private Color _tintA = new(1f, 0.92f, 0.92f);
        [SerializeField] private Color _tintB = new(0.72f, 0.55f, 0.58f);

        private readonly ParticleSystem.Particle[] _active = new ParticleSystem.Particle[Capacity];
        private ParticleSystem _particles;
        private Mesh _cellMesh;
        private float _untilEmission;

        private void Awake()
        {
            _particles = GetComponent<ParticleSystem>();
            var main = _particles.main;
            main.playOnAwake = false;
            main.loop = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startSpeed = 0f;
            main.gravityModifier = 0f;
            main.maxParticles = Capacity;
            main.startRotation3D = true;
            var emission = _particles.emission;
            emission.enabled = false;
            var shape = _particles.shape;
            shape.enabled = false;

            _cellMesh = BloodCellMesh.Create();
            var particleRenderer = GetComponent<ParticleSystemRenderer>();
            particleRenderer.renderMode = ParticleSystemRenderMode.Mesh;
            particleRenderer.mesh = _cellMesh;
            particleRenderer.sharedMaterial = _cellMaterial;
            particleRenderer.alignment = ParticleSystemRenderSpace.World;
            particleRenderer.shadowCastingMode = ShadowCastingMode.Off;
            particleRenderer.receiveShadows = false;
            particleRenderer.SetActiveVertexStreams(new List<ParticleSystemVertexStream>
            {
                ParticleSystemVertexStream.Position,
                ParticleSystemVertexStream.Normal,
                ParticleSystemVertexStream.Color,
            });
        }

        private void OnEnable()
        {
            _particles.Play();
            float averageLifetime = (_lifetime.x + _lifetime.y) * 0.5f;
            int initialCount = Mathf.Min(Capacity, Mathf.RoundToInt(_cellsPerSecond * averageLifetime));
            for (int index = 0; index < initialCount; index++)
                EmitCell();

            int count = _particles.GetParticles(_active);
            for (int index = 0; index < count; index++)
                _active[index].remainingLifetime *= Random.Range(0.02f, 1f);

            ApplyFlow(count);
            _untilEmission = NextInterval();
        }

        private void OnDisable()
        {
            _particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        private void OnDestroy()
        {
            Destroy(_cellMesh);
        }

        private void LateUpdate()
        {
            _untilEmission -= Time.deltaTime;
            while (_untilEmission <= 0f)
            {
                EmitCell();
                _untilEmission += NextInterval();
            }

            ApplyFlow(_particles.GetParticles(_active));
        }

        private float NextInterval() => Random.Range(0.35f, 1.65f) / _cellsPerSecond;

        private void EmitCell()
        {
            _particles.Emit(new ParticleSystem.EmitParams
            {
                startLifetime = Random.Range(_lifetime.x, _lifetime.y),
                randomSeed = (uint)Random.Range(1, int.MaxValue),
            }, 1);
        }

        private void ApplyFlow(int count)
        {
            for (int index = 0; index < count; index++)
            {
                ref var particle = ref _active[index];
                uint seed = particle.randomSeed;
                float age = particle.startLifetime - particle.remainingLifetime;
                float progress = 1f - particle.remainingLifetime / particle.startLifetime;
                float alongTunnel = Mathf.Lerp(_startOffset, 1f, progress);

                _tunnel.GetFrame(alongTunnel, out _, out Vector3 right, out Vector3 up);
                float angle = Sample(seed, 1) * Mathf.PI * 2f + Mathf.Sin(age * 0.6f + Sample(seed, 7) * 6.28f) * 0.15f;
                float lane = Mathf.Sqrt(Sample(seed, 2)) * _laneSpread;
                float laneRadius = _tunnel.GetRadius(alongTunnel) * lane;
                float clearRadius = CapsuleClearance(angle) + Sample(seed, 14) * 0.4f;
                float entry = Mathf.SmoothStep(0f, 1f, alongTunnel / _entryLength);
                float radius = Mathf.Lerp(Mathf.Max(laneRadius, clearRadius), laneRadius, entry);
                Vector3 offset = (right * Mathf.Cos(angle) + up * Mathf.Sin(angle)) * radius;
                particle.position = _tunnel.GetPoint(alongTunnel) + offset;

                float grow = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(_startOffset, _startOffset + 0.04f, alongTunnel));
                particle.startSize = Mathf.Lerp(_sizeRange.x, _sizeRange.y, Sample(seed, 3) * Sample(seed, 4)) * grow;

                var baseRotation = new Vector3(Sample(seed, 5), Sample(seed, 6), Sample(seed, 8)) * 360f;
                var spinAxis = new Vector3(Sample(seed, 9) - 0.5f, Sample(seed, 10) - 0.5f, Sample(seed, 11) - 0.5f).normalized;
                float spin = Mathf.Lerp(_spinSpeed.x, _spinSpeed.y, Sample(seed, 12));
                particle.rotation3D = baseRotation + spinAxis * (spin * age);

                particle.startColor = Color.Lerp(_tintA, _tintB, Sample(seed, 13));
            }

            _particles.SetParticles(_active, count);
        }

        private float CapsuleClearance(float angle)
        {
            float horizontal = Mathf.Abs(Mathf.Cos(angle)) / _capsuleHalfExtents.x;
            float vertical = Mathf.Abs(Mathf.Sin(angle)) / _capsuleHalfExtents.y;
            return 1f / Mathf.Max(horizontal, vertical, 0.0001f);
        }

        private static float Sample(uint seed, uint channel)
        {
            unchecked
            {
                uint bits = seed + channel * 0x9E3779B9u;
                bits ^= bits >> 16;
                bits *= 0x7FEB352Du;
                bits ^= bits >> 15;
                bits *= 0x846CA68Bu;
                bits ^= bits >> 16;
                return (bits & 0xFFFFFFu) / 16777216f;
            }
        }
    }
}
