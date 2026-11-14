using Services.Audio;
using TMPro;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace Game.Entities.Props
{
    /// <summary>
    /// "DO NOT PRESS" button. Pressing it anyway sounds a short alarm, spins a red beacon and makes
    /// the sign get progressively more annoyed with every press.
    /// </summary>
    [RequireComponent(typeof(XRSimpleInteractable))]
    public sealed class PanicButton : MonoBehaviour
    {
        private static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");

        [SerializeField] private Transform _cap;
        [SerializeField, Min(0f)] private float _pressDepth = 0.012f;
        [SerializeField] private TMP_Text _sign;
        [SerializeField] private Transform _beacon;
        [SerializeField] private Light _beaconLight;
        [SerializeField] private Renderer _beaconLamp;
        [Tooltip("Room-filling red light that blinks on and off during the alarm.")]
        [SerializeField] private Light _roomFlash;
        [SerializeField, Min(0f)] private float _roomFlashIntensity = 2.5f;
        [SerializeField, Min(0.5f)] private float _blinkRate = 3f;
        [SerializeField] private Color _beaconColor = new(1f, 0.08f, 0.04f);
        [SerializeField, Min(0.5f)] private float _alarmDuration = 3.5f;
        [SerializeField] private float _spinSpeed = 540f;
        [SerializeField] private string[] _replies =
        {
            "DO NOT PRESS",
            "I SAID DO NOT PRESS",
            "SERIOUSLY?",
            "STOP IT",
            "OK, YOU WIN",
            "...",
        };

        private XRSimpleInteractable _interactable;
        private AudioSource _audio;
        private AudioClip _alarm;
        private MaterialPropertyBlock _block;
        private Vector3 _capRest;
        private float _alarmLeft;
        private float _pressAnim;
        private int _presses;
        private int _touching;
        private float _nextTouch;

        private void Awake()
        {
            _interactable = GetComponent<XRSimpleInteractable>();
            _block = new MaterialPropertyBlock();
            if (_cap != null)
                _capRest = _cap.localPosition;

            _alarm = ProceduralTones.CreateTwoToneAlarm("Panic alarm");
            _audio = gameObject.AddComponent<AudioSource>();
            _audio.playOnAwake = false;
            _audio.loop = true;
            _audio.clip = _alarm;
            _audio.spatialBlend = 0.8f;
            _audio.minDistance = 1f;
            _audio.maxDistance = 12f;
            _audio.volume = 0.55f;

            if (_sign != null)
                _sign.text = _replies[0];
            if (_roomFlash != null)
                _roomFlash.enabled = false;
            SetBeacon(0f);
        }

        private void OnEnable() => _interactable.selectEntered.AddListener(HandlePressed);

        // Pressing with a finger: any part of a physical hand entering the touch zone above the cap.
        private void OnTriggerEnter(Collider other)
        {
            var body = other.attachedRigidbody;
            if (body == null || !body.name.EndsWith("Physics Hand", System.StringComparison.Ordinal))
                return;

            if (_touching++ == 0 && Time.time >= _nextTouch)
            {
                _nextTouch = Time.time + 0.4f;
                HandlePressed(null);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            var body = other.attachedRigidbody;
            if (body != null && body.name.EndsWith("Physics Hand", System.StringComparison.Ordinal))
                _touching = Mathf.Max(0, _touching - 1);
        }

        private void OnDisable() => _interactable.selectEntered.RemoveListener(HandlePressed);

        private void OnDestroy()
        {
            if (_alarm != null)
                Destroy(_alarm);
        }

        private void Update()
        {
            if (_cap != null && _pressAnim > 0f)
            {
                _pressAnim = Mathf.MoveTowards(_pressAnim, 0f, Time.deltaTime * 4f);
                _cap.localPosition = _capRest - Vector3.up * (_pressDepth * Mathf.Clamp01(_pressAnim * 2f));
            }

            if (_alarmLeft <= 0f)
                return;

            _alarmLeft -= Time.deltaTime;
            if (_beacon != null)
                _beacon.Rotate(0f, _spinSpeed * Time.deltaTime, 0f, Space.Self);

            bool blinkOn = Mathf.Repeat(Time.time * _blinkRate, 1f) < 0.5f;
            SetBeacon(blinkOn ? 1f : 0.15f);
            if (_roomFlash != null)
            {
                _roomFlash.enabled = blinkOn;
                _roomFlash.intensity = _roomFlashIntensity;
                _roomFlash.color = _beaconColor;
            }

            if (_alarmLeft > 0f)
                return;

            if (_roomFlash != null)
                _roomFlash.enabled = false;

            _audio.Stop();
            SetBeacon(0f);
        }

        private void HandlePressed(SelectEnterEventArgs _)
        {
            _pressAnim = 1f;
            _presses++;
            if (_sign != null)
                _sign.text = _replies[Mathf.Min(_presses, _replies.Length - 1)];

            _alarmLeft = _alarmDuration;
            if (!_audio.isPlaying)
                _audio.Play();
        }

        private void SetBeacon(float strength)
        {
            if (_beaconLight != null)
            {
                _beaconLight.enabled = strength > 0f;
                _beaconLight.intensity = 3f * strength;
                _beaconLight.color = _beaconColor;
            }

            if (_beaconLamp == null)
                return;

            _beaconLamp.GetPropertyBlock(_block);
            _block.SetColor(EmissionId, _beaconColor * (0.15f + 4f * strength));
            _beaconLamp.SetPropertyBlock(_block);
        }
    }
}
