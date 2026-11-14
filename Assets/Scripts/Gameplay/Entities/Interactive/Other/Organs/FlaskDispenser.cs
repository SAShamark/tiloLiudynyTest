using System;
using System.Collections.Generic;
using Gameplay.Entities.Interactive.BaseLogic.Items;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace Gameplay.Entities.Interactive.Other.Organs
{
    public sealed class FlaskDispenser : MonoBehaviour
    {
        private enum SlotState { Stored, Dispensing, Presented, Retracting, Returning, Free }

        [Serializable]
        private sealed class Slot
        {
            public OrganFlask Flask;
            public Transform StoredPoint;
            [NonSerialized] public SlotState State;
            [NonSerialized] public float Progress;
            [NonSerialized] public Vector3 FromPosition;
            [NonSerialized] public Quaternion FromRotation;
        }

        [SerializeField] private AudioSource _audio;

        [SerializeField] private Slot[] _slots;
        [Tooltip("Point inside the machine, behind the chute, where a flask lands before rolling out.")]
        [SerializeField] private Transform _chutePoint;
        [SerializeField] private Transform _presentPoint;
        [SerializeField, Min(0.1f)] private float _dropDuration = 0.35f;
        [SerializeField, Min(0.1f)] private float _rollDuration = 0.7f;
        [SerializeField, Min(0.1f)] private float _returnDuration = 0.6f;
        [Tooltip("Flask radius used to turn rolling distance into spin.")]
        [SerializeField, Min(0.01f)] private float _flaskRadius = 0.045f;
        [SerializeField] private AudioClip _dispenseClip;

        public OrganConfig PresentedOrgan
        {
            get
            {
                foreach (var slot in _slots)
                {
                    if (slot.State is SlotState.Dispensing or SlotState.Presented)
                        return slot.Flask.Organ;
                }

                return null;
            }
        }

        public event Action Changed;

        public IReadOnlyList<OrganFlask> Flasks
        {
            get
            {
                var flasks = new List<OrganFlask>(_slots.Length);
                foreach (var slot in _slots)
                    flasks.Add(slot.Flask);
                return flasks;
            }
        }

        public void Initialize()
        {

            foreach (var slot in _slots)
            {
                slot.Flask.SetHome(slot.StoredPoint.position, slot.StoredPoint.rotation);
                slot.Flask.Recovered += HandleRecovered;
                slot.Flask.Interactable.selectEntered.AddListener(HandleGrabbed);

                Store(slot);
            }
        }

        public void Toggle(OrganConfig organ)
        {
            Slot requested = Find(organ);
            if (requested == null)
                return;

            switch (requested.State)
            {
                case SlotState.Dispensing:
                case SlotState.Presented:
                    StartMove(requested, SlotState.Retracting);
                    Notify();
                    return;
                case SlotState.Free when requested.Flask.IsHeld || requested.Flask.IsSocketed:
                    return;
                case SlotState.Free:
                case SlotState.Returning:
                    Store(requested);
                    break;
            }

            foreach (var slot in _slots)
            {
                if (slot != requested && slot.State is SlotState.Presented or SlotState.Dispensing)
                    StartMove(slot, SlotState.Retracting);
            }

            StartMove(requested, SlotState.Dispensing);
            _audio.PlayOneShot(_dispenseClip, 0.6f);
            Notify();
        }

        public void Return(OrganFlask flask)
        {
            Slot slot = Find(flask);
            if (slot == null || slot.Flask.IsHeld)
                return;

            StartMove(slot, SlotState.Returning);
            Notify();
        }

        private void OnDestroy()
        {
            foreach (var slot in _slots)
            {
                slot.Flask.Recovered -= HandleRecovered;
                slot.Flask.Interactable.selectEntered.RemoveListener(HandleGrabbed);
            }
        }

        private void Update()
        {
            foreach (var slot in _slots)
            {
                switch (slot.State)
                {
                    case SlotState.Dispensing:
                        UpdateDispensing(slot);
                        break;
                    case SlotState.Retracting:
                        UpdateRetracting(slot);
                        break;
                    case SlotState.Returning:
                        UpdateReturning(slot);
                        break;
                }
            }
        }

        private void UpdateDispensing(Slot slot)
        {
            float total = _dropDuration + _rollDuration;
            slot.Progress = Mathf.MoveTowards(slot.Progress, 1f, Time.deltaTime / total);
            float time = slot.Progress * total;

            if (time < _dropDuration)
            {
                float progress = time / _dropDuration;
                slot.Flask.MoveTo(Vector3.Lerp(slot.StoredPoint.position, _chutePoint.position, progress * progress),
                    Quaternion.Slerp(slot.StoredPoint.rotation, _chutePoint.rotation, progress));
            }
            else
            {
                float progress = Mathf.SmoothStep(0f, 1f, (time - _dropDuration) / _rollDuration);
                Vector3 position = Vector3.Lerp(_chutePoint.position, _presentPoint.position, progress);
                float distance = Vector3.Distance(_chutePoint.position, position);
                float spin = distance / _flaskRadius * Mathf.Rad2Deg;
                slot.Flask.MoveTo(position, _presentPoint.rotation * Quaternion.AngleAxis(spin, Vector3.up));
            }

            if (slot.Progress < 1f)
                return;

            slot.State = SlotState.Presented;
            slot.Flask.MoveTo(_presentPoint.position, _presentPoint.rotation * Quaternion.AngleAxis(
                Vector3.Distance(_chutePoint.position, _presentPoint.position) / _flaskRadius * Mathf.Rad2Deg, Vector3.up));
            slot.Flask.Park(grabbable: true);
            Notify();
        }

        private void UpdateRetracting(Slot slot)
        {
            slot.Progress = Mathf.MoveTowards(slot.Progress, 1f, Time.deltaTime / (_rollDuration * 0.6f));
            float progress = Mathf.SmoothStep(0f, 1f, slot.Progress);
            slot.Flask.MoveTo(Vector3.Lerp(slot.FromPosition, _chutePoint.position, progress), slot.FromRotation);
            if (slot.Progress >= 1f)
                Store(slot);
        }

        private void UpdateReturning(Slot slot)
        {
            slot.Progress = Mathf.MoveTowards(slot.Progress, 1f, Time.deltaTime / _returnDuration);
            float progress = Mathf.SmoothStep(0f, 1f, slot.Progress);
            Vector3 position = Vector3.Lerp(slot.FromPosition, slot.StoredPoint.position, progress)
                + Vector3.up * (Mathf.Sin(progress * Mathf.PI) * 0.25f);
            slot.Flask.MoveTo(position, Quaternion.Slerp(slot.FromRotation, slot.StoredPoint.rotation, progress));
            if (slot.Progress >= 1f)
                Store(slot);
        }

        private void StartMove(Slot slot, SlotState state)
        {
            slot.State = state;
            slot.Progress = 0f;
            slot.FromPosition = slot.Flask.transform.position;
            slot.FromRotation = slot.Flask.transform.rotation;
            slot.Flask.Park(grabbable: false);
        }

        private void Store(Slot slot)
        {
            bool changed = slot.State != SlotState.Stored;
            slot.State = SlotState.Stored;
            slot.Progress = 0f;
            slot.Flask.Park(grabbable: false);
            slot.Flask.MoveTo(slot.StoredPoint.position, slot.StoredPoint.rotation);
            if (changed)
                Notify();
        }

        private Slot Find(OrganConfig organ)
        {
            foreach (var slot in _slots)
            {
                if (slot.Flask.Organ == organ)
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
            if (args.interactableObject.transform.TryGetComponent(out OrganFlask flask) && Find(flask) is { } slot
                && slot.State != SlotState.Free)
            {
                slot.State = SlotState.Free;
                Notify();
            }
        }

        private void HandleRecovered(ReturningItem item)
        {
            if (item is OrganFlask flask && Find(flask) is { } slot)
                Store(slot);
        }

        private void Notify() => Changed?.Invoke();
    }
}
