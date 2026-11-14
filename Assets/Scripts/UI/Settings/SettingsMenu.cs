using System;
using Gameplay.Entities.Player.Movement;
using Gameplay.Entities.Player;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Attachment;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Comfort;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;
using UnityEngine;

namespace UI.Settings
{
    public sealed class SettingsMenu : MonoBehaviour
    {
        private const float DefaultVignette = 50f;
        private const float MenuDistance = 0.9f;
        private const float MenuDrop = 0.12f;

        [SerializeField] private TeleportationArea _floor;
        [SerializeField] private TunnelingVignetteController _vignettePrefab;
        [SerializeField] private SettingsMenuView _view;
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

        private float _appliedHeight;

        private PlayerRig _rig;
        private PalmMenuGesture _palmGesture;
        private InputAction _toggleMenu;

        public LocomotionLogic Locomotion { get; private set; }
        public bool ParticipantIdle => _participantIdle;
        public bool StickWarnings => _stickWarnings;
        public bool ShowBody => _showBody;

        public event Action<bool> ParticipantIdleChanged;
        public event Action<bool> StickWarningsChanged;
        public event Action<bool> ShowBodyChanged;

        public void Initialize(PlayerRig rig)
        {
            _rig = rig;
            Locomotion = new LocomotionLogic(rig.Head, _floor, _vignettePrefab,
                rig.FindInRig<ContinuousMoveProvider>(), rig.FindInRig<TeleportationProvider>(),
                rig.FindInRig<SnapTurnProvider>(), rig.FindInRig<ContinuousTurnProvider>(),
                rig.FindAllInRig<ControllerInputActionManager>(), rig.FindAllInRig<InteractionAttachController>());
            Locomotion.SetSnapDelay(_snapDelay);
            Locomotion.SetSnapAngle(_snapAngle);
            Locomotion.SetSmoothTurnSpeed(_smoothTurnSpeed);

            _view.Initialize(rig.Camera, DefaultVignette);
            _view.ModeSelected += Locomotion.SetMode;
            _view.VignetteChanged += Locomotion.SetVignetteStrength;
            _view.TurnModeSelected += HandleTurnModeSelected;
            _view.TurnAmountChanged += HandleTurnAmountChanged;
            _view.ParticipantIdleSelected += HandleParticipantIdleSelected;
            _view.StickWarningsSelected += HandleStickWarningsSelected;
            _view.ShowBodySelected += HandleShowBodySelected;
            _view.HeightChanged += HandleHeightChanged;
            _view.ReturnClicked += ReturnToStart;
            _view.CloseClicked += Close;

            _view.ShowParticipantIdle(_participantIdle);
            _view.ShowStickWarnings(_stickWarnings);
            _view.ShowBody(_showBody);
            _view.ShowHeight(_heightOffset);
            HandleHeightChanged(_heightOffset);

            _palmGesture = new PalmMenuGesture(rig);
            _toggleMenu = new InputAction(type: InputActionType.Button, binding: "<XRController>{LeftHand}/primaryButton");
            _toggleMenu.Enable();

            _view.ShowMode(MovementMode.Stick);
            Locomotion.SetMode(MovementMode.Stick);
            Locomotion.SetVignetteStrength(DefaultVignette);
            HandleTurnModeSelected(_defaultTurnMode);
        }

        private void Update()
        {
            bool togglePressed = _toggleMenu.WasPressedThisFrame();
            bool palmRaised = _palmGesture.Tick(Time.unscaledDeltaTime);
            if (togglePressed || palmRaised)
                SetOpen(!_view.IsOpen);

            if (_rig.IsOutsidePlayArea(_playArea, _minHeadHeight))
                ReturnToStart();
        }

        private void OnDestroy()
        {
            _toggleMenu.Dispose();
            _view.ModeSelected -= Locomotion.SetMode;
            _view.VignetteChanged -= Locomotion.SetVignetteStrength;
            _view.TurnModeSelected -= HandleTurnModeSelected;
            _view.TurnAmountChanged -= HandleTurnAmountChanged;
            _view.ParticipantIdleSelected -= HandleParticipantIdleSelected;
            _view.StickWarningsSelected -= HandleStickWarningsSelected;
            _view.ShowBodySelected -= HandleShowBodySelected;
            _view.HeightChanged -= HandleHeightChanged;
            _view.ReturnClicked -= ReturnToStart;
            _view.CloseClicked -= Close;
        }

        private void HandleHeightChanged(float centimetres)
        {
            _heightOffset = centimetres;
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
            ShowBodyChanged?.Invoke(visible);
        }

        private void HandleParticipantIdleSelected(bool idle)
        {
            _participantIdle = idle;
            ParticipantIdleChanged?.Invoke(idle);
        }

        private void HandleStickWarningsSelected(bool enabled)
        {
            _stickWarnings = enabled;
            StickWarningsChanged?.Invoke(enabled);
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
    }
}
