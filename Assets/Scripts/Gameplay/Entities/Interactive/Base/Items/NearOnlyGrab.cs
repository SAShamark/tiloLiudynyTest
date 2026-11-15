using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Filtering;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace Gameplay.Entities.Interactive.Base.Items
{
    [RequireComponent(typeof(XRBaseInteractable))]
    public sealed class NearOnlyGrab : MonoBehaviour, IXRHoverFilter, IXRSelectFilter
    {
        [SerializeField, Min(0f)] private float _maxHandDistance = 0.15f;

        private XRBaseInteractable _interactable;

        public bool canProcess => isActiveAndEnabled;

        private void Awake() => _interactable = GetComponent<XRBaseInteractable>();

        private void OnEnable()
        {
            _interactable.hoverFilters.Add(this);
            _interactable.selectFilters.Add(this);
        }

        private void OnDisable()
        {
            _interactable.hoverFilters.Remove(this);
            _interactable.selectFilters.Remove(this);
        }

        public bool Process(IXRHoverInteractor interactor, IXRHoverInteractable interactable) => IsNear(interactor);

        public bool Process(IXRSelectInteractor interactor, IXRSelectInteractable interactable) => IsNear(interactor);

        private bool IsNear(IXRInteractor interactor)
        {
            if (interactor is XRSocketInteractor)
            {
                return true;
            }

            Vector3 hand = interactor.GetAttachTransform(_interactable).position;
            float maxDistanceSqr = _maxHandDistance * _maxHandDistance;
            foreach (var collider in _interactable.colliders)
            {
                if ((collider.ClosestPoint(hand) - hand).sqrMagnitude <= maxDistanceSqr)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
