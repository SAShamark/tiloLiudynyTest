using System;
using Game.Entities.Pickups;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace Game.Entities.Organs
{
    /// <summary>
    /// Storage cabinet for organ flasks. Asking for an organ slides its flask out onto the tray;
    /// a flask left on the tray slides back in when another one is requested.
    /// Dropped flasks return to their slot.
    /// </summary>
    public sealed class FlaskDispenser : MonoBehaviour
    {
        private enum SlotState { Stored, MovingOut, Presented, MovingIn, Free }

        [Serializable]
        private sealed class Slot
        {
            public OrganFlask Flask;
            public Transform StoredPoint;
            [NonSerialized] public SlotState State;
            [NonSerialized] public float Progress;
        }

        [SerializeField] private Slot[] _slots;
        [SerializeField] private Transform _presentPoint;
        [SerializeField, Min(0.1f)] private float _moveDuration = 0.9f;
        [SerializeField] private AudioClip _moveClip;

        private AudioSource _audio;

        public void Init()
        {
            _audio = gameObject.AddComponent<AudioSource>();
            _audio.playOnAwake = false;
            _audio.spatialBlend = 1f;
            _audio.maxDistance = 4f;

            foreach (var slot in _slots)
            {
                slot.Flask.SetHome(slot.StoredPoint.position, slot.StoredPoint.rotation);
                slot.Flask.OnRecovered += HandleRecovered;
                slot.Flask.Interactable.selectEntered.AddListener(HandleGrabbed);

                // A flask placed elsewhere in the scene (e.g. already in the station socket) starts out of the cabinet.
                if (Vector3.Distance(slot.Flask.transform.position, slot.StoredPoint.position) > 0.05f)
                {
                    slot.State = SlotState.Free;
                    continue;
                }

                Store(slot);
            }
        }

        public void Present(OrganConfig organ)
        {
            Slot requested = Find(organ);
            if (requested == null || requested.State is SlotState.MovingOut or SlotState.Presented)
                return;

            if (requested.State == SlotState.Free)
            {
                // The flask is out in the room; only recall it if nobody holds it and it is not in a socket.
                if (requested.Flask.IsHeld || requested.Flask.IsSocketed)
                    return;

                Store(requested);
            }

            foreach (var slot in _slots)
            {
                if (slot != requested && slot.State is SlotState.Presented or SlotState.MovingOut)
                    StartMove(slot, SlotState.MovingIn);
            }

            StartMove(requested, SlotState.MovingOut);
        }

        private void OnDestroy()
        {
            if (_slots == null)
                return;

            foreach (var slot in _slots)
            {
                if (slot.Flask == null)
                    continue;

                slot.Flask.OnRecovered -= HandleRecovered;
                slot.Flask.Interactable.selectEntered.RemoveListener(HandleGrabbed);
            }
        }

        private void Update()
        {
            foreach (var slot in _slots)
            {
                if (slot.State is not (SlotState.MovingOut or SlotState.MovingIn))
                    continue;

                slot.Progress = Mathf.MoveTowards(slot.Progress, 1f, Time.deltaTime / _moveDuration);
                float eased = Mathf.SmoothStep(0f, 1f, slot.Progress);
                bool outward = slot.State == SlotState.MovingOut;
                Transform from = outward ? slot.StoredPoint : _presentPoint;
                Transform to = outward ? _presentPoint : slot.StoredPoint;
                slot.Flask.MoveTo(Vector3.Lerp(from.position, to.position, eased),
                    Quaternion.Slerp(from.rotation, to.rotation, eased));

                if (slot.Progress < 1f)
                    continue;

                if (outward)
                {
                    slot.State = SlotState.Presented;
                    slot.Flask.Park(grabbable: true);
                }
                else
                {
                    Store(slot);
                }
            }
        }

        private void StartMove(Slot slot, SlotState state)
        {
            slot.State = state;
            slot.Progress = 0f;
            slot.Flask.Park(grabbable: false);
            if (_moveClip != null)
                _audio.PlayOneShot(_moveClip, 0.5f);
        }

        private void Store(Slot slot)
        {
            slot.State = SlotState.Stored;
            slot.Progress = 0f;
            slot.Flask.Park(grabbable: false);
            slot.Flask.MoveTo(slot.StoredPoint.position, slot.StoredPoint.rotation);
        }

        private Slot Find(OrganConfig organ)
        {
            foreach (var slot in _slots)
            {
                if (slot.Flask != null && slot.Flask.Organ == organ)
                    return slot;
            }

            return null;
        }

        private Slot Find(OrganFlask flask)
        {
            foreach (var slot in _slots)
            {
                if (slot.Flask == flask)
                    return slot;
            }

            return null;
        }

        private void HandleGrabbed(SelectEnterEventArgs args)
        {
            if (args.interactableObject.transform.TryGetComponent(out OrganFlask flask) && Find(flask) is { } slot)
                slot.State = SlotState.Free;
        }

        private void HandleRecovered(Pickup pickup)
        {
            if (pickup is OrganFlask flask && Find(flask) is { } slot)
                Store(slot);
        }
    }
}
