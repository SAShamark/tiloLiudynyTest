using System;
using UnityEngine;

namespace Gameplay.Entities.Interactive.Station
{
    [Serializable]
    public class HeartPulseLoop
    {
        private static readonly int PulseState = Animator.StringToHash("Base Layer.Pulse");

        [SerializeField, Min(0.3f)] private float _beatPeriod = 0.9f;
        [SerializeField, Range(0f, 1f)] private float _beatVolume = 0.7f;
        [SerializeField] private AudioClip _heartbeat;

        private Animator _animator;
        private HeartScanner _scanner;
        private AudioSource _audio;
        private float _clipLength;
        private float _untilBeat;

        public bool IsRunning { get; private set; }
        public float BeatPeriod => _beatPeriod;

        public void Initialize(Animator animator, HeartScanner scanner, AudioSource audio)
        {
            _animator = animator;
            _scanner = scanner;
            _audio = audio;
            _clipLength = GetPulseClipLength(animator);
            Stop();
        }

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
