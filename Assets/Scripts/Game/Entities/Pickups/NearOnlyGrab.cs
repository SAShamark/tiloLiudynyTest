using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Filtering;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace Game.Entities.Pickups
{
    /// <summary>
    /// Lets the item be picked up only by reaching for it with the hand: rejects hover and select from the
    /// far ray of a Near-Far interactor, so the item is never grabbed from across the room.
    /// </summary>
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
