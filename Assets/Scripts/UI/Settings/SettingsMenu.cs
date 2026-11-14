using Gameplay.Entities.Participant;
using Gameplay.Entities.Player.Movement;
using Gameplay.Entities.Player;
using UnityEngine.InputSystem;
using UnityEngine;

namespace UI.Settings
{
    public sealed class SettingsMenu : MonoBehaviour
    {
        private const float MenuDistance = 0.9f;
        private const float MenuDrop = 0.12f;

        [SerializeField] private SettingsMenuView _view;
        [SerializeField] private InputActionProperty _toggleMenu;
        [SerializeField] private PalmMenuGesture _palmGesture = new();

        [Header("Movement defaults")]
        [SerializeField] private MovementMode _defaultMovementMode = MovementMode.Stick;
        [SerializeField, Range(0f, 100f)] private float _vignetteStrength = 50f;

        [Header("Turning defaults")]
        [SerializeField] private TurnMode _defaultTurnMode = TurnMode.Snap;
        [SerializeField] private float _snapAngle = 45f;
        [SerializeField] private float _smoothTurnSpeed = 90f;
        [SerializeField, Min(0f)] private float _snapDelay = 0.2f;

        [Header("Participant & safety defaults")]
        [SerializeField] private bool _participantIdle;
        [SerializeField] private bool _stickWarnings;
        [SerializeField] private bool _showBody = true;
        [SerializeField, Range(-60f, 40f)] private float _heightOffset;

        private PlayerControl _player;
        private ParticipantControl _participant;
        private PlayerRig _rig;
        private LocomotionLogic _locomotion;

        public void Initialize(PlayerControl player, ParticipantControl participant)
        {
            _player = player;
            _participant = participant;
            _rig = player.Rig;
            _locomotion = player.Locomotion;
            _player.ReturnedToStart += Close;
            _locomotion.SetSnapDelay(_snapDelay);
            _locomotion.SetSnapAngle(_snapAngle);
            _locomotion.SetSmoothTurnSpeed(_smoothTurnSpeed);

            _view.Initialize(_rig.Camera, _vignetteStrength);
            _view.ModeSelected += HandleModeSelected;
            _view.VignetteChanged += _locomotion.SetVignetteStrength;
            _view.TurnModeSelected += HandleTurnModeSelected;
            _view.TurnAmountChanged += HandleTurnAmountChanged;
            _view.ParticipantIdleSelected += HandleParticipantIdleSelected;
            _view.StickWarningsSelected += HandleStickWarningsSelected;
            _view.ShowBodySelected += HandleShowBodySelected;
            _view.HeightChanged += HandleHeightChanged;
            _view.ReturnClicked += _player.ReturnToStart;
            _view.CloseClicked += Close;

            _view.ShowParticipantIdle(_participantIdle);
            _view.ShowStickWarnings(_stickWarnings);
            _view.ShowBody(_showBody);
            _view.ShowHeight(_heightOffset);
            _player.SetHeightOffset(_heightOffset);
            _player.SetBodyVisible(_showBody);
            _participant.SetIdle(_participantIdle);

            _palmGesture.Initialize(_rig);
            _toggleMenu.action.Enable();

            _view.ShowMode(_defaultMovementMode);
            HandleModeSelected(_defaultMovementMode);
            _locomotion.SetVignetteStrength(_vignetteStrength);
            HandleTurnModeSelected(_defaultTurnMode);
        }

        private void Update()
        {
            bool togglePressed = _toggleMenu.action.WasPressedThisFrame();
            bool palmRaised = _palmGesture.Tick(Time.unscaledDeltaTime);
            if (togglePressed || palmRaised)
            {
                SetOpen(!_view.IsOpen);
            }
        }

        private void OnDestroy()
        {
            _toggleMenu.action.Disable();
            _player.ReturnedToStart -= Close;
        }

        private void HandleHeightChanged(float centimetres)
        {
            _heightOffset = centimetres;
            _player.SetHeightOffset(centimetres);
        }

        private void HandleShowBodySelected(bool visible)
        {
            _showBody = visible;
            _player.SetBodyVisible(visible);
        }

        private void HandleParticipantIdleSelected(bool idle)
        {
            _participantIdle = idle;
            _participant.SetIdle(idle);
        }

        private void HandleStickWarningsSelected(bool enabled)
        {
            _stickWarnings = enabled;
            RefreshParticipantMonitoring();
        }

        private void HandleModeSelected(MovementMode mode)
        {
            _locomotion.SetMode(mode);
            RefreshParticipantMonitoring();
        }

        private void RefreshParticipantMonitoring()
        {
            MovementMode mode = _locomotion.Mode;
            _participant.SetMonitoring(mode == MovementMode.Physical || (mode == MovementMode.Stick && _stickWarnings));
        }

        private void HandleTurnModeSelected(TurnMode mode)
        {
            _locomotion.SetTurnMode(mode);
            _view.ShowTurn(mode, mode == TurnMode.Snap ? _snapAngle : _smoothTurnSpeed);
        }

        private void HandleTurnAmountChanged(float amount)
        {
            if (_locomotion.Turn == TurnMode.Snap)
            {
                _snapAngle = amount;
                _locomotion.SetSnapAngle(amount);
            }
            else
            {
                _smoothTurnSpeed = amount;
                _locomotion.SetSmoothTurnSpeed(amount);
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

            _locomotion.SetSuspended(open);
        }

        private void Close() => SetOpen(false);
    }
}
