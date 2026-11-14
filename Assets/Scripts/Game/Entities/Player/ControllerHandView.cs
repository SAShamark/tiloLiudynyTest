using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace Game.Entities.Player
{
    /// <summary>
    /// Physical controller hands. Each hand is a rigidbody that chases its controller with velocities, so it
    /// stops against tables and walls and can nudge loose objects. While an item is held the hand locks onto
    /// it at the grab offset and the fingers close around the item's colliders instead of making a fist.
    /// </summary>
    public sealed class ControllerHandView : MonoBehaviour
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

        private Hand _left;
        private Hand _right;
        private Collider[] _rigColliders = Array.Empty<Collider>();

        public void Init(Transform trackingSpace)
        {
            var origin = trackingSpace.GetComponentInParent<Unity.XR.CoreUtils.XROrigin>();
            if (origin != null)
                _rigColliders = origin.GetComponentsInChildren<Collider>(true);

            _left = CreateHand(trackingSpace, "Left", _leftModel, XRNode.LeftHand);
            _right = CreateHand(trackingSpace, "Right", _rightModel, XRNode.RightHand);
            if (_left != null && _right != null)
                _left.IgnoreOtherHand(_right);

            Application.onBeforeRender += HandleBeforeRender;
        }

        /// <summary>Wrist bone of the visible physical hand, or null while that hand is not tracked.</summary>
        public Transform GetWrist(bool left)
        {
            Hand hand = left ? _left : _right;
            return hand != null && hand.IsVisible ? hand.Wrist : null;
        }

        private void OnDestroy()
        {
            Application.onBeforeRender -= HandleBeforeRender;
            _left?.Dispose();
            _right?.Dispose();
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

            // Physics body lives at the scene root so the rig's own motion never drags it through walls.
            var body = new GameObject(side + " Physics Hand");
            body.layer = _handLayer;
            body.transform.SetPositionAndRotation(controller.position, controller.rotation);
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

            if (_material != null)
            {
                foreach (var renderer in visual.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    var slots = renderer.sharedMaterials;
                    Array.Fill(slots, _material);
                    renderer.sharedMaterials = slots;
                    renderer.updateWhenOffscreen = true;
                }
            }

            var colliders = new List<Collider>();
            colliders.Add(CreatePalm(body.transform, wrist, index, little, middle));

            var fingers = new List<Finger>();
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
                var finger = new Finger(joints.ToArray(), tip, controller.right, name == "Thumb", name == "Index");
                fingers.Add(finger);
                colliders.AddRange(finger.CreateColliders(_fingerRadius, _handLayer));
            }

            foreach (var hand in colliders)
            foreach (var rigCollider in _rigColliders)
            {
                if (rigCollider != null)
                    Physics.IgnoreCollision(hand, rigCollider, true);
            }

            controllerVisual.gameObject.SetActive(false);
            var interactors = controller.GetComponentsInChildren<XRBaseInteractor>(true);
            return new Hand(this, node, controller, rigidbody, visual.gameObject, fingers.ToArray(), colliders.ToArray(), interactors) { Wrist = wrist };
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
            _left?.FixedTick(Time.fixedDeltaTime);
            _right?.FixedTick(Time.fixedDeltaTime);
        }

        private void LateUpdate()
        {
            _left?.LateTick(Time.unscaledDeltaTime);
            _right?.LateTick(Time.unscaledDeltaTime);
        }

        private void HandleBeforeRender()
        {
            _left?.FollowHeldItem();
            _right?.FollowHeldItem();
        }

        private sealed class Hand
        {
            private readonly ControllerHandView _owner;
            private readonly XRNode _node;
            private readonly Transform _controller;
            private readonly Rigidbody _body;
            private readonly GameObject _visual;
            private readonly Finger[] _fingers;
            private readonly Collider[] _colliders;
            private readonly XRBaseInteractor[] _interactors;
            private readonly List<InputDevice> _devices = new List<InputDevice>();
            private InputDevice _device;
            private float _grip;
            private float _trigger;

            private XRGrabInteractable _held;
            private Collider[] _heldColliders = Array.Empty<Collider>();
            private Vector3 _heldLocalPosition;
            private Quaternion _heldLocalRotation;
            private Collider[] _releasedColliders = Array.Empty<Collider>();
            private float _releaseTimer;
            private bool _locked;
            private const float LockDistance = 0.08f;

            public Transform Wrist { get; set; }
            public bool IsVisible => _visual != null && _visual.activeInHierarchy;

            public Hand(ControllerHandView owner, XRNode node, Transform controller, Rigidbody body, GameObject visual,
                Finger[] fingers, Collider[] colliders, XRBaseInteractor[] interactors)
            {
                _owner = owner;
                _node = node;
                _controller = controller;
                _body = body;
                _visual = visual;
                _fingers = fingers;
                _colliders = colliders;
                _interactors = interactors;
                foreach (var interactor in _interactors)
                {
                    interactor.selectEntered.AddListener(HandleSelectEntered);
                    interactor.selectExited.AddListener(HandleSelectExited);
                }
            }

            public void Dispose()
            {
                foreach (var interactor in _interactors)
                {
                    if (interactor == null)
                        continue;
                    interactor.selectEntered.RemoveListener(HandleSelectEntered);
                    interactor.selectExited.RemoveListener(HandleSelectExited);
                }

                if (_body != null)
                    Destroy(_body.gameObject);
            }

            public void IgnoreOtherHand(Hand other)
            {
                foreach (var mine in _colliders)
                foreach (var theirs in other._colliders)
                    Physics.IgnoreCollision(mine, theirs, true);
            }

            // Chase the controller with velocities so the solver stops the hand at obstacles.
            public void FixedTick(float deltaTime)
            {
                if (_locked || !_visual.activeSelf)
                    return;

                Vector3 target = _controller.position;
                Quaternion targetRotation = _controller.rotation;
                if ((target - _body.position).sqrMagnitude > _owner._snapDistance * _owner._snapDistance)
                {
                    _body.position = target;
                    _body.rotation = targetRotation;
                    _body.linearVelocity = Vector3.zero;
                    _body.angularVelocity = Vector3.zero;
                    return;
                }

                float strength = _owner._followStrength;
                _body.linearVelocity = (target - _body.position) * (strength / deltaTime);

                Quaternion delta = targetRotation * Quaternion.Inverse(_body.rotation);
                delta.ToAngleAxis(out float angle, out Vector3 axis);
                if (angle > 180f)
                    angle -= 360f;
                if (Mathf.Abs(angle) > 0.01f && !float.IsNaN(axis.x))
                    _body.angularVelocity = axis * (angle * Mathf.Deg2Rad * strength / deltaTime);
                else
                    _body.angularVelocity = Vector3.zero;
            }

            public void LateTick(float deltaTime)
            {
                UpdateDevice();
                bool tracked = _device.isValid &&
                    _device.TryGetFeatureValue(CommonUsages.isTracked, out bool isTracked) && isTracked;
                if (_visual.activeSelf != tracked)
                {
                    _visual.SetActive(tracked);
                    SetCollidersEnabled(tracked);
                    if (tracked)
                        _body.position = _controller.position;
                }

                if (_releaseTimer > 0f)
                {
                    _releaseTimer -= deltaTime;
                    if (_releaseTimer <= 0f)
                        SetIgnore(_releasedColliders, false);
                }

                if (!tracked)
                    return;

                _device.TryGetFeatureValue(CommonUsages.grip, out float grip);
                _device.TryGetFeatureValue(CommonUsages.trigger, out float trigger);
                float blend = 1f - Mathf.Exp(-20f * deltaTime);
                _grip = Mathf.Lerp(_grip, grip, blend);
                _trigger = Mathf.Lerp(_trigger, trigger, blend);

                TryLock();
                foreach (var finger in _fingers)
                    finger.Tick(_locked, _grip, _trigger, blend);

                FollowHeldItem();
            }

            public void FollowHeldItem()
            {
                if (_held == null || !_locked)
                    return;

                Transform item = _held.transform;
                _body.position = item.TransformPoint(_heldLocalPosition);
                _body.rotation = item.rotation * _heldLocalRotation;
                _body.transform.SetPositionAndRotation(_body.position, _body.rotation);
            }

            private void HandleSelectEntered(SelectEnterEventArgs args)
            {
                if (args.interactableObject is not XRGrabInteractable grab)
                    return;

                _held = grab;
                _heldColliders = grab.colliders.ToArray();
                SetIgnore(_heldColliders, true);
                _locked = false;
                TryLock();
            }

            // A far grab pulls the item in over several frames: the hand keeps following the controller until
            // the item is actually in the palm, and only then locks onto it and closes the fingers.
            private void TryLock()
            {
                if (_held == null || _locked || DistanceToItem() > LockDistance)
                    return;

                Transform item = _held.transform;
                _locked = true;
                _body.isKinematic = true;
                _heldLocalPosition = item.InverseTransformPoint(_body.position);
                _heldLocalRotation = Quaternion.Inverse(item.rotation) * _body.rotation;
                foreach (var finger in _fingers)
                    finger.FitTo(_heldColliders, _owner._fingerRadius);
            }

            private float DistanceToItem()
            {
                Vector3 palm = _colliders.Length > 0 ? _colliders[0].bounds.center : _body.position;
                float best = float.MaxValue;
                foreach (var collider in _heldColliders)
                {
                    if (collider == null || !collider.enabled)
                        continue;
                    Vector3 point = collider is MeshCollider { convex: false } ? collider.bounds.ClosestPoint(palm) : collider.ClosestPoint(palm);
                    best = Mathf.Min(best, Vector3.Distance(point, palm));
                }

                return best;
            }

            private void HandleSelectExited(SelectExitEventArgs args)
            {
                if (args.interactableObject is not XRGrabInteractable grab || grab != _held)
                    return;

                _held = null;
                _locked = false;
                _body.isKinematic = false;
                _releasedColliders = _heldColliders;
                _heldColliders = Array.Empty<Collider>();
                _releaseTimer = _owner._releaseGrace;
            }

            private void SetIgnore(Collider[] others, bool ignore)
            {
                foreach (var hand in _colliders)
                foreach (var other in others)
                {
                    if (hand != null && other != null)
                        Physics.IgnoreCollision(hand, other, ignore);
                }
            }

            private void SetCollidersEnabled(bool enabled)
            {
                foreach (var collider in _colliders)
                    collider.enabled = enabled;
            }

            private void UpdateDevice()
            {
                if (_device.isValid)
                    return;

                var side = _node == XRNode.LeftHand ? InputDeviceCharacteristics.Left : InputDeviceCharacteristics.Right;
                InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.Controller | side, _devices);
                if (_devices.Count > 0)
                    _device = _devices[0];
            }
        }

        /// <summary>One finger: curls all its joints together and can fit that curl to an object's surface.</summary>
        private sealed class Finger
        {
            private const int FitSteps = 24;

            private readonly Transform[] _joints;
            private readonly Quaternion[] _rest;
            private readonly Vector3[] _axes;
            private readonly float[] _maxCurl;
            private readonly Transform _tip;
            private readonly bool _isIndex;
            private float _amount;
            private float _fitted = 1f;

            public Finger(Transform[] joints, Transform tip, Vector3 curlAxisWorld, bool isThumb, bool isIndex)
            {
                _joints = joints;
                _tip = tip;
                _isIndex = isIndex;
                _rest = new Quaternion[joints.Length];
                _axes = new Vector3[joints.Length];
                _maxCurl = new float[joints.Length];
                for (int i = 0; i < joints.Length; i++)
                {
                    _rest[i] = joints[i].localRotation;
                    _axes[i] = joints[i].InverseTransformDirection(curlAxisWorld);
                    _maxCurl[i] = isThumb ? 28f : i == 0 ? 70f : 78f;
                }
            }

            public IEnumerable<Collider> CreateColliders(float radius, int layer)
            {
                for (int i = 0; i < _joints.Length; i++)
                {
                    Vector3 end = i + 1 < _joints.Length ? _joints[i + 1].position : TipPosition();
                    var holder = new GameObject(_joints[i].name + " collider");
                    holder.layer = layer;
                    holder.transform.SetParent(_joints[i], false);
                    var capsule = holder.AddComponent<CapsuleCollider>();
                    Vector3 localEnd = _joints[i].InverseTransformPoint(end);
                    float length = localEnd.magnitude;
                    capsule.direction = DominantAxis(localEnd);
                    capsule.center = localEnd * 0.5f;
                    float scale = _joints[i].lossyScale.x;
                    capsule.radius = radius / Mathf.Max(scale, 0.0001f);
                    capsule.height = length + capsule.radius * 2f;
                    yield return capsule;
                }
            }

            /// <summary>Finds the curl at which this finger first touches the item, so it rests on the surface.</summary>
            public void FitTo(Collider[] item, float radius)
            {
                _fitted = 1f;
                for (int step = 0; step <= FitSteps; step++)
                {
                    float amount = (float)step / FitSteps;
                    Pose(amount);
                    if (!Touches(item, radius))
                        continue;

                    _fitted = Mathf.Max(0f, amount - 0.5f / FitSteps);
                    break;
                }

                Pose(_amount);
            }

            public void Tick(bool holding, float grip, float trigger, float blend)
            {
                float target = holding ? _fitted : _isIndex ? Mathf.Max(grip, trigger) : grip;
                _amount = Mathf.Lerp(_amount, target, holding ? Mathf.Max(blend, 0.35f) : blend);
                Pose(_amount);
            }

            private void Pose(float amount)
            {
                for (int i = 0; i < _joints.Length; i++)
                    _joints[i].localRotation = _rest[i] * Quaternion.AngleAxis(_maxCurl[i] * amount, _axes[i]);
            }

            private bool Touches(Collider[] item, float radius)
            {
                for (int i = 0; i < _joints.Length; i++)
                {
                    Vector3 start = _joints[i].position;
                    Vector3 end = i + 1 < _joints.Length ? _joints[i + 1].position : TipPosition();
                    for (int s = 1; s <= 2; s++)
                    {
                        Vector3 point = Vector3.Lerp(start, end, s * 0.5f);
                        foreach (var collider in item)
                        {
                            if (collider == null || !collider.enabled || collider.isTrigger || !SupportsClosestPoint(collider))
                                continue;
                            if ((collider.ClosestPoint(point) - point).sqrMagnitude <= radius * radius)
                                return true;
                        }
                    }
                }

                return false;
            }

            private Vector3 TipPosition()
            {
                if (_tip != null)
                    return _tip.position;

                int last = _joints.Length - 1;
                Vector3 previous = last > 0 ? _joints[last - 1].position : _joints[last].parent.position;
                return _joints[last].position + (_joints[last].position - previous) * 0.75f;
            }

            private static bool SupportsClosestPoint(Collider collider) =>
                collider is not MeshCollider mesh || mesh.convex;

            private static int DominantAxis(Vector3 v)
            {
                Vector3 a = new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));
                return a.x >= a.y && a.x >= a.z ? 0 : a.y >= a.z ? 1 : 2;
            }
        }
    }
}
