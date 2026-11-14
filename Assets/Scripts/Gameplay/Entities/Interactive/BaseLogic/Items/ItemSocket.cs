using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace Gameplay.Entities.Interactive.BaseLogic.Items
{
    [Serializable]
    public sealed class ItemSocket<T> where T : ReturningItem
    {
        [SerializeField] private XRSocketInteractor _interactor;

        public XRSocketInteractor Interactor => _interactor;
        public T Current { get; private set; }

        public event Action<T> Inserted;
        public event Action<T> Removed;

        public void Initialize()
        {
            _interactor.selectEntered.AddListener(HandleSelectEntered);
            _interactor.selectExited.AddListener(HandleSelectExited);
        }

        public void Dispose()
        {
            _interactor.selectEntered.RemoveListener(HandleSelectEntered);
            _interactor.selectExited.RemoveListener(HandleSelectExited);
        }

        private void HandleSelectEntered(SelectEnterEventArgs args)
        {
            if (!args.interactableObject.transform.TryGetComponent(out T item))
            {
                return;
            }

            Current = item;
            Inserted?.Invoke(item);
        }

        private void HandleSelectExited(SelectExitEventArgs args)
        {
            if (!args.interactableObject.transform.TryGetComponent(out T item))
            {
                return;
            }

            if (Current == item)
            {
                Current = null;
            }

            Removed?.Invoke(item);
        }
    }
}
