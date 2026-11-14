using System;
using Game.Entities.Player;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Attachment;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Comfort;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;

namespace Game.Comfort
{
    /// <summary>
    /// Comfort settings: movement mode, turning, vignette strength, recentering and the play-area guard.
    /// </summary>
    public sealed class ComfortMenu : MonoBehaviour
    {
        private const float DefaultVignette = 50f;
        private const float MenuDistance = 0.9f;
        private const float MenuDrop = 0.12f;

        [SerializeField] private TeleportationArea _floor;
        [SerializeField] private TunnelingVignetteController _vignettePrefab;
        [SerializeField] private ComfortMenuView _view;
        [Tooltip("Walkable floor in world XZ (x = X, y = Z). Leaving it returns the player to the start.")]
        [SerializeField] private Rect _playArea = Rect.MinMaxRect(-3.9f, -7.4f, 3.9f, 2.4f);
        [SerializeField] private float _minHeadHeight = -0.5f;

        [Header("Turning defaults")]
        [SerializeField] private TurnMode _defaultTurnMode = TurnMode.Snap;
        [SerializeField] private float _snapAngle = 45f;
        [SerializeField] private float _smoothTurnSpeed = 90f;
        [Tooltip("Pause between two snap turns while the stick is held. The XRI default of 0.5 s feels sluggish.")]
        [SerializeField, Min(0f)] private float _snapDelay = 0.2f;

        [Header("Participant & safety defaults")]
        [SerializeField] private bool _participantIdle;
        [Tooltip("Show proximity warnings while moving with the stick, not only when walking physically.")]
        [SerializeField] private bool _stickWarnings;
        [SerializeField] private bool _showBody = true;
        [Tooltip("Raises or lowers the view (centimetres) for players who feel too tall or too short.")]
        [SerializeField, Range(-60f, 40f)] private float _heightOffset;

        // Offset currently added on top of whatever height the XR Origin itself set (metres).
        private float _appliedHeight;

        private PlayerRig _rig;
        private PalmMenuGesture _palmGesture;
        private InputAction _toggleMenu;

        public LocomotionController Locomotion { get; private set; }
        public bool ParticipantIdle => _participantIdle;
        public bool StickWarnings => _stickWarnings;

        public event Action<bool> OnParticipantIdleChanged;
        public event Action<bool> OnStickWarningsChanged;
        public bool ShowBody => _showBody;
        public event Action<bool> OnShowBodyChanged;

        public bool Init(PlayerRig rig)
        {
            if (_floor == null || _vignettePrefab == null || _view == null)
            {
                Debug.LogError("Comfort menu requires a teleport floor, vignette prefab and menu view.", this);
                return false;
            }

            var move = rig.FindInRig<ContinuousMoveProvider>();
            var teleport = rig.FindInRig<TeleportationProvider>();
            if (move == null || teleport == null)
            {
                Debug.LogError("The XR Origin is missing a movement or teleportation provider.", this);
                return false;
            }

            _rig = rig;
            Locomotion = new LocomotionController(rig.Head, _floor, _vignettePrefab, move, teleport,
                rig.FindInRig<SnapTurnProvider>(), rig.FindInRig<ContinuousTurnProvider>(),
                rig.FindAllInRig<ControllerInputActionManager>(), rig.FindAllInRig<InteractionAttachController>());
            Locomotion.SetSnapDelay(_snapDelay);
            Locomotion.SetSnapAngle(_snapAngle);
            Locomotion.SetSmoothTurnSpeed(_smoothTurnSpeed);

            _view.Init(rig.Camera, DefaultVignette);
            _view.OnModeSelected += HandleModeSelected;
            _view.OnVignetteChanged += HandleVignetteChanged;
            _view.OnTurnModeSelected += HandleTurnModeSelected;
            _view.OnTurnAmountChanged += HandleTurnAmountChanged;
            _view.OnReturnClicked += ReturnToStart;
            _view.OnCloseClicked += Close;
            _view.OnParticipantIdleSelected += HandleParticipantIdleSelected;
            _view.OnStickWarningsSelected += HandleStickWarningsSelected;
            _view.ShowParticipantIdle(_participantIdle);
            _view.ShowStickWarnings(_stickWarnings);
            _view.OnShowBodySelected += HandleShowBodySelected;
            _view.ShowBody(_showBody);
            _view.OnHeightChanged += HandleHeightChanged;
            HandleHeightChanged(_heightOffset);

            _palmGesture = new PalmMenuGesture(rig);
            _toggleMenu = new InputAction(type: InputActionType.Button, binding: "<XRController>{LeftHand}/primaryButton");
            _toggleMenu.Enable();

            HandleModeSelected(MovementMode.Stick);
            HandleVignetteChanged(DefaultVignette);
            HandleTurnModeSelected(_defaultTurnMode);
            return true;
        }

