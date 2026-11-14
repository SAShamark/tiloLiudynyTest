using System;
using System.Collections.Generic;
using Game.Entities.Environment;
using Game.Entities.Organs;
using Game.Entities.Power;
using Game.Entities.Station.Parts;
using Game.Safety;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace Game.Entities.Station
{
    /// <summary>
    /// Station flow: power the generator with the battery and plug the cable in (Ready, the selected organ is projected),
    /// pull the lever (Active, scanner loops and the heart beats), pull again to stop. Losing power or the cable shuts it down.
    /// The organ comes from the flask in the flask socket or from the organ panel, whichever was used last.
    /// </summary>
    public sealed class HeartStationControl : MonoBehaviour
    {
        private static readonly Vector3 CableAnchorPosition = new(-0.55f, 0.23f, -0.08f);
        private const string BodyRendererPath = "Scanner body/Scanner upper bezel";

        [SerializeField] private XRSocketInteractor _socket;
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

        private Probe _probe;
        private PowerUnit _powerUnit;
        private ISafetyGate _safety;
        private AudioSource _socketAudio;
        private bool _probeConnected;

        public StationState State { get; private set; } = StationState.Unpowered;

        public OrganConfig SelectedOrgan { get; private set; }

        public event Action<StationState> OnStateChanged;
        public event Action<OrganConfig> OnOrganChanged;
        /// <summary>A flask was pushed out of the socket to make room for another one.</summary>
        public event Action<OrganFlask> OnFlaskEjected;

        private bool IsStopActive => _safety != null && _safety.IsStopActive;

        public void Init(Probe probe, PowerUnit powerUnit, ISafetyGate safety)
        {
            _probe = probe;
            _powerUnit = powerUnit;
            _safety = safety;
            _socketAudio = CreateSocketAudio();
            _pulse.Init(_animator, _scanner, _socketAudio);

            _socket.selectEntered.AddListener(HandleProbeInserted);
            _socket.selectExited.AddListener(HandleProbeRemoved);
            _flaskSocket.selectEntered.AddListener(HandleFlaskInserted);
            _flaskSocket.selectExited.AddListener(HandleFlaskRemoved);
            if (_powerUnit != null)
                _powerUnit.OnPoweredChanged += HandlePoweredChanged;

            if (_probe != null)
                BuildCableAndLever();
        }

        private void OnDestroy()
        {
            _pulse.Dispose();
            if (_powerUnit != null)
                _powerUnit.OnPoweredChanged -= HandlePoweredChanged;
            if (_socket == null)
                return;

            _socket.selectEntered.RemoveListener(HandleProbeInserted);
            _socket.selectExited.RemoveListener(HandleProbeRemoved);
            if (_flaskSocket == null)
                return;

            _flaskSocket.selectEntered.RemoveListener(HandleFlaskInserted);
            _flaskSocket.selectExited.RemoveListener(HandleFlaskRemoved);
        }

        private void LateUpdate()
        {
            if (_socketAudio == null)
                return;

            bool stop = IsStopActive;
            _pulse.Tick(Time.deltaTime, stop);

            bool showGuide = !stop && !_socket.hasSelection && _probe != null && _probe.IsHeld;
            SetActiveIfChanged(_probeGuide, showGuide);
            SetActiveIfChanged(_socketHighlight, showGuide && _socket.hasHover);

            if (stop && _socketAudio.isPlaying)
                _socketAudio.Stop();

            TrySwapFlask();
        }

        private IReadOnlyList<OrganFlask> _flasks = Array.Empty<OrganFlask>();

        /// <summary>Flasks that may be swapped into the socket.</summary>
        public void SetSwappableFlasks(IReadOnlyList<OrganFlask> flasks) => _flasks = flasks;

        // Swapping in one motion: bring the new flask to the station and the old one is ejected for you.
        private void TrySwapFlask()
        {
            if (!_flaskSocket.hasSelection)
                return;

            var current = _flaskSocket.firstInteractableSelected.transform.GetComponent<OrganFlask>();
            if (current == null)
                return;

            Vector3 socketPosition = _flaskSocket.attachTransform.position;
            foreach (var flask in _flasks)
            {
                if (flask == current || !flask.IsHeld)
                    continue;
                if ((flask.transform.position - socketPosition).sqrMagnitude > _swapDistance * _swapDistance)
                    continue;

                _flaskSocket.interactionManager.SelectExit((IXRSelectInteractor)_flaskSocket, (IXRSelectInteractable)current.Interactable);
                OnFlaskEjected?.Invoke(current);
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
            OnStateChanged?.Invoke(state);
        }

        public void SelectOrgan(OrganConfig organ)
        {
            if (SelectedOrgan == organ)
                return;

            SelectedOrgan = organ;
            _projector.SetOrgan(organ);
            if (organ == null && State == StationState.Active)
                SetState(StationState.Ready);
            else
                RefreshPulse();

            OnOrganChanged?.Invoke(organ);
        }

        private void RefreshPulse()
        {
            bool running = State == StationState.Active && SelectedOrgan != null;
            if (_activeEffect != null && running != _activeEffect.IsPlaying)
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

        private bool IsPowered => _powerUnit == null || _powerUnit.IsPowered;

        private void RefreshPower()
        {
            bool ready = IsPowered && _probeConnected;
            if (!ready)
                SetState(StationState.Unpowered);
            else if (State == StationState.Unpowered)
                SetState(StationState.Ready);
        }

        private void HandlePoweredChanged(bool _) => RefreshPower();

        private bool CanPullLever() => State != StationState.Unpowered && SelectedOrgan != null && !IsStopActive;

        private void HandleLeverPulled()
        {
            SetState(State == StationState.Active ? StationState.Ready : StationState.Active);
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
                _socketAudio.PlayOneShot(_insertionClick, 0.45f);
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

        private AudioSource CreateSocketAudio()
        {
            var source = _socket.gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 1f;
            source.rolloffMode = AudioRolloffMode.Linear;
            source.minDistance = 0.5f;
            source.maxDistance = 3f;
            return source;
        }

        private void BuildCableAndLever()
        {
            var body = transform.Find(BodyRendererPath)?.GetComponent<Renderer>();
            Material bodyMaterial = body != null ? body.sharedMaterial : _probe.GetComponent<Renderer>().sharedMaterial;

            Transform anchor = _powerUnit != null && _powerUnit.CableAnchor != null
                ? _powerUnit.CableAnchor
                : CreateLocalAnchor();
            anchor.gameObject.AddComponent<StationCable>().Init(_probe.transform, bodyMaterial);

            _lever.Init(CanPullLever, () => State == StationState.Active, HandleLeverPulled);
        }

        private Transform CreateLocalAnchor()
        {
            var anchor = new GameObject("Power cable anchor").transform;
            anchor.SetParent(transform, false);
            anchor.localPosition = CableAnchorPosition;
            return anchor;
        }

        private static void SetActiveIfChanged(GameObject target, bool active)
        {
            if (target.activeSelf != active)
                target.SetActive(active);
        }
    }
}
