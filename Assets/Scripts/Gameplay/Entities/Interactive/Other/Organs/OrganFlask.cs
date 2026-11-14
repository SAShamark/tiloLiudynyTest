using Gameplay.Entities.Interactive.BaseLogic.Items;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace Gameplay.Entities.Interactive.Other.Organs
{
    public sealed class OrganFlask : ReturningItem
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
            if (_organ.Model != null)
                OrganModelFactory.Spawn(_organ, _previewRoot, _previewSize, out _);
        }

        private void Update()
        {
            _previewRoot.Rotate(0f, _previewSpinSpeed * Time.deltaTime, 0f, Space.Self);
        }

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

        protected override void OnReleased()
        {
            if (!Grab.isSelected)
            {
                Body.isKinematic = false;
            }
        }
    }
}
