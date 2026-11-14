using UnityEngine;

namespace Gameplay.Effects
{
    public sealed class EffectPlayer : MonoBehaviour
    {
        [SerializeField] private bool _loop;
        [Tooltip("Playback speed of the whole effect; stock effects are often slow for close-up VR.")]
        [SerializeField, Min(0.1f)] private float _speed = 1.5f;

        private ParticleSystem[] _systems;

        public bool IsPlaying { get; private set; }

        private void Awake()
        {
            _systems = GetComponentsInChildren<ParticleSystem>(true);
            foreach (var system in _systems)
            {
                var main = system.main;
                main.scalingMode = ParticleSystemScalingMode.Hierarchy;
                main.playOnAwake = false;
                main.loop = _loop;
                main.simulationSpeed = _speed;
            }

            Stop(immediately: true);
        }

        public void Play()
        {
            IsPlaying = true;
            foreach (var system in _systems)
            {
                system.Clear(false);
                system.Play(false);
            }
        }

        public void Stop(bool immediately = false)
        {
            IsPlaying = false;
            var behaviour = immediately
                ? ParticleSystemStopBehavior.StopEmittingAndClear
                : ParticleSystemStopBehavior.StopEmitting;
            foreach (var system in _systems)
                system.Stop(false, behaviour);
        }
    }
}
