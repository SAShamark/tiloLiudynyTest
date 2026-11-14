using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace Gameplay.Entities.Interactive.Base.Items
{
    [RequireComponent(typeof(Rigidbody), typeof(XRGrabInteractable))]
    public abstract class GrabItem : MonoBehaviour
    {
        private HapticImpulsePlayer _lastController;

        protected XRGrabInteractable Grab { get; private set; }
        protected Rigidbody Body { get; private set; }

        public bool IsHeld => Grab.isSelected && Grab.interactorsSelecting[0] is not XRSocketInteractor;
        public bool IsSocketed => Grab.isSelected && Grab.interactorsSelecting[0] is XRSocketInteractor;

        public event Action<GrabItem> Grabbed;

        protected virtual void Awake()
        {
            Grab = GetComponent<XRGrabInteractable>();
            Body = GetComponent<Rigidbody>();
        }

        protected virtual void OnEnable()
        {
            Grab.selectEntered.AddListener(HandleSelectEntered);
            Grab.selectExited.AddListener(HandleSelectExited);
            Grab.activated.AddListener(HandleActivated);
        }

        protected virtual void OnDisable()
        {
            Grab.selectEntered.RemoveListener(HandleSelectEntered);
            Grab.selectExited.RemoveListener(HandleSelectExited);
            Grab.activated.RemoveListener(HandleActivated);
            _lastController = null;
        }

        public void SendHaptics(float amplitude, float duration)
        {
            if (_lastController != null)
            {
                _lastController.SendHapticImpulse(amplitude, duration);
            }
        }

        protected void ForgetController() => _lastController = null;

        protected virtual void OnReleased()
        {
        }

        protected virtual void OnActivated()
        {
        }

        private void HandleSelectEntered(SelectEnterEventArgs args)
        {
            if (args.interactorObject is XRBaseInputInteractor controller)
            {
                _lastController = controller.GetComponentInParent<HapticImpulsePlayer>();
                Grabbed?.Invoke(this);
            }
        }

        private void HandleSelectExited(SelectExitEventArgs _) => OnReleased();

        private void HandleActivated(ActivateEventArgs _) => OnActivated();
    }
}
