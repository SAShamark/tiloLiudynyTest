using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace Gameplay.Entities.Interactive.Pickups
{
    [RequireComponent(typeof(Rigidbody), typeof(XRGrabInteractable))]
    public abstract class Pickup : MonoBehaviour
    {
        [SerializeField] private DropRecovery _recovery = new();

        private HapticImpulsePlayer _lastController;

        protected XRGrabInteractable Grab { get; private set; }
        protected Rigidbody Body { get; private set; }

        public bool IsHeld => Grab.isSelected && Grab.interactorsSelecting[0] is not XRSocketInteractor;
        public bool IsSocketed => Grab.isSelected && Grab.interactorsSelecting[0] is XRSocketInteractor;

        public event Action<Pickup> Recovered;

        protected virtual void Awake()
        {
            Grab = GetComponent<XRGrabInteractable>();
            Body = GetComponent<Rigidbody>();
            _recovery.Initialize(Body, Grab);
        }

        protected virtual void OnEnable() => Grab.selectEntered.AddListener(RememberController);

        protected virtual void OnDisable()
        {
            Grab.selectEntered.RemoveListener(RememberController);
            _lastController = null;
            _recovery.Reset();
        }

        protected virtual void FixedUpdate()
        {
            if (!_recovery.FixedTick(Time.fixedDeltaTime))
                return;

            _lastController = null;
            Recovered?.Invoke(this);
        }

        public void SetHome(Vector3 position, Quaternion rotation) => _recovery.SetHome(position, rotation);

        public void SendHaptics(float amplitude, float duration)
        {
            if (_lastController != null)
                _lastController.SendHapticImpulse(amplitude, duration);
        }

        public void ConfirmInsertion(bool playHaptics)
        {
            if (playHaptics && _lastController != null)
                _lastController.SendHapticImpulse(0.45f, 0.12f);

            _lastController = null;
        }

        private void RememberController(SelectEnterEventArgs args)
        {
            if (args.interactorObject is XRBaseInputInteractor controller)
                _lastController = controller.GetComponentInParent<HapticImpulsePlayer>();
        }
    }
}
