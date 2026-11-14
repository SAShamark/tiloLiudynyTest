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

        private PlayerRig _rig;
        private PalmMenuGesture _palmGesture;
        private InputAction _toggleMenu;

        public LocomotionController Locomotion { get; private set; }

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
