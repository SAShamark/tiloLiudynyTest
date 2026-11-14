using System;
using System.Collections.Generic;
using Gameplay.Effects;
using Gameplay.Entities.Interactive.BaseLogic.Items;
using Gameplay.Entities.Interactive.Other.Organs;
using Gameplay.Entities.Interactive.Other.Station.Parts;
using Gameplay.Entities.Interactive.Other.Power;
using Gameplay.Entities.Participant;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace Gameplay.Entities.Interactive.Other.Station
{
    public sealed class HeartStationControl : MonoBehaviour
    {
        [SerializeField] private ItemSocket<Probe> _probeSocket = new();
        [SerializeField] private AudioSource _socketAudio;
        [SerializeField] private Animator _animator;
        [SerializeField] private GameObject _socketHighlight;
        [SerializeField] private GameObject _probeGuide;
        [SerializeField] private AudioClip _insertionClick;
        [SerializeField] private HeartScanner _scanner;
        [SerializeField] private StationLever _lever;
        [SerializeField] private ItemSocket<OrganFlask> _flaskSocket = new();
        [Tooltip("Bringing another flask this close to an occupied flask socket ejects the current one.")]
        [SerializeField, Min(0.02f)] private float _swapDistance = 0.18f;
        [SerializeField] private OrganProjector _projector;
        [SerializeField] private HeartPulseLoop _pulse = new();
        [Tooltip("Looping effect under the projection while the station runs.")]
        [SerializeField] private EffectPlayer _activeEffect;
        [Tooltip("Sparks when the cable plug goes into the station.")]
        [SerializeField] private SparkBurst _plugSparks;

        private Probe _probe;
        private PowerUnit _powerUnit;
        private ParticipantControl _participant;
        private bool _leverOn;
        private IReadOnlyList<OrganFlask> _flasks = Array.Empty<OrganFlask>();

        public StationState State { get; private set; } = StationState.Unpowered;

        public OrganConfig SelectedOrgan { get; private set; }

        public bool IsOrganVisible => State == StationState.Active && SelectedOrgan != null;

        public Transform ProjectionPoint => _projector.transform;

        public Transform ModelRoot => _projector.ModelRoot;

        public event Action<StationState> StateChanged;
        public event Action Shocked;
        public event Action<OrganConfig> OrganChanged;
        public event Action<OrganFlask> FlaskEjected;

        private bool IsStopActive => _participant.IsTooClose;

        public void Initialize(Probe probe, PowerUnit powerUnit, ParticipantControl participant)
        {
            _probe = probe;
            _powerUnit = powerUnit;
            _participant = participant;
            _pulse.Initialize(_animator, _scanner, _socketAudio);

            _probeSocket.Initialize();
            _probeSocket.Inserted += HandleProbeInserted;
            _probeSocket.Removed += HandleProbeRemoved;
            _flaskSocket.Initialize();
            _flaskSocket.Inserted += HandleFlaskInserted;
            _flaskSocket.Removed += HandleFlaskRemoved;
            _powerUnit.PoweredChanged += HandlePoweredChanged;

            _lever.Toggled += HandleLeverToggled;
            RefreshLever();
        }

        private void OnDestroy()
        {
            _powerUnit.PoweredChanged -= HandlePoweredChanged;
            _lever.Toggled -= HandleLeverToggled;
            _probeSocket.Inserted -= HandleProbeInserted;
            _probeSocket.Removed -= HandleProbeRemoved;
            _probeSocket.Dispose();
            _flaskSocket.Inserted -= HandleFlaskInserted;
            _flaskSocket.Removed -= HandleFlaskRemoved;
            _flaskSocket.Dispose();
        }

        private void LateUpdate()
        {
            bool stop = IsStopActive;
            _lever.SetLocked(stop);
            _pulse.Tick(Time.deltaTime, stop);

            bool showGuide = !stop && !_probeSocket.Interactor.hasSelection && _probe.IsHeld;
            SetActiveIfChanged(_probeGuide, showGuide);
            SetActiveIfChanged(_socketHighlight, showGuide && _probeSocket.Interactor.hasHover);

            if (stop && _socketAudio.isPlaying)
                _socketAudio.Stop();

            TrySwapFlask();
        }

        public void SetSwappableFlasks(IReadOnlyList<OrganFlask> flasks) => _flasks = flasks;

        private void TrySwapFlask()
        {
            var current = _flaskSocket.Current;
            if (current == null)
                return;

            var socket = _flaskSocket.Interactor;
            Vector3 socketPosition = socket.attachTransform.position;
            foreach (var flask in _flasks)
            {
                if (flask == current || !flask.IsHeld)
                    continue;
                if ((flask.transform.position - socketPosition).sqrMagnitude > _swapDistance * _swapDistance)
                    continue;

                socket.interactionManager.SelectExit((IXRSelectInteractor)socket, (IXRSelectInteractable)current.Interactable);
                FlaskEjected?.Invoke(current);
                return;
            }
        }

        private void SetState(StationState state)
        {
            if (State == state)
                return;

            State = state;
            _projector.SetPowered(state != StationState.Unpowered);
            RefreshPulse();
            RefreshLever();
            StateChanged?.Invoke(state);
        }

        public void SelectOrgan(OrganConfig organ)
        {
            if (SelectedOrgan == organ)
                return;

            SelectedOrgan = organ;
            _projector.SetOrgan(organ);
            RefreshActivation();
            RefreshPulse();
            RefreshLever();

            OrganChanged?.Invoke(organ);
        }

        public void ReceiveShock()
        {
            _scanner.Play();
            Shocked?.Invoke();
        }

        private void RefreshPulse()
        {
            bool running = State == StationState.Active && SelectedOrgan != null;
            if (running != _activeEffect.IsPlaying)
            {
                if (running)
                    _activeEffect.Play();
                else
                    _activeEffect.Stop();
            }

            _projector.SetMaterialized(State == StationState.Active && SelectedOrgan != null);
            if (State == StationState.Active)
                _pulse.Start(SelectedOrgan != null && SelectedOrgan.Beats);
            else if (_pulse.IsRunning)
                _pulse.Stop();
        }

        private bool IsPowered => _powerUnit.IsPowered;

        private void RefreshPower()
        {
            bool ready = IsPowered && _probeSocket.Current != null;
            if (!ready)
                SetState(StationState.Unpowered);
            else if (State == StationState.Unpowered)
                SetState(StationState.Ready);

            RefreshActivation();
        }

        private void RefreshActivation()
        {
            if (State == StationState.Unpowered)
                return;

            SetState(_leverOn && SelectedOrgan != null ? StationState.Active : StationState.Ready);
        }

        private void HandlePoweredChanged(bool _) => RefreshPower();

        private void RefreshLever() =>
            _lever.ShowIndicators(State != StationState.Unpowered && SelectedOrgan != null, State == StationState.Active);

        private void HandleLeverToggled(bool on)
        {
            _leverOn = on;
            RefreshActivation();
            _socketAudio.PlayOneShot(_insertionClick, 0.55f);
        }

        private void HandleProbeInserted(Probe probe)
        {
            _socketHighlight.SetActive(false);
            _probeGuide.SetActive(false);
            RefreshPower();

            bool playFeedback = !IsStopActive;
            probe.ConfirmInsertion(playFeedback);
            if (playFeedback)
            {
                _socketAudio.PlayOneShot(_insertionClick, 0.45f);
                _plugSparks.Play();
            }
        }

        private void HandleProbeRemoved(Probe _)
        {
            RefreshPower();
        }

        private void HandleFlaskInserted(OrganFlask flask)
        {
            flask.ConfirmInsertion(!IsStopActive);
            _socketAudio.PlayOneShot(_insertionClick, 0.45f);
            SelectOrgan(flask.Organ);
        }

        private void HandleFlaskRemoved(OrganFlask flask)
        {
            if (SelectedOrgan == flask.Organ)
                SelectOrgan(null);
        }




        private static void SetActiveIfChanged(GameObject target, bool active)
        {
            if (target.activeSelf != active)
                target.SetActive(active);
        }
    }
}
