using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace Gameplay.Entities.Player.Hands
{
    public sealed class PhysicalHandsControl : MonoBehaviour
    {
        [SerializeField] private GameObject _leftModel;
        [SerializeField] private GameObject _rightModel;
        [SerializeField] private Material _material;
        [SerializeField] private Vector3 _wristOffset = new Vector3(0f, -0.015f, -0.07f);

        [Header("Physics")]
        [Tooltip("Layer for the hand colliders; Ignore Raycast keeps them out of UI and teleport rays.")]
        [SerializeField] private int _handLayer = 2;
        [SerializeField, Range(0.1f, 1f)] private float _followStrength = 0.9f;
        [Tooltip("If the physical hand ends up this far from the controller it snaps back.")]
        [SerializeField, Min(0.05f)] private float _snapDistance = 0.3f;
        [SerializeField, Min(0.003f)] private float _fingerRadius = 0.009f;
        [Tooltip("Keeps hand colliders off a released item briefly so it is not flicked away.")]
        [SerializeField, Min(0f)] private float _releaseGrace = 0.35f;

        private PhysicalHand _left;
        private PhysicalHand _right;
        private Collider[] _rigColliders = Array.Empty<Collider>();

        private PhysicalHandSettings Settings => new(_followStrength, _snapDistance, _fingerRadius, _releaseGrace);

        public void Initialize(Transform trackingSpace)
        {
            var origin = trackingSpace.GetComponentInParent<Unity.XR.CoreUtils.XROrigin>();
            _rigColliders = origin.GetComponentsInChildren<Collider>(true);

            _left = CreateHand(trackingSpace, "Left", _leftModel, XRNode.LeftHand);
            _right = CreateHand(trackingSpace, "Right", _rightModel, XRNode.RightHand);
            _left.IgnoreOtherHand(_right);

            Application.onBeforeRender += HandleBeforeRender;
        }

        public Transform GetWrist(bool left)
        {
            PhysicalHand hand = left ? _left : _right;
            return hand.IsVisible ? hand.Wrist : null;
        }

        private void OnDestroy()
        {
            Application.onBeforeRender -= HandleBeforeRender;
            _left.Dispose();
            _right.Dispose();
        }

        private PhysicalHand CreateHand(Transform trackingSpace, string side, GameObject model, XRNode node)
        {
            Transform controller = trackingSpace.Find(side + " Controller");
            Transform controllerVisual = controller.Find(side + " Controller Visual");

            var body = new GameObject(side + " Physics Hand");
            body.layer = _handLayer;
            body.transform.SetPositionAndRotation(controller.position, controller.rotation);
            body.AddComponent<PhysicalHandBody>();
            var rigidbody = body.AddComponent<Rigidbody>();
            rigidbody.useGravity = false;
            rigidbody.mass = 1f;
            rigidbody.linearDamping = 0f;
            rigidbody.angularDamping = 0f;
            rigidbody.maxAngularVelocity = 50f;
            rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            Transform visual = Instantiate(model, body.transform, false).transform;
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
                Destroy(body);
                return null;
            }

            Vector3 forward = visual.InverseTransformDirection(middle.position - wrist.position);
            Vector3 across = visual.InverseTransformDirection(index.position - little.position);
            Vector3 up = Vector3.Cross(forward, across) * (node == XRNode.LeftHand ? 1f : -1f);
            Vector3 wristPosition = visual.InverseTransformPoint(wrist.position);
            visual.localRotation = Quaternion.Inverse(Quaternion.LookRotation(forward, up));
            visual.localPosition = _wristOffset - visual.localRotation * Vector3.Scale(wristPosition, visual.localScale);

            foreach (var renderer in visual.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                var slots = renderer.sharedMaterials;
                Array.Fill(slots, _material);
                renderer.sharedMaterials = slots;
                renderer.updateWhenOffscreen = true;
            }


            var colliders = new List<Collider>();
            colliders.Add(CreatePalm(body.transform, wrist, index, little, middle));

            var fingers = new List<HandFinger>();
            foreach (string name in new[] { "Index", "Middle", "Ring", "Little", "Thumb" })
            {
                var joints = new List<Transform>();
                foreach (string segment in new[] { "Metacarpal", "Proximal", "Intermediate", "Distal" })
                {
                    if (bones.TryGetValue(name + segment, out Transform joint) && segment != "Metacarpal")
                        joints.Add(joint);
                }

                if (joints.Count == 0)
                    continue;

                bones.TryGetValue(name + "Tip", out Transform tip);
                var finger = new HandFinger(joints.ToArray(), tip, controller.right, name == "Thumb", name == "Index");
                fingers.Add(finger);
                colliders.AddRange(finger.CreateColliders(_fingerRadius, _handLayer));
            }

            foreach (var hand in colliders)
            foreach (var rigCollider in _rigColliders)
            {
                Physics.IgnoreCollision(hand, rigCollider, true);
            }

            controllerVisual.gameObject.SetActive(false);
            var interactors = controller.GetComponentsInChildren<XRBaseInteractor>(true);
            return new PhysicalHand(Settings, node, controller, rigidbody, visual.gameObject, fingers.ToArray(), colliders.ToArray(), interactors) { Wrist = wrist };
        }

        private Collider CreatePalm(Transform body, Transform wrist, Transform index, Transform little, Transform middle)
        {
            var palm = new GameObject("Palm collider");
            palm.layer = _handLayer;
            palm.transform.SetParent(body, false);
            Vector3 centre = (wrist.position + index.position + little.position + middle.position) * 0.25f;
            palm.transform.SetPositionAndRotation(centre, Quaternion.LookRotation(middle.position - wrist.position, Vector3.Cross(middle.position - wrist.position, index.position - little.position)));
            var box = palm.AddComponent<BoxCollider>();
            box.size = new Vector3(Vector3.Distance(index.position, little.position) + 0.012f, 0.028f, Vector3.Distance(wrist.position, middle.position) + 0.01f);
            return box;
        }

        private void FixedUpdate()
        {
            _left.FixedTick(Time.fixedDeltaTime);
            _right.FixedTick(Time.fixedDeltaTime);
        }

        private void LateUpdate()
        {
            _left.LateTick(Time.unscaledDeltaTime);
            _right.LateTick(Time.unscaledDeltaTime);
        }

        private void HandleBeforeRender()
        {
            _left.FollowHeldItem();
            _right.FollowHeldItem();
        }
    }
}
