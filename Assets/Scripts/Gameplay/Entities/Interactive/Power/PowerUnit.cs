using System;
using Gameplay.Effects;
using Gameplay.Entities.Interactive.Base.Items;
using UnityEngine;

namespace Gameplay.Entities.Interactive.Power
{
    public sealed class PowerUnit : MonoBehaviour
    {

        [SerializeField] private AudioSource _audio;
        [SerializeField] private AudioClip _powerUpClip;
        [SerializeField] private ItemSocket<Battery> _batterySocket = new();
        [SerializeField] private Renderer[] _chargeBars;
        [SerializeField] private Color _barOffColor = new(0.08f, 0.1f, 0.12f);
        [SerializeField] private Color _barOnColor = new(0.25f, 1f, 0.45f);
        [SerializeField, Min(0.05f)] private float _barFillInterval = 0.18f;
        [SerializeField] private SparkBurst _insertSparks;
        private int _litBars;
        private float _untilNextBar;
        private const float ReadyPulseDuration = 0.45f;
        private float _readyPulseRemaining;
        private readonly RendererTint _tint = new();

        public bool IsPowered { get; private set; }

        public event Action<bool> PoweredChanged;

        private void Awake()
        {
            for (int i = 0; i < _chargeBars.Length; i++)
                SetBar(i, false);
        }

        private void OnEnable()
        {
            _batterySocket.Initialize();
            _batterySocket.Inserted += HandleInserted;
            _batterySocket.Removed += HandleRemoved;
        }

        private void OnDisable()
        {
            _batterySocket.Inserted -= HandleInserted;
            _batterySocket.Removed -= HandleRemoved;
            _batterySocket.Dispose();
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

            int target = _batterySocket.Current != null ? _chargeBars.Length : 0;
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

        private void HandleInserted(Battery battery)
        {
            _untilNextBar = _barFillInterval;
            battery.ConfirmInsertion(true);
            _audio.PlayOneShot(_powerUpClip, 0.6f);
            _insertSparks.Play();
        }

        private void HandleRemoved(Battery _)
        {
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
            _tint.SetBaseAndEmission(bar, Color.Lerp(color, Color.white, pulse * 0.65f), on ? color * (2f + pulse * 2f) : Color.black);
        }
    }
}
