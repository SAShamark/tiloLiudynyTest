using Gameplay.Entities.Interactive.BaseLogic.Items;
using UnityEngine;

namespace Gameplay.Entities.Interactive.Other.Props
{
    public sealed class SqueezeToy : GrabItem
    {
        [SerializeField] private AudioSource _audio;
        [SerializeField] private AudioClip _squeak;
        [SerializeField] private Transform _visual;
        [SerializeField] private Vector3 _squashScale = new(1.18f, 0.72f, 1.18f);
        [SerializeField, Min(1f)] private float _recoverSpeed = 9f;
        [SerializeField, Range(0f, 1f)] private float _volume = 0.8f;

        private Vector3 _restScale;
        private float _squash;
        private float _lastSqueak = -10f;
        private int _streak;

        protected override void Awake()
        {
            base.Awake();
            _restScale = _visual.localScale;
        }

        private void Update()
        {
            if (_squash <= 0f)
                return;

            _squash = Mathf.MoveTowards(_squash, 0f, Time.deltaTime * _recoverSpeed * 0.5f);
            float wobble = Mathf.Sin((1f - _squash) * Mathf.PI * 3f) * 0.08f * _squash;
            _visual.localScale = Vector3.Scale(_restScale, Vector3.LerpUnclamped(Vector3.one, _squashScale, _squash + wobble));
        }

        private void OnCollisionEnter(Collision collision)
        {
            float impact = collision.relativeVelocity.magnitude;
            if (impact < 1.6f || Time.time - _lastSqueak < 0.25f)
                return;

            _lastSqueak = Time.time;
            _audio.pitch = Mathf.Lerp(0.95f, 1.25f, Mathf.InverseLerp(1.6f, 6f, impact)) + Random.Range(-0.05f, 0.05f);
            _audio.PlayOneShot(_squeak, _volume * Mathf.Clamp01(impact / 5f));
            _squash = Mathf.Clamp01(impact / 4f);
        }

        protected override void OnActivated()
        {
            _streak = Time.time - _lastSqueak < 0.6f ? Mathf.Min(_streak + 1, 6) : 0;
            _lastSqueak = Time.time;

            _audio.pitch = 1f + _streak * 0.08f + Random.Range(-0.04f, 0.04f);
            _audio.PlayOneShot(_squeak, _volume);
            _squash = 1f;
            SendHaptics(0.35f, 0.08f);
        }
    }
}
