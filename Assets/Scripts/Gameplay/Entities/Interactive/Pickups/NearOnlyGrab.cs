using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Filtering;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace Gameplay.Entities.Interactive.Pickups
{
    [RequireComponent(typeof(XRBaseInteractable))]
    public sealed class NearOnlyGrab : MonoBehaviour, IXRHoverFilter, IXRSelectFilter
    {
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

        private static bool IsNear(IXRInteractor interactor)
        {
            if (interactor is XRSocketInteractor)
                return true;
            if (interactor is NearFarInteractor nearFar)
                return nearFar.selectionRegion.Value != NearFarInteractor.Region.Far;
            if (interactor is XRRayInteractor)
                return false;
            return true;
        }
    }
}
