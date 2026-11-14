using System;
using Services.Audio;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace Game.Entities.Power
{
    /// <summary>
    /// Generator that powers the station once a battery is inserted.
    /// Charge bars fill up one by one; the station cable starts here.
    /// </summary>
    public sealed class PowerUnit : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");

        [SerializeField] private XRSocketInteractor _batterySocket;
        [SerializeField] private Transform _cableAnchor;
        [SerializeField] private Renderer[] _chargeBars;
        [SerializeField] private Color _barOffColor = new(0.08f, 0.1f, 0.12f);
        [SerializeField] private Color _barOnColor = new(0.25f, 1f, 0.45f);
        [SerializeField, Min(0.05f)] private float _barFillInterval = 0.18f;

        private MaterialPropertyBlock _block;
        private AudioSource _audio;
        private AudioClip _powerUpClip;
        private Battery _battery;
        private int _litBars;
        private float _untilNextBar;

        public bool IsPowered { get; private set; }
        public Transform CableAnchor => _cableAnchor;

        public event Action<bool> OnPoweredChanged;

        private void Awake()
        {
            _block = new MaterialPropertyBlock();
            _audio = gameObject.AddComponent<AudioSource>();
            _audio.playOnAwake = false;
            _audio.spatialBlend = 1f;
            _audio.rolloffMode = AudioRolloffMode.Linear;
            _audio.maxDistance = 4f;
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
            if (_powerUpClip != null)
                Destroy(_powerUpClip);
        }

        private void Update()
        {
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
            OnPoweredChanged?.Invoke(powered);
        }

        private void SetBar(int index, bool on)
        {
            var bar = _chargeBars[index];
            if (bar == null)
                return;

            Color color = on ? _barOnColor : _barOffColor;
            bar.GetPropertyBlock(_block);
            _block.SetColor(BaseColorId, color);
            _block.SetColor(EmissionId, on ? color * 2f : Color.black);
            bar.SetPropertyBlock(_block);
        }
    }
}
