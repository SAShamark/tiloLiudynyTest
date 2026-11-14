using Gameplay.Effects;
using Gameplay.Entities.Interactive.Base.Buttons;
using UnityEngine;

namespace Gameplay.Entities.Interactive.Props
{
    public sealed class CoffeeMachine : MonoBehaviour
    {
        [SerializeField] private AudioSource _audio;
        [SerializeField] private AudioClip _ready;
        [SerializeField] private AudioClip _sad;
        [SerializeField] private PressButton _button;
        [SerializeField] private Transform _spout;
        [SerializeField] private Transform _stream;
        [SerializeField] private Renderer _statusLamp;
        [SerializeField] private Color _readyColor = new(0.2f, 1f, 0.4f);
        [SerializeField] private Color _brewingColor = new(1f, 0.55f, 0.1f);
        [SerializeField, Min(0.5f)] private float _brewDuration = 3.5f;
        [SerializeField, Min(0.01f)] private float _catchRadius = 0.06f;
        [SerializeField, Min(0.05f)] private float _streamLength = 0.27f;

        private readonly Collider[] _hits = new Collider[8];
        private float _brewLeft;
        private readonly RendererTint _tint = new();

        private void Awake()
        {
            SetStream(0f);
            SetLamp(_readyColor);
        }

        private void OnEnable() => _button.Pressed += HandlePressed;

        private void OnDisable() => _button.Pressed -= HandlePressed;

        private void Update()
        {
            if (_brewLeft <= 0f)
                return;

            _brewLeft -= Time.deltaTime;
            CoffeeMug mug = FindMugUnderSpout();
            float bottom = mug != null ? Mathf.Max(0f, _spout.position.y - mug.SurfaceHeight) : _streamLength;
            SetStream(Mathf.Min(bottom, _streamLength));
            if (mug != null)
                mug.AddCoffee(Time.deltaTime / _brewDuration);

            if (_brewLeft > 0f)
                return;

            _audio.Stop();
            SetStream(0f);
            SetLamp(_readyColor);
            _audio.PlayOneShot(mug != null ? _ready : _sad, 0.7f);
        }

        private void HandlePressed()
        {
            if (_brewLeft > 0f)
                return;

            _brewLeft = _brewDuration;
            _audio.Play();
            SetLamp(_brewingColor);
        }

        private CoffeeMug FindMugUnderSpout()
        {
            Vector3 spout = _spout.position;
            Vector3 bottom = spout - Vector3.up * (_streamLength + 0.03f);
            int count = Physics.OverlapCapsuleNonAlloc(spout, bottom, _catchRadius, _hits, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                var body = _hits[i].attachedRigidbody;
                if (body == null || !body.TryGetComponent(out CoffeeMug mug))
                    continue;
                if (Vector3.Angle(mug.transform.up, Vector3.up) > 30f)
                    continue;

                Vector3 offset = mug.transform.position - spout;
                offset.y = 0f;
                if (offset.sqrMagnitude <= _catchRadius * _catchRadius)
                    return mug;
            }

            return null;
        }

        private void SetStream(float length)
        {
            _stream.gameObject.SetActive(length > 0.001f);
            Vector3 scale = _stream.localScale;
            scale.y = length * 0.5f;
            _stream.localScale = scale;
            _stream.position = _spout.position - Vector3.up * (length * 0.5f);
        }

        private void SetLamp(Color color)
        {
            _tint.SetEmission(_statusLamp, color * 2.5f);
        }
    }
}
