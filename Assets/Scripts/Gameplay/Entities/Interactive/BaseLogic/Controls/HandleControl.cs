using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace Gameplay.Entities.Interactive.BaseLogic.Controls
{
    public abstract class HandleControl : MonoBehaviour
    {
        [SerializeField, FormerlySerializedAs("_knob")] private XRSimpleInteractable _interactable;

        protected bool IsGrabbed => _interactable.isSelected;
        protected Vector3 HandPosition => _interactable.interactorsSelecting[0].GetAttachTransform(_interactable).position;
        protected Vector3 GrabPosition { get; private set; }

        protected virtual void OnEnable() => _interactable.selectEntered.AddListener(HandleSelected);

        protected virtual void OnDisable() => _interactable.selectEntered.RemoveListener(HandleSelected);

        protected virtual void OnGrabbed()
        {
        }

        private void HandleSelected(SelectEnterEventArgs _)
        {
            GrabPosition = HandPosition;
            OnGrabbed();
        }
    }
}
