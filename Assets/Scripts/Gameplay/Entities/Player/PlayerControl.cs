using System;
using Gameplay.Entities.Player.Body;
using Gameplay.Entities.Player.Hands;
using Gameplay.Entities.Player.Movement;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;

namespace Gameplay.Entities.Player
{
    public sealed class PlayerControl : MonoBehaviour
    {
        [SerializeField] private XROrigin _xrOrigin;
        [SerializeField] private HapticImpulsePlayer _leftHaptics;
        [SerializeField] private HapticImpulsePlayer _rightHaptics;
        [SerializeField] private PhysicalHandsControl _hands;
        [SerializeField] private PlayerBodyView _body;
        [SerializeField] private LocomotionLogic _locomotion = new();

        [Header("Play area")]
        [Tooltip("Walkable floor in world XZ (x = X, y = Z). Leaving it returns the player to the start.")]
        [SerializeField] private Rect _playArea = Rect.MinMaxRect(-3.9f, -7.4f, 3.9f, 2.4f);
        [SerializeField] private float _minHeadHeight = -0.5f;

        private float _appliedHeight;

        public PlayerRig Rig { get; private set; }
        public LocomotionLogic Locomotion => _locomotion;

        public event Action ReturnedToStart;

        public void Initialize()
        {
            Rig = new PlayerRig(_xrOrigin, _leftHaptics, _rightHaptics);
            _hands.Initialize();
            _locomotion.Initialize();
            _body.Initialize(Rig, _hands);
        }

        public void SetBodyVisible(bool visible) => _body.SetVisible(visible);

        public void SetHeightOffset(float centimetres)
        {
            float wanted = centimetres * 0.01f;
            Transform space = Rig.TrackingSpace;
            Vector3 position = space.localPosition;
            position.y += wanted - _appliedHeight;
            space.localPosition = position;
            _appliedHeight = wanted;
        }

        public void ReturnToStart()
        {
            Rig.ReturnToStart();
            ReturnedToStart?.Invoke();
        }

        private void Update()
        {
            if (Rig.IsOutsidePlayArea(_playArea, _minHeadHeight))
            {
                ReturnToStart();
            }
        }
    }
}
