using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace Game.Entities.Station
{
    public sealed class StationLever : MonoBehaviour
    {
        private Transform _pivot;
        private Renderer _grip;
        private Material _bodyMaterial;
        private Material _readyMaterial;
        private XRSimpleInteractable _interaction;
        private Func<bool> _canPull;
        private Action _onPull;
        private Vector3 _grabStart;
        private bool _held;
        private bool _fired;
        private float _angle;

        public void Init(Material body, Material ready, Func<bool> canPull, Action onPull)
        {
            _canPull = canPull;
            _onPull = onPull;
            _bodyMaterial = body;
            _readyMaterial = ready;
            _pivot = new GameObject("Lever pivot").transform;
            _pivot.SetParent(transform, false);
            CreatePart(transform, "Base", new Vector3(0f, -0.025f, 0f), new Vector3(0.12f, 0.05f, 0.16f), body);
            CreatePart(_pivot, "Arm", new Vector3(0f, 0.09f, 0f), new Vector3(0.035f, 0.18f, 0.035f), body);
            var grip = CreatePart(_pivot, "Grip", new Vector3(0f, 0.18f, 0f), new Vector3(0.13f, 0.068f, 0.068f), ready);
            _grip = grip.GetComponent<Renderer>();
            _interaction = grip.AddComponent<XRSimpleInteractable>();
            _interaction.selectEntered.AddListener(args =>
            {
                _grabStart = transform.InverseTransformPoint(args.interactorObject.GetAttachTransform(_interaction).position);
                _held = true;
                _fired = false;
            });
            _interaction.selectExited.AddListener(_ => _held = false);
        }

        private static GameObject CreatePart(Transform parent, string partName, Vector3 position, Vector3 scale, Material material)
        {
            var part = GameObject.CreatePrimitive(PrimitiveType.Cube);
            part.name = partName;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = position;
            part.transform.localScale = scale;
            part.GetComponent<Renderer>().sharedMaterial = material;
            if (partName != "Grip")
                part.GetComponent<Collider>().enabled = false;
            return part;
        }

        private void LateUpdate()
        {
            bool ready = _canPull();
            var material = ready ? _readyMaterial : _bodyMaterial;
            if (_grip.sharedMaterial != material)
                _grip.sharedMaterial = material;
            float pull = 0f;
            if (_held && _interaction.isSelected)
            {
                var hand = _interaction.interactorsSelecting[0].GetAttachTransform(_interaction);
                Vector3 offset = transform.InverseTransformPoint(hand.position) - _grabStart;
                pull = Mathf.Clamp01(-offset.z / 0.16f);
                if (ready && !_fired && pull >= 0.85f)
                {
                    _fired = true;
                    _onPull();
                }
            }

            _angle = Mathf.MoveTowards(_angle, (ready ? -52f : -8f) * pull, 360f * Time.deltaTime);
            _pivot.localRotation = Quaternion.Euler(_angle, 0f, 0f);
        }
    }
}
