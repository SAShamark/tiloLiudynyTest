using Services.Audio;
using UnityEngine;

namespace Gameplay.Effects
{
    public sealed class SparkBurst : MonoBehaviour
    {
        [SerializeField] private AudioSource _audio;
        [SerializeField] private ParticleSystem _sparks;
        [SerializeField, Range(0f, 1f)] private float _volume = 0.35f;
        [SerializeField, Range(0.5f, 2f)] private float _pitch = 1.5f;
        private AudioClip _crackle;

        private void Awake()
        {
            _crackle = ProceduralTones.CreateZap("Spark crackle");
        }

        private void OnDestroy()
        {
            Destroy(_crackle);
        }

        public void Play()
        {
            _sparks.Play(true);
            _audio.pitch = _pitch * Random.Range(0.9f, 1.1f);
            _audio.PlayOneShot(_crackle, _volume);
        }

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
