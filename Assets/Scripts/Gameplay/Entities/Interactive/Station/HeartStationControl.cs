using System;
using System.Collections.Generic;
using Gameplay.Effects;
using Gameplay.Entities.Interactive.Organs;
using Gameplay.Entities.Interactive.Power;
using Gameplay.Entities.Participant;
using Gameplay.Entities.Interactive.Station.Parts;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace Gameplay.Entities.Interactive.Station
{
    public sealed class HeartStationControl : MonoBehaviour
    {
        private const string BodyRendererPath = "Scanner body/Scanner upper bezel";

        [SerializeField] private XRSocketInteractor _socket;
        [SerializeField] private AudioSource _socketAudio;
        [SerializeField] private Animator _animator;
        [SerializeField] private GameObject _socketHighlight;
        [SerializeField] private GameObject _probeGuide;
        [SerializeField] private AudioClip _insertionClick;
        [SerializeField] private HeartScanner _scanner;
        [SerializeField] private StationLever _lever;
        [SerializeField] private XRSocketInteractor _flaskSocket;
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
        private bool _probeConnected;
        private bool _leverOn;
        private IReadOnlyList<OrganFlask> _flasks = Array.Empty<OrganFlask>();

        public StationState State { get; private set; } = StationState.Unpowered;

        public OrganConfig SelectedOrgan { get; private set; }

        public bool IsOrganVisible => State == StationState.Active && SelectedOrgan != null;

        public Transform ProjectionPoint => _projector.transform;

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

            _socket.selectEntered.AddListener(HandleProbeInserted);
            _socket.selectExited.AddListener(HandleProbeRemoved);
            _flaskSocket.selectEntered.AddListener(HandleFlaskInserted);
            _flaskSocket.selectExited.AddListener(HandleFlaskRemoved);
            _powerUnit.PoweredChanged += HandlePoweredChanged;

            BuildCableAndLever();
        }

        private void OnDestroy()
        {
            _pulse.Dispose();
            _powerUnit.PoweredChanged -= HandlePoweredChanged;
            _socket.selectEntered.RemoveListener(HandleProbeInserted);
            _socket.selectExited.RemoveListener(HandleProbeRemoved);
            _flaskSocket.selectEntered.RemoveListener(HandleFlaskInserted);
            _flaskSocket.selectExited.RemoveListener(HandleFlaskRemoved);
        }

        private void LateUpdate()
        {
            bool stop = IsStopActive;
            _pulse.Tick(Time.deltaTime, stop);

            bool showGuide = !stop && !_socket.hasSelection && _probe.IsHeld;
            SetActiveIfChanged(_probeGuide, showGuide);
            SetActiveIfChanged(_socketHighlight, showGuide && _socket.hasHover);

            if (stop && _socketAudio.isPlaying)
                _socketAudio.Stop();

            TrySwapFlask();
        }

        public void SetSwappableFlasks(IReadOnlyList<OrganFlask> flasks) => _flasks = flasks;

        private void TrySwapFlask()
        {
            if (!_flaskSocket.hasSelection)
                return;

            var current = _flaskSocket.firstInteractableSelected.transform.GetComponent<OrganFlask>();
            Vector3 socketPosition = _flaskSocket.attachTransform.position;
            foreach (var flask in _flasks)
            {
                if (flask == current || !flask.IsHeld)
                    continue;
                if ((flask.transform.position - socketPosition).sqrMagnitude > _swapDistance * _swapDistance)
                    continue;

                _flaskSocket.interactionManager.SelectExit((IXRSelectInteractor)_flaskSocket, (IXRSelectInteractable)current.Interactable);
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
            bool ready = IsPowered && _probeConnected;
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

        private bool CanPullLever() => !IsStopActive;

        private bool ConditionsMet() => State != StationState.Unpowered && SelectedOrgan != null;

        private void HandleLeverPulled()
        {
            _leverOn = !_leverOn;
            RefreshActivation();
            _socketAudio.PlayOneShot(_insertionClick, 0.55f);
        }

        private void HandleProbeInserted(SelectEnterEventArgs args)
        {
            if (!args.interactableObject.transform.TryGetComponent(out Probe probe))
                return;

            _socketHighlight.SetActive(false);
            _probeGuide.SetActive(false);
            _probeConnected = true;
            RefreshPower();

            bool playFeedback = !IsStopActive;
            probe.ConfirmInsertion(playFeedback);
            if (playFeedback)
            {
                _socketAudio.PlayOneShot(_insertionClick, 0.45f);
                _plugSparks.Play();
            }
        }

        private void HandleProbeRemoved(SelectExitEventArgs args)
        {
            if (!args.interactableObject.transform.TryGetComponent(out Probe _))
                return;

            _probeConnected = false;
            RefreshPower();
        }

        private void HandleFlaskInserted(SelectEnterEventArgs args)
        {
            if (!args.interactableObject.transform.TryGetComponent(out OrganFlask flask))
                return;

            flask.ConfirmInsertion(!IsStopActive);
            _socketAudio.PlayOneShot(_insertionClick, 0.45f);
            SelectOrgan(flask.Organ);
        }

        private void HandleFlaskRemoved(SelectExitEventArgs args)
        {
            if (args.interactableObject.transform.TryGetComponent(out OrganFlask flask) && SelectedOrgan == flask.Organ)
                SelectOrgan(null);
        }


        private void BuildCableAndLever()
        {
            Material bodyMaterial = transform.Find(BodyRendererPath).GetComponent<Renderer>().sharedMaterial;
            _powerUnit.CableAnchor.gameObject.AddComponent<StationCable>().Initialize(_probe.transform, bodyMaterial);

            _lever.Initialize(CanPullLever, () => _leverOn, HandleLeverPulled);
            _lever.SetIndicators(ConditionsMet, () => State == StationState.Active);
        }


        private static void SetActiveIfChanged(GameObject target, bool active)
        {
            if (target.activeSelf != active)
                target.SetActive(active);
        }
    }
}
