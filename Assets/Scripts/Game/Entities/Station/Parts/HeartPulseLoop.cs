using System;
using Services.Audio;
using UnityEngine;

namespace Game.Entities.Station.Parts
{
    /// <summary>
    /// Keeps the heart beating: loops the pulse animation and fires the scanner and heartbeat sound every beat.
    /// </summary>
    [Serializable]
    public class HeartPulseLoop
    {
        private static readonly int PulseState = Animator.StringToHash("Base Layer.Pulse");

        [SerializeField, Min(0.3f)] private float _beatPeriod = 0.9f;
        [SerializeField, Range(0f, 1f)] private float _beatVolume = 0.7f;

        private Animator _animator;
        private HeartScanner _scanner;
        private AudioSource _audio;
        private AudioClip _heartbeat;
        private float _clipLength;
        private float _untilBeat;

        public bool IsRunning { get; private set; }

        public void Init(Animator animator, HeartScanner scanner, AudioSource audio)
        {
            _animator = animator;
            _scanner = scanner;
            _audio = audio;
            _heartbeat = ProceduralTones.CreateHeartbeat("Heartbeat");
            _clipLength = GetPulseClipLength(animator);
            Stop();
        }

        /// <param name="animate">Play the pulse animation; organs that do not beat only get the scanner and sound.</param>
        public void Start(bool animate)
        {
            bool wasRunning = IsRunning;
            IsRunning = true;
            if (animate)
            {
                bool wasAnimating = _animator.speed > 0f;
                _animator.speed = _clipLength / _beatPeriod;
                if (!wasAnimating)
                    _animator.Play(PulseState, 0, 0f);
            }
            else
            {
                ResetAnimation();
            }

            if (!wasRunning)
                _untilBeat = 0f;
        }

        public void Stop()
        {
            IsRunning = false;
            ResetAnimation();
        }

        private void ResetAnimation()
        {
            _animator.Play(PulseState, 0, 0f);
            _animator.Update(0f);
            _animator.speed = 0f;
        }

        /// <param name="suppressed">True while a safety stop is active: effects pause, the beat keeps its rhythm.</param>
        public void Tick(float deltaTime, bool suppressed)
        {
            _scanner.UpdateEffect(suppressed);
            if (!IsRunning)
                return;

            _untilBeat -= deltaTime;
            if (_untilBeat > 0f)
                return;

            _untilBeat += _beatPeriod;
            if (suppressed)
                return;

            _scanner.Play();
            _audio.PlayOneShot(_heartbeat, _beatVolume);
        }

        public void Dispose()
        {
            if (_heartbeat != null)
                UnityEngine.Object.Destroy(_heartbeat);
        }

        private static float GetPulseClipLength(Animator animator)
        {
            foreach (var clip in animator.runtimeAnimatorController.animationClips)
            {
                if (clip.name == "Pulse")
                    return clip.length;
            }

            return 0.75f;
        }
    }
}
