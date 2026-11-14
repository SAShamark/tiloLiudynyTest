using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Attachment;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Comfort;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;

namespace Game.Comfort
{
    /// <summary>
    /// Switches XRI locomotion providers according to the selected movement and turn settings.
    /// </summary>
    public sealed class LocomotionController
    {
        private readonly TeleportationArea _floor;
        private readonly ContinuousMoveProvider _move;
        private readonly TeleportationProvider _teleport;
        private readonly SnapTurnProvider _snapTurn;
        private readonly ContinuousTurnProvider _continuousTurn;
        private readonly IReadOnlyList<ControllerInputActionManager> _inputManagers;
        private readonly TunnelingVignetteController _vignette;
        private readonly LocomotionVignetteProvider _moveVignette;
        private readonly MeshRenderer _vignetteRenderer;
        private bool _suspended;

        public MovementMode Mode { get; private set; }
        public TurnMode Turn { get; private set; } = TurnMode.Snap;
        public bool IsPhysicalOnly => Mode == MovementMode.Physical;

        public event Action<MovementMode> OnModeChanged;

        public LocomotionController(Transform head, TeleportationArea floor, TunnelingVignetteController vignettePrefab,
            ContinuousMoveProvider move, TeleportationProvider teleport,
            SnapTurnProvider snapTurn, ContinuousTurnProvider continuousTurn,
            IReadOnlyList<ControllerInputActionManager> inputManagers,
            IEnumerable<InteractionAttachController> attachControllers)
        {
            _floor = floor;
            _move = move;
            _teleport = teleport;
            _snapTurn = snapTurn;
            _continuousTurn = continuousTurn;
            _inputManagers = inputManagers;

            // The starter rig uses the thumbstick to push/pull and rotate held objects, which blocks
            // turning while something is grabbed with the ray. Turning matters more here.
            foreach (var attachController in attachControllers)
                attachController.useManipulationInput = false;

            _floor.teleportationProvider = _teleport;
            _vignette = UnityEngine.Object.Instantiate(vignettePrefab, head, false);
            _vignetteRenderer = _vignette.GetComponent<MeshRenderer>();
            _moveVignette = new LocomotionVignetteProvider { locomotionProvider = _move, enabled = true };
            _vignette.locomotionVignetteProviders.Add(_moveVignette);
        }

        public void SetMode(MovementMode mode)
        {
            Mode = mode;
            Apply();
            OnModeChanged?.Invoke(mode);
        }

        public void SetTurnMode(TurnMode turn)
        {
            Turn = turn;
            foreach (var manager in _inputManagers)
                manager.smoothTurnEnabled = turn == TurnMode.Smooth;
            Apply();
        }

        public void SetSnapAngle(float degrees)
        {
            if (_snapTurn != null)
                _snapTurn.turnAmount = degrees;
        }

        public void SetSnapDelay(float seconds)
        {
            if (_snapTurn != null)
                _snapTurn.debounceTime = seconds;
        }

        public void SetSmoothTurnSpeed(float degreesPerSecond)
        {
            if (_continuousTurn != null)
                _continuousTurn.turnSpeed = degreesPerSecond;
        }

        /// <summary>Disables artificial locomotion while a menu is open.</summary>
        public void SetSuspended(bool suspended)
        {
            _suspended = suspended;
            Apply();
        }

        public void SetVignetteStrength(float percent)
        {
            _vignette.defaultParameters.apertureSize = Mathf.Lerp(1f, 0.55f, percent / 100f);
            _vignetteRenderer.enabled = percent > 0f;
        }

        private void Apply()
        {
            bool active = !_suspended;
            bool canTurn = active && !IsPhysicalOnly;
            _move.enabled = active && Mode == MovementMode.Stick;
            _teleport.enabled = active && Mode == MovementMode.Teleport;
            _floor.enabled = _teleport.enabled;
            if (!_move.enabled)
                _vignette.EndTunnelingVignette(_moveVignette);
            if (_snapTurn != null)
                _snapTurn.enabled = canTurn && Turn == TurnMode.Snap;
            if (_continuousTurn != null)
                _continuousTurn.enabled = canTurn && Turn == TurnMode.Smooth;
        }
    }
}
