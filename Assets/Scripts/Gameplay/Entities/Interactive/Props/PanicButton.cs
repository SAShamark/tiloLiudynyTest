using Gameplay.Effects;
using Gameplay.Entities.Interactive.Base.Buttons;
using TMPro;
using UnityEngine;

namespace Gameplay.Entities.Interactive.Props
{
    public sealed class PanicButton : PressButton
    {

        [SerializeField] private AudioSource _audio;
        [SerializeField] private TMP_Text _sign;
        [SerializeField] private Transform _beacon;
        [SerializeField] private Light _beaconLight;
        [SerializeField] private Renderer _beaconLamp;
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

        private float _alarmLeft;
        private int _presses;
        private readonly RendererTint _tint = new();

        protected override void Awake()
        {
            base.Awake();
            _sign.text = _replies[0];
            _roomFlash.enabled = false;
            SetBeacon(0f);
        }

        protected override void Update()
        {
            base.Update();
            if (_alarmLeft <= 0f)
            {
                return;
            }

            _alarmLeft -= Time.deltaTime;
            _beacon.Rotate(0f, _spinSpeed * Time.deltaTime, 0f, Space.Self);

            bool blinkOn = Mathf.Repeat(Time.time * _blinkRate, 1f) < 0.5f;
            SetBeacon(blinkOn ? 1f : 0.15f);
            _roomFlash.enabled = blinkOn;
            _roomFlash.intensity = _roomFlashIntensity;
            _roomFlash.color = _beaconColor;

            if (_alarmLeft > 0f)
            {
                return;
            }

            _roomFlash.enabled = false;
            _audio.Stop();
            SetBeacon(0f);
        }

        protected override void OnPressed()
        {
            _presses++;
            _sign.text = _replies[Mathf.Min(_presses, _replies.Length - 1)];

            _alarmLeft = _alarmDuration;
            if (!_audio.isPlaying)
            {
                _audio.Play();
            }
        }

        private void SetBeacon(float strength)
        {
            _beaconLight.enabled = strength > 0f;
            _beaconLight.intensity = 3f * strength;
            _beaconLight.color = _beaconColor;
            _tint.SetEmission(_beaconLamp, _beaconColor * (0.15f + 4f * strength));
        }
    }
}
