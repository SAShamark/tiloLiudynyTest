using Game.Entities.Pickups;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace Game.Entities.Organs
{
    /// <summary>
    /// Glass flask with a miniature organ inside. Inserting it into the station projects that organ.
    /// While parked in the dispenser it is kinematic and cannot be grabbed.
    /// </summary>
    public sealed class OrganFlask : Pickup
    {
        [SerializeField] private OrganConfig _organ;
        [SerializeField] private Transform _previewRoot;
        [SerializeField, Min(0.01f)] private float _previewSize = 0.09f;
        [SerializeField] private float _previewSpinSpeed = 25f;

        public OrganConfig Organ => _organ;
        public XRGrabInteractable Interactable => Grab;

        protected override void Awake()
        {
            base.Awake();
            if (_organ != null && _organ.Model != null && _previewRoot != null)
                OrganModelFactory.Spawn(_organ, _previewRoot, _previewSize, out _);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            Grab.selectExited.AddListener(HandleReleased);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            Grab.selectExited.RemoveListener(HandleReleased);
        }

        private void Update()
        {
            if (_previewRoot != null)
                _previewRoot.Rotate(0f, _previewSpinSpeed * Time.deltaTime, 0f, Space.Self);
        }

        /// <summary>Holds the flask in place (dispenser slot or tray); grabbing can be allowed or not.</summary>
        public void Park(bool grabbable)
        {
            Body.isKinematic = true;
            Grab.enabled = grabbable;
        }

        public void MoveTo(Vector3 position, Quaternion rotation)
        {
            transform.SetPositionAndRotation(position, rotation);
            Body.position = position;
            Body.rotation = rotation;
        }

        // XRI restores the pre-grab kinematic state on release, which would leave a flask taken from
        // the tray floating in the air. Once released by a hand it should fall like a normal object.
        private void HandleReleased(SelectExitEventArgs args)
        {
            if (!Grab.isSelected)
                Body.isKinematic = false;
        }
    }
}
