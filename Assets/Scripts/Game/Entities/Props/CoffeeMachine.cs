using Services.Audio;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace Game.Entities.Props
{
    /// <summary>
    /// Press the button to brew: a stream runs from the spout for a few seconds and fills whatever
    /// mug stands underneath. No mug? Coffee goes straight into the drip tray, with a sad beep.
    /// </summary>
    public sealed class CoffeeMachine : MonoBehaviour
    {
        private static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");

        [SerializeField] private XRSimpleInteractable _button;
        [SerializeField] private Transform _spout;
        [SerializeField] private Transform _stream;
        [SerializeField] private Renderer _statusLamp;
        [SerializeField] private Color _readyColor = new(0.2f, 1f, 0.4f);
        [SerializeField] private Color _brewingColor = new(1f, 0.55f, 0.1f);
        [SerializeField, Min(0.5f)] private float _brewDuration = 3.5f;
        [Tooltip("Horizontal distance from the spout within which a mug catches the stream.")]
        [SerializeField, Min(0.01f)] private float _catchRadius = 0.06f;
        [Tooltip("Length of the stream with nothing under it (spout to drip tray).")]
        [SerializeField, Min(0.05f)] private float _streamLength = 0.27f;

        private CoffeeMug[] _mugs;
        private AudioSource _audio;
        private AudioClip _pour;
        private AudioClip _ready;
        private AudioClip _sad;
        private MaterialPropertyBlock _block;
        private float _brewLeft;

        private void Awake()
        {
            _mugs = FindObjectsByType<CoffeeMug>(FindObjectsSortMode.None);
            _block = new MaterialPropertyBlock();
            _pour = ProceduralTones.CreatePour("Coffee pour");
            _ready = ProceduralTones.CreateBeep("Coffee ready", 1320f, 0.15f);
            _sad = ProceduralTones.CreateBeep("Coffee sad", 330f, 0.35f);

            _audio = gameObject.AddComponent<AudioSource>();
            _audio.playOnAwake = false;
            _audio.spatialBlend = 1f;
            _audio.minDistance = 0.4f;
            _audio.maxDistance = 6f;
            _audio.clip = _pour;
            _audio.loop = true;
            _audio.volume = 0.5f;

            SetStream(0f);
            SetLamp(_readyColor);
        }

        private void OnEnable() => _button.selectEntered.AddListener(HandlePressed);

        private void OnDisable() => _button.selectEntered.RemoveListener(HandlePressed);

        private void OnDestroy()
        {
            Destroy(_pour);
            Destroy(_ready);
            Destroy(_sad);
        }

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

        private void HandlePressed(SelectEnterEventArgs _)
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
            foreach (var mug in _mugs)
            {
                if (mug == null || Vector3.Angle(mug.transform.up, Vector3.up) > 30f)
                    continue;

                Vector3 offset = mug.transform.position - spout;
                if (offset.y > 0f || offset.y < -_streamLength - 0.03f)
                    continue;
                offset.y = 0f;
                if (offset.sqrMagnitude <= _catchRadius * _catchRadius)
                    return mug;
            }

            return null;
        }

        private void SetStream(float length)
        {
            if (_stream == null)
                return;

            _stream.gameObject.SetActive(length > 0.001f);
            Vector3 scale = _stream.localScale;
            scale.y = length * 0.5f;
            _stream.localScale = scale;
            _stream.position = _spout.position - Vector3.up * (length * 0.5f);
        }

        private void SetLamp(Color color)
        {
            if (_statusLamp == null)
                return;

            _statusLamp.GetPropertyBlock(_block);
            _block.SetColor(EmissionId, color * 2.5f);
            _statusLamp.SetPropertyBlock(_block);
        }
    }
}
