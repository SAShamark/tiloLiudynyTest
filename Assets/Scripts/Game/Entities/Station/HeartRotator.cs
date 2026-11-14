using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace Game.Entities.Station
{
    /// <summary>
    /// Lets the player grab the heart and turn it like a ball to inspect it.
    /// After release the heart eases back to its resting orientation.
    /// </summary>
    [RequireComponent(typeof(XRSimpleInteractable))]
    public sealed class HeartRotator : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float _sensitivity = 1.6f;
        [SerializeField, Min(0f)] private float _returnDelay = 2f;
        [SerializeField, Min(0.1f)] private float _returnSpeed = 2.5f;

        private XRSimpleInteractable _interactable;
        private Quaternion _restRotation;
        private Quaternion _grabRotation;
        private Vector3 _grabDirection;
        private float _releasedTime;

        private void Awake()
        {
            _interactable = GetComponent<XRSimpleInteractable>();
            _restRotation = transform.localRotation;
        }

        private void OnEnable() => _interactable.selectEntered.AddListener(HandleGrab);

        private void OnDisable() => _interactable.selectEntered.RemoveListener(HandleGrab);

        private void LateUpdate()
        {
            if (_interactable.isSelected)
            {
                Rotate();
                _releasedTime = 0f;
                return;
            }

            _releasedTime += Time.deltaTime;
            if (_releasedTime < _returnDelay)
                return;

            float blend = 1f - Mathf.Exp(-_returnSpeed * Time.deltaTime);
            transform.localRotation = Quaternion.Slerp(transform.localRotation, _restRotation, blend);
        }

        private void HandleGrab(SelectEnterEventArgs args)
        {
            _grabRotation = transform.rotation;
            _grabDirection = HandDirection();
        }

        private void Rotate()
        {
            Vector3 current = HandDirection();
            if (current.sqrMagnitude < 0.0001f || _grabDirection.sqrMagnitude < 0.0001f)
                return;

            Quaternion delta = Quaternion.FromToRotation(_grabDirection, current);
            Quaternion amplified = Quaternion.SlerpUnclamped(Quaternion.identity, delta, _sensitivity);
            transform.rotation = amplified * _grabRotation;
        }

        private Vector3 HandDirection()
        {
            var interactor = _interactable.interactorsSelecting[0];
            Vector3 hand = interactor.GetAttachTransform(_interactable).position;
            return (hand - transform.position).normalized;
        }
    }
}