        private void Update()
        {
            if (_rig == null)
                return;

            bool togglePressed = _toggleMenu.WasPressedThisFrame();
            bool palmRaised = _palmGesture.Tick(Time.unscaledDeltaTime);
            if (togglePressed || palmRaised)
                SetOpen(!_view.IsOpen);

            if (_rig.IsOutsidePlayArea(_playArea, _minHeadHeight))
                ReturnToStart();
        }

        private void OnDestroy()
        {
            _toggleMenu?.Dispose();
            if (_view == null)
                return;

            _view.OnModeSelected -= HandleModeSelected;
            _view.OnVignetteChanged -= HandleVignetteChanged;
            _view.OnTurnModeSelected -= HandleTurnModeSelected;
            _view.OnTurnAmountChanged -= HandleTurnAmountChanged;
            _view.OnReturnClicked -= ReturnToStart;
            _view.OnCloseClicked -= Close;
            _view.OnParticipantIdleSelected -= HandleParticipantIdleSelected;
            _view.OnStickWarningsSelected -= HandleStickWarningsSelected;
            _view.OnShowBodySelected -= HandleShowBodySelected;
            _view.OnHeightChanged -= HandleHeightChanged;
        }

        // Shifts the tracking space relative to its current height (the XR Origin may set its own camera offset
        // after start-up), replacing only the part this slider added before.
        private void HandleHeightChanged(float centimetres)
        {
            _heightOffset = centimetres;
            _view.ShowHeight(centimetres);
            float wanted = centimetres * 0.01f;
            Transform space = _rig.TrackingSpace;
            Vector3 position = space.localPosition;
            position.y += wanted - _appliedHeight;
            space.localPosition = position;
            _appliedHeight = wanted;
        }

        private void HandleShowBodySelected(bool visible)
        {
            _showBody = visible;
            _view.ShowBody(visible);
            OnShowBodyChanged?.Invoke(visible);
        }

        private void HandleParticipantIdleSelected(bool idle)
        {
            _participantIdle = idle;
            _view.ShowParticipantIdle(idle);
            OnParticipantIdleChanged?.Invoke(idle);
        }

        private void HandleStickWarningsSelected(bool enabled)
        {
            _stickWarnings = enabled;
            _view.ShowStickWarnings(enabled);
            OnStickWarningsChanged?.Invoke(enabled);
        }

        private void SetOpen(bool open)
        {
            if (open)
            {
                Vector3 forward = _rig.FlatForward;
                Vector3 position = _rig.Head.position + forward * MenuDistance - Vector3.up * MenuDrop;
                _view.Open(position, Quaternion.LookRotation(forward, Vector3.up));
            }
            else
            {
                _view.Close();
            }

            Locomotion.SetSuspended(open);
        }

        private void Close() => SetOpen(false);

        private void ReturnToStart()
        {
            _rig.ReturnToStart();
            SetOpen(false);
        }

        private void HandleModeSelected(MovementMode mode)
        {
            _view.ShowMode(mode);
            Locomotion.SetMode(mode);
        }

        private void HandleTurnModeSelected(TurnMode mode)
        {
            Locomotion.SetTurnMode(mode);
            _view.ShowTurn(mode, mode == TurnMode.Snap ? _snapAngle : _smoothTurnSpeed);
        }

        private void HandleTurnAmountChanged(float amount)
        {
            if (Locomotion.Turn == TurnMode.Snap)
            {
                _snapAngle = amount;
                Locomotion.SetSnapAngle(amount);
            }
            else
            {
                _smoothTurnSpeed = amount;
                Locomotion.SetSmoothTurnSpeed(amount);
            }
        }

        private void HandleVignetteChanged(float percent)
        {
            _view.ShowVignetteStrength(percent);
            Locomotion.SetVignetteStrength(percent);
        }
    }
}
