using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace Gameplay.Entities.Player.Hands
{
    public sealed class PhysicalHandsControl : MonoBehaviour
    {
        [Serializable]
        private sealed class HandSide
        {
            public PhysicalHand Hand;
            public Transform Controller;
            public GameObject ControllerVisual;
            public XRBaseInteractor[] Interactors;
        }

        [SerializeField] private HandSide _left = new();
        [SerializeField] private HandSide _right = new();
        [SerializeField] private Collider[] _rigColliders;
        [SerializeField] private PhysicalHandSettings _settings;

        public void Initialize()
        {
            InitializeSide(_left);
            InitializeSide(_right);
            _left.Hand.IgnoreOtherHand(_right.Hand);
            Application.onBeforeRender += HandleBeforeRender;
        }

        public Transform GetWrist(bool left)
        {
            PhysicalHand hand = left ? _left.Hand : _right.Hand;
            return hand.IsVisible ? hand.Wrist : null;
        }

        private void InitializeSide(HandSide side)
        {
            side.ControllerVisual.SetActive(false);
            side.Hand.Initialize(_settings, side.Controller, side.Interactors);
            foreach (var hand in side.Hand.Colliders)
            {
                foreach (var rigCollider in _rigColliders)
                {
                    Physics.IgnoreCollision(hand, rigCollider, true);
                }
            }
        }

        private void OnDestroy()
        {
            Application.onBeforeRender -= HandleBeforeRender;
            _left.Hand.Dispose();
            _right.Hand.Dispose();
        }

        private void FixedUpdate()
        {
            _left.Hand.FixedTick(Time.fixedDeltaTime);
            _right.Hand.FixedTick(Time.fixedDeltaTime);
        }

        private void LateUpdate()
        {
            _left.Hand.LateTick(Time.unscaledDeltaTime);
            _right.Hand.LateTick(Time.unscaledDeltaTime);
        }

        private void HandleBeforeRender()
        {
            _left.Hand.FollowHeldItem();
            _right.Hand.FollowHeldItem();
        }
    }
}
