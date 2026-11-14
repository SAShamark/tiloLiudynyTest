using Services.Audio;
using UnityEngine;

namespace Game.Entities.Environment
{
    /// <summary>
    /// One-shot spark shower with a short electric crackle, e.g. when a plug or battery goes in.
    /// <see cref="Emit"/> adds a few sparks at a point without the sound, for continuous effects.
    /// </summary>
    public sealed class SparkBurst : MonoBehaviour
    {
        [SerializeField] private ParticleSystem _sparks;
        [SerializeField, Range(0f, 1f)] private float _volume = 0.35f;
        [SerializeField, Range(0.5f, 2f)] private float _pitch = 1.5f;

        private AudioSource _audio;
        private AudioClip _crackle;

        private void Awake()
        {
            if (_sparks == null)
                _sparks = GetComponentInChildren<ParticleSystem>();

            _crackle = ProceduralTones.CreateZap("Spark crackle");
            _audio = gameObject.AddComponent<AudioSource>();
            _audio.playOnAwake = false;
            _audio.spatialBlend = 1f;
            _audio.minDistance = 0.3f;
            _audio.maxDistance = 5f;
        }

        private void OnDestroy()
        {
            if (_crackle != null)
                Destroy(_crackle);
        }

        public void Play()
        {
            _sparks.Play(true);
            _audio.pitch = _pitch * Random.Range(0.9f, 1.1f);
            _audio.PlayOneShot(_crackle, _volume);
        }

        /// <summary>Emits <paramref name="count"/> sparks at <paramref name="position"/>, optionally with a quiet crackle.</summary>
        public void Emit(Vector3 position, int count, bool crackle)
        {
            var emit = new ParticleSystem.EmitParams { position = position, applyShapeToPosition = true };
            _sparks.Emit(emit, count);
            if (!crackle || _audio.isPlaying)
                return;

            _audio.pitch = _pitch * Random.Range(1.2f, 1.6f);
            _audio.PlayOneShot(_crackle, _volume * 0.5f);
        }
    }
}
