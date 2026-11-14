using System;
using Gameplay.Effects;
using Services.Audio;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace Gameplay.Entities.Interactive.Power
{
    public sealed class PowerUnit : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");

        [SerializeField] private AudioSource _audio;
        [SerializeField] private XRSocketInteractor _batterySocket;
        [SerializeField] private Transform _cableAnchor;
        [SerializeField] private Renderer[] _chargeBars;
        [SerializeField] private Color _barOffColor = new(0.08f, 0.1f, 0.12f);
        [SerializeField] private Color _barOnColor = new(0.25f, 1f, 0.45f);
        [SerializeField, Min(0.05f)] private float _barFillInterval = 0.18f;
        [SerializeField] private SparkBurst _insertSparks;
        private AudioClip _powerUpClip;
        private Battery _battery;
        private int _litBars;
        private float _untilNextBar;
        private const float ReadyPulseDuration = 0.45f;
        private float _readyPulseRemaining;
        private MaterialPropertyBlock _propertyBlock;

        public bool IsPowered { get; private set; }
        public Transform CableAnchor => _cableAnchor;

        public event Action<bool> PoweredChanged;

        private void Awake()
        {
            _powerUpClip = ProceduralTones.CreatePowerUp("Power up");
            for (int i = 0; i < _chargeBars.Length; i++)
                SetBar(i, false);
        }

        private void OnEnable()
        {
            _batterySocket.selectEntered.AddListener(HandleInserted);
            _batterySocket.selectExited.AddListener(HandleRemoved);
        }

        private void OnDisable()
        {
            _batterySocket.selectEntered.RemoveListener(HandleInserted);
            _batterySocket.selectExited.RemoveListener(HandleRemoved);
        }

        private void OnDestroy()
        {
            Destroy(_powerUpClip);
        }

        private void Update()
        {
            if (_readyPulseRemaining > 0f)
            {
                _readyPulseRemaining = Mathf.Max(0f, _readyPulseRemaining - Time.deltaTime);
                float pulse = Mathf.Sin(Mathf.PI * _readyPulseRemaining / ReadyPulseDuration);
                for (int i = 0; i < _litBars; i++)
                    SetBar(i, true, pulse);
            }

            int target = _battery != null ? _chargeBars.Length : 0;
            if (_litBars == target)
                return;

            _untilNextBar -= Time.deltaTime;
            if (_untilNextBar > 0f)
                return;

            _untilNextBar = _barFillInterval;
            if (_litBars < target)
                SetBar(_litBars++, true);
            else
                SetBar(--_litBars, false);

            bool powered = _litBars == _chargeBars.Length;
            if (powered != IsPowered)
                SetPowered(powered);
        }

        private void HandleInserted(SelectEnterEventArgs args)
        {
            if (!args.interactableObject.transform.TryGetComponent(out Battery battery))
                return;

            _battery = battery;
            _untilNextBar = _barFillInterval;
            battery.ConfirmInsertion(true);
            _audio.PlayOneShot(_powerUpClip, 0.6f);
            _insertSparks.Play();
        }

        private void HandleRemoved(SelectExitEventArgs args)
        {
            if (!args.interactableObject.transform.TryGetComponent(out Battery _))
                return;

            _battery = null;
            SetPowered(false);
        }

        private void SetPowered(bool powered)
        {
            IsPowered = powered;
            _readyPulseRemaining = powered ? ReadyPulseDuration : 0f;
            if (!powered)
            {
                for (int i = 0; i < _litBars; i++)
                    SetBar(i, true);
            }
            PoweredChanged?.Invoke(powered);
        }

        private void SetBar(int index, bool on, float pulse = 0f)
        {
            var bar = _chargeBars[index];
            Color color = on ? _barOnColor : _barOffColor;
            SetBaseAndEmission(bar, Color.Lerp(color, Color.white, pulse * 0.65f), on ? color * (2f + pulse * 2f) : Color.black);
        }

        private void SetBaseAndEmission(Renderer target, Color baseColor, Color emission)
        {
            _propertyBlock ??= new MaterialPropertyBlock();
            target.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetColor(BaseColorId, baseColor);
            _propertyBlock.SetColor(ColorId, baseColor);
            _propertyBlock.SetColor(EmissionId, emission);
            target.SetPropertyBlock(_propertyBlock);
        }
    }
}
