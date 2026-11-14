using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace Game.Entities.Player
{
    public sealed class ControllerHandView : MonoBehaviour
    {
        [SerializeField] private GameObject _leftModel;
        [SerializeField] private GameObject _rightModel;
        [SerializeField] private Material _material;
        [SerializeField] private Vector3 _wristOffset = new Vector3(0f, -0.015f, -0.07f);

        private Hand _left;
        private Hand _right;

        public void Init(Transform trackingSpace)
        {
            _left = CreateHand(trackingSpace, "Left", _leftModel, XRNode.LeftHand);
            _right = CreateHand(trackingSpace, "Right", _rightModel, XRNode.RightHand);
        }

        private Hand CreateHand(Transform trackingSpace, string side, GameObject model, XRNode node)
        {
            Transform controller = trackingSpace.Find(side + " Controller");
            Transform controllerVisual = controller == null ? null : controller.Find(side + " Controller Visual");
            if (controllerVisual == null || model == null)
            {
                Debug.LogWarning($"Missing {side.ToLowerInvariant()} controller or hand model.", this);
                return null;
            }

            Transform visual = Instantiate(model, controller, false).transform;
            visual.name = side + " Controller Hand";
            var bones = new Dictionary<string, Transform>(StringComparer.Ordinal);
            string prefix = side[0] + "_";
            foreach (Transform bone in visual.GetComponentsInChildren<Transform>(true))
            {
                if (bone.name.StartsWith(prefix, StringComparison.Ordinal))
                    bones[bone.name.Substring(prefix.Length)] = bone;
            }

            if (!bones.TryGetValue("Wrist", out Transform wrist) ||
                !bones.TryGetValue("MiddleProximal", out Transform middle) ||
                !bones.TryGetValue("IndexProximal", out Transform index) ||
                !bones.TryGetValue("LittleProximal", out Transform little))
            {
                Debug.LogWarning($"{visual.name} is missing the expected hand bones.", this);
                Destroy(visual.gameObject);
                return null;
            }

            Vector3 forward = visual.InverseTransformDirection(middle.position - wrist.position);
            Vector3 across = visual.InverseTransformDirection(index.position - little.position);
            Vector3 up = Vector3.Cross(forward, across) * (node == XRNode.LeftHand ? 1f : -1f);
            Vector3 wristPosition = visual.InverseTransformPoint(wrist.position);
            visual.localRotation = Quaternion.Inverse(Quaternion.LookRotation(forward, up));
            visual.localPosition = _wristOffset - visual.localRotation * Vector3.Scale(wristPosition, visual.localScale);

            if (_material != null)
            {
                foreach (var renderer in visual.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    var slots = renderer.sharedMaterials;
                    Array.Fill(slots, _material);
                    renderer.sharedMaterials = slots;
                }
            }

            var joints = new List<FingerJoint>();
            foreach (string finger in new[] { "Index", "Middle", "Ring", "Little", "Thumb" })
            {
                foreach (string segment in new[] { "Proximal", "Intermediate", "Distal" })
                {
                    if (bones.TryGetValue(finger + segment, out Transform joint))
                    {
                        Vector3 axis = joint.InverseTransformDirection(controller.right);
                        float curl = finger == "Thumb" ? 22f : segment == "Proximal" ? 65f : 72f;
                        joints.Add(new FingerJoint(joint, joint.localRotation, axis, curl, finger == "Index"));
                    }
                }
            }

            controllerVisual.gameObject.SetActive(false);
            visual.gameObject.SetActive(false);
            return new Hand(node, visual.gameObject, joints.ToArray());
        }

        private void LateUpdate()
        {
            _left?.Update();
            _right?.Update();
        }

        private sealed class Hand
        {
            private readonly XRNode _node;
            private readonly GameObject _visual;
            private readonly FingerJoint[] _joints;
            private readonly List<InputDevice> _devices = new List<InputDevice>();
            private InputDevice _device;
            private float _grip;
            private float _trigger;

            public Hand(XRNode node, GameObject visual, FingerJoint[] joints)
            {
                _node = node;
                _visual = visual;
                _joints = joints;
            }

            public void Update()
            {
                if (!_device.isValid)
                {
                    var side = _node == XRNode.LeftHand
                        ? InputDeviceCharacteristics.Left
                        : InputDeviceCharacteristics.Right;
                    InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.Controller | side, _devices);
                    if (_devices.Count > 0)
                        _device = _devices[0];
                }

                bool tracked = _device.isValid &&
                    _device.TryGetFeatureValue(CommonUsages.isTracked, out bool isTracked) && isTracked;
                if (_visual.activeSelf != tracked)
                    _visual.SetActive(tracked);
                if (!tracked || !_visual.activeInHierarchy)
                    return;

                _device.TryGetFeatureValue(CommonUsages.grip, out float grip);
                _device.TryGetFeatureValue(CommonUsages.trigger, out float trigger);
                float blend = 1f - Mathf.Exp(-20f * Time.unscaledDeltaTime);
                _grip = Mathf.Lerp(_grip, grip, blend);
                _trigger = Mathf.Lerp(_trigger, trigger, blend);
                foreach (var joint in _joints)
                    joint.Apply(_grip, _trigger);
            }
        }

        private readonly struct FingerJoint
        {
            private readonly Transform _bone;
            private readonly Quaternion _rest;
            private readonly Vector3 _axis;
            private readonly float _curl;
            private readonly bool _isIndex;

            public FingerJoint(Transform bone, Quaternion rest, Vector3 axis, float curl, bool isIndex)
            {
                _bone = bone;
                _rest = rest;
                _axis = axis;
                _curl = curl;
                _isIndex = isIndex;
            }

            public void Apply(float grip, float trigger)
            {
                float amount = _isIndex ? Mathf.Max(grip, trigger) : grip;
                _bone.localRotation = _rest * Quaternion.AngleAxis(_curl * amount, _axis);
            }
        }
    }
}
