using System;
using Gameplay.Entities.Player.Hands;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace Gameplay.Entities.Interactive.BaseLogic.Buttons
{
    [RequireComponent(typeof(XRSimpleInteractable))]
    public abstract class PressButton : MonoBehaviour
    {
        [SerializeField] private Transform _cap;
        [Tooltip("Cap offset in its parent space while the button is held down.")]
        [SerializeField] private Vector3 _pressOffset = new(0f, -0.01f, 0f);
        [SerializeField, Min(1f)] private float _capSpeed = 8f;
        [SerializeField, Min(0f)] private float _touchCooldown = 0.4f;

        private XRSimpleInteractable _interactable;
        private Vector3 _capRest;
        private float _capProgress;
        private int _touching;
        private float _nextTouch;

        public event Action Pressed;

        protected virtual void Awake()
        {
            _interactable = GetComponent<XRSimpleInteractable>();
            _capRest = _cap.localPosition;
        }

        protected virtual void OnEnable() => _interactable.selectEntered.AddListener(HandleSelected);

        protected virtual void OnDisable()
        {
            _interactable.selectEntered.RemoveListener(HandleSelected);
            _touching = 0;
        }

        protected virtual void Update()
        {
            float target = _interactable.isSelected || _touching > 0 ? 1f : 0f;
            if (Mathf.Approximately(_capProgress, target))
            {
                return;
            }

            _capProgress = Mathf.MoveTowards(_capProgress, target, _capSpeed * Time.deltaTime);
            _cap.localPosition = _capRest + _pressOffset * _capProgress;
        }

        protected virtual void OnPressed()
        {
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!PhysicalHandBody.IsHand(other))
            {
                return;
            }

            if (_touching++ == 0 && Time.time >= _nextTouch)
            {
                _nextTouch = Time.time + _touchCooldown;
                Press();
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (PhysicalHandBody.IsHand(other))
            {
                _touching = Mathf.Max(0, _touching - 1);
            }
        }

        private void HandleSelected(SelectEnterEventArgs _) => Press();

        private void Press()
        {
            _capProgress = 1f;
            _cap.localPosition = _capRest + _pressOffset;
            OnPressed();
            Pressed?.Invoke();
        }
    }
}
