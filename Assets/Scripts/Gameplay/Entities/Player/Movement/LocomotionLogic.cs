using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Attachment;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Comfort;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;

namespace Gameplay.Entities.Player.Movement
{
    [Serializable]
    public sealed class LocomotionLogic
    {
        [SerializeField] private TeleportationArea _floor;
        [SerializeField] private ContinuousMoveProvider _move;
        [SerializeField] private TeleportationProvider _teleport;
        [SerializeField] private SnapTurnProvider _snapTurn;
        [SerializeField] private ContinuousTurnProvider _continuousTurn;
        [SerializeField] private ControllerInputActionManager[] _inputManagers;
        [SerializeField] private InteractionAttachController[] _attachControllers;
        [SerializeField] private TunnelingVignetteController _vignette;
        [SerializeField] private MeshRenderer _vignetteRenderer;

        private LocomotionVignetteProvider _moveVignette;
        private bool _suspended;

        public MovementMode Mode { get; private set; }
        public TurnMode Turn { get; private set; } = TurnMode.Snap;

        public void Initialize()
        {
            foreach (var attachController in _attachControllers)
            {
                attachController.useManipulationInput = false;
            }

            _floor.teleportationProvider = _teleport;
            _moveVignette = new LocomotionVignetteProvider { locomotionProvider = _move, enabled = true };
            _vignette.locomotionVignetteProviders.Add(_moveVignette);
        }

        public void SetMode(MovementMode mode)
        {
            Mode = mode;
            Apply();
        }

        public void SetTurnMode(TurnMode turn)
        {
            Turn = turn;
            foreach (var manager in _inputManagers)
            {
                manager.smoothTurnEnabled = turn == TurnMode.Smooth;
            }

            Apply();
        }

        public void SetSnapAngle(float degrees) => _snapTurn.turnAmount = degrees;

        public void SetSnapDelay(float seconds) => _snapTurn.debounceTime = seconds;

        public void SetSmoothTurnSpeed(float degreesPerSecond) => _continuousTurn.turnSpeed = degreesPerSecond;

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
            bool canTurn = active && Mode != MovementMode.Physical;
            _move.enabled = active && Mode == MovementMode.Stick;
            _teleport.enabled = active && Mode == MovementMode.Teleport;
            _floor.enabled = _teleport.enabled;
            if (!_move.enabled)
            {
                _vignette.EndTunnelingVignette(_moveVignette);
            }

            _snapTurn.enabled = canTurn && Turn == TurnMode.Snap;
            _continuousTurn.enabled = canTurn && Turn == TurnMode.Smooth;
        }
    }
}
