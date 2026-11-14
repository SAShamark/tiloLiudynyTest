using Gameplay.Entities.Player.Hands;
using Services.Audio;
using TMPro;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace Gameplay.Entities.Interactive.Props
{
    [RequireComponent(typeof(XRSimpleInteractable))]
    public sealed class PanicButton : MonoBehaviour
    {
        private static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");

        [SerializeField] private AudioSource _audio;
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
        private AudioClip _alarm;
        private Vector3 _capRest;
        private float _alarmLeft;
        private float _pressAnim;
        private int _presses;
        private int _touching;
        private float _nextTouch;
        private MaterialPropertyBlock _propertyBlock;

        private void Awake()
        {
            _interactable = GetComponent<XRSimpleInteractable>();
            _capRest = _cap.localPosition;

            _alarm = ProceduralTones.CreateTwoToneAlarm("Panic alarm");
            _audio.clip = _alarm;

            _sign.text = _replies[0];
            _roomFlash.enabled = false;
            SetBeacon(0f);
        }

        private void OnEnable() => _interactable.selectEntered.AddListener(HandlePressed);

        private void OnTriggerEnter(Collider other)
        {
            if (!PhysicalHandBody.IsHand(other))
                return;

            if (_touching++ == 0 && Time.time >= _nextTouch)
            {
                _nextTouch = Time.time + 0.4f;
                HandlePressed(null);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (PhysicalHandBody.IsHand(other))
                _touching = Mathf.Max(0, _touching - 1);
        }

        private void OnDisable() => _interactable.selectEntered.RemoveListener(HandlePressed);

        private void OnDestroy()
        {
            Destroy(_alarm);
        }

        private void Update()
        {
            if (_pressAnim > 0f)
            {
                _pressAnim = Mathf.MoveTowards(_pressAnim, 0f, Time.deltaTime * 4f);
                _cap.localPosition = _capRest - Vector3.up * (_pressDepth * Mathf.Clamp01(_pressAnim * 2f));
            }

            if (_alarmLeft <= 0f)
                return;

            _alarmLeft -= Time.deltaTime;
            _beacon.Rotate(0f, _spinSpeed * Time.deltaTime, 0f, Space.Self);

            bool blinkOn = Mathf.Repeat(Time.time * _blinkRate, 1f) < 0.5f;
            SetBeacon(blinkOn ? 1f : 0.15f);
            _roomFlash.enabled = blinkOn;
            _roomFlash.intensity = _roomFlashIntensity;
            _roomFlash.color = _beaconColor;


            if (_alarmLeft > 0f)
                return;

            _roomFlash.enabled = false;

            _audio.Stop();
            SetBeacon(0f);
        }

        private void HandlePressed(SelectEnterEventArgs _)
        {
            _pressAnim = 1f;
            _presses++;
            _sign.text = _replies[Mathf.Min(_presses, _replies.Length - 1)];

            _alarmLeft = _alarmDuration;
            if (!_audio.isPlaying)
                _audio.Play();
        }

        private void SetBeacon(float strength)
        {
            _beaconLight.enabled = strength > 0f;
            _beaconLight.intensity = 3f * strength;
            _beaconLight.color = _beaconColor;
            SetEmission(_beaconLamp, _beaconColor * (0.15f + 4f * strength));
        }

        private void SetEmission(Renderer target, Color emission)
        {
            _propertyBlock ??= new MaterialPropertyBlock();
            target.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetColor(EmissionId, emission);
            target.SetPropertyBlock(_propertyBlock);
        }
    }
}
