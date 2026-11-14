using UnityEngine;

namespace Game.Entities.Player
{
    /// <summary>
    /// Simple self-avatar: torso and legs that stand under the headset, turn with the head when it turns far
    /// enough and play the walk clip while the player moves. The arms reach the physical hands with two-bone IK;
    /// the model's own hands and head are hidden so they never double up or block the view. Casts a shadow.
    /// </summary>
    [DefaultExecutionOrder(100)] // after the physical hands have moved this frame
    public sealed class PlayerBodyView : MonoBehaviour
    {
        private static readonly int MoveXParameter = Animator.StringToHash("MoveX");
        private static readonly int MoveZParameter = Animator.StringToHash("MoveZ");
        private static readonly int WalkRateParameter = Animator.StringToHash("WalkRate");

        [SerializeField] private Animator _animator;
        [Tooltip("How far the body stands behind the eyes, so looking down shows the chest and feet, not the inside.")]
        [SerializeField, Min(0f)] private float _backOffset = 0.13f;
        [Tooltip("Head yaw away from the body (degrees) that makes the body turn to catch up.")]
        [SerializeField, Range(10f, 120f)] private float _turnThreshold = 45f;
        [SerializeField, Min(0.5f)] private float _turnSpeed = 6f;
        [Tooltip("Walking speed (m/s) at which the walk clip plays at normal speed.")]
        [SerializeField, Min(0.1f)] private float _clipWalkSpeed = 1.1f;
        [Tooltip("Speeds (m/s) at which the strafe and backward clips play at normal speed.")]
        [SerializeField, Min(0.1f)] private float _clipStrafeSpeed = 0.9f;
        [SerializeField, Min(0.1f)] private float _clipBackSpeed = 0.9f;
        [Tooltip("Eye height of the model at scale 1, used to fit the body to the player's height.")]
        [SerializeField, Min(0.5f)] private float _modelEyeHeight = 1.62f;
        [Tooltip("Below this share of the standing eye height (crouching) the body is hidden to keep it out of the face.")]
        [SerializeField, Range(0.3f, 0.95f)] private float _hideBelow = 0.72f;

        private PlayerRig _rig;
        private Transform _floor;
        private Transform[] _hiddenBones;
        private Renderer[] _renderers;
        private Vector3 _lastHead;
        private float _speed;
        private Vector2 _move;
        private float _bodyYaw;
        private float _standingEye;
        private float _lastEye;
        private bool _visible = true;
        private bool _shown = true;
        private ControllerHandView _hands;
        private ArmChain _leftArm;
        private ArmChain _rightArm;

        private void OnDestroy() => Application.onBeforeRender -= SolveArms;

        // Runs after the hands have taken their final pose for the frame, so the forearms meet the wrists exactly.
        private void SolveArms()
        {
            if (_rig == null || _hands == null || !_shown)
                return;

            Quaternion body = Quaternion.Euler(0f, _bodyYaw, 0f);
            _leftArm.Solve(_hands.GetWrist(true), body);
            _rightArm.Solve(_hands.GetWrist(false), body);
        }

        /// <summary>Analytic two-bone IK for one arm, with the elbow pointing down, out and slightly back.</summary>
        private sealed class ArmChain
        {
            private readonly Transform _shoulder;
            private readonly Transform _upper;
            private readonly Transform _lower;
            private readonly Transform _hand;
            private readonly float _side;

            public ArmChain(Animator animator, HumanBodyBones upper, HumanBodyBones lower, HumanBodyBones hand, float side)
            {
                _shoulder = animator.GetBoneTransform(side < 0f ? HumanBodyBones.LeftShoulder : HumanBodyBones.RightShoulder);
                _upper = animator.GetBoneTransform(upper);
                _lower = animator.GetBoneTransform(lower);
                _hand = animator.GetBoneTransform(hand);
                _side = side;
            }

            public void Solve(Transform target, Quaternion body)
            {
                if (target == null || _upper == null || _lower == null || _hand == null)
                    return;

                float upperLength = Vector3.Distance(_upper.position, _lower.position);
                float lowerLength = Vector3.Distance(_lower.position, _hand.position);
                float reach = upperLength + lowerLength;

                // Out of reach: swing the collarbone toward the hand first, like a real shoulder does.
                if (_shoulder != null)
                {
                    float shortfall = Vector3.Distance(_upper.position, target.position) - reach * 0.92f;
                    if (shortfall > 0f)
                    {
                        Vector3 current = _upper.position - _shoulder.position;
                        Vector3 wanted = target.position - _shoulder.position;
                        float t = Mathf.Clamp01(shortfall / 0.12f);
                        Quaternion swing = Quaternion.FromToRotation(current, Vector3.Slerp(current.normalized, wanted.normalized, 0.35f * t));
                        _shoulder.rotation = Quaternion.RotateTowards(Quaternion.identity, swing, 25f) * _shoulder.rotation;
                    }
                }

                Vector3 shoulder = _upper.position;
                Vector3 toTarget = target.position - shoulder;
                float distance = Mathf.Clamp(toTarget.magnitude, 0.02f, (upperLength + lowerLength) * 0.999f);
                Vector3 direction = toTarget.normalized;

                Vector3 hint = body * new Vector3(_side * 0.35f, -0.6f, -0.25f);
                Vector3 bend = Vector3.ProjectOnPlane(hint, direction);
                if (bend.sqrMagnitude < 0.0001f)
                    bend = Vector3.ProjectOnPlane(body * Vector3.down, direction);
                bend.Normalize();

                float cos = Mathf.Clamp((upperLength * upperLength + distance * distance - lowerLength * lowerLength)
                    / (2f * upperLength * distance), -1f, 1f);
                float sin = Mathf.Sqrt(1f - cos * cos);
                Vector3 elbow = shoulder + direction * (upperLength * cos) + bend * (upperLength * sin);

                _upper.rotation = Quaternion.FromToRotation(_lower.position - shoulder, elbow - shoulder) * _upper.rotation;
                Vector3 wristGoal = shoulder + direction * distance;
                _lower.rotation = Quaternion.FromToRotation(_hand.position - _lower.position, wristGoal - _lower.position) * _lower.rotation;
            }
        }

        public void Init(PlayerRig rig, ControllerHandView hands)
        {
            _rig = rig;
            _hands = hands;
            _leftArm = new ArmChain(_animator, HumanBodyBones.LeftUpperArm, HumanBodyBones.LeftLowerArm, HumanBodyBones.LeftHand, -1f);
            _rightArm = new ArmChain(_animator, HumanBodyBones.RightUpperArm, HumanBodyBones.RightLowerArm, HumanBodyBones.RightHand, 1f);
            Application.onBeforeRender += SolveArms;
            _floor = rig.TrackingSpace.parent != null ? rig.TrackingSpace.parent : rig.TrackingSpace;
            _renderers = GetComponentsInChildren<Renderer>(true);
            foreach (var renderer in _renderers)
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;

            _hiddenBones = new[]
            {
                _animator.GetBoneTransform(HumanBodyBones.Neck) ?? _animator.GetBoneTransform(HumanBodyBones.Head),
                // The model's own hands are hidden: the forearm ends at the wrist where the physical hand begins.
                _animator.GetBoneTransform(HumanBodyBones.LeftHand),
                _animator.GetBoneTransform(HumanBodyBones.RightHand),
            };

            _lastHead = rig.Head.position;
            _bodyYaw = Yaw(rig.FlatForward);
            SnapToHead();
        }

        public void SetVisible(bool visible)
        {
            _visible = visible;
            ApplyShown();
        }

        private void LateUpdate()
        {
            if (_rig == null)
                return;

            Transform head = _rig.Head;
            float deltaTime = Mathf.Max(Time.deltaTime, 0.0001f);

            Vector3 moved = head.position - _lastHead;
            _lastHead = head.position;
            moved.y = 0f;
            float rawSpeed = moved.magnitude / deltaTime;
            if (rawSpeed > 6f)
                rawSpeed = 0f; // teleport or recenter, not walking
            _speed = Mathf.Lerp(_speed, rawSpeed, 1f - Mathf.Exp(-8f * deltaTime));

            float eye = head.position.y - _floor.position.y;
            // A sudden jump in eye height is a height-setting change or recenter, not crouching: start over.
            if (_standingEye <= 0f || Mathf.Abs(eye - _lastEye) > 0.15f)
                _standingEye = eye;
            else if (eye > _standingEye)
                _standingEye = Mathf.Lerp(_standingEye, eye, 0.02f);
            else
                _standingEye = Mathf.MoveTowards(_standingEye, eye, 0.03f * deltaTime);
            _lastEye = eye;
            FitHeight();
            ApplyShown(eye);

            UpdateYaw(deltaTime, moved);
            SnapToHead();

            // Direction of travel relative to the torso picks the clip: forward, backward, left or right strafe
            // (and blends between them for diagonals). Speed beyond the clips' own pace speeds the cycle up.
            Vector3 velocity = rawSpeed > 0f ? moved / deltaTime : Vector3.zero;
            Vector3 local = Quaternion.Inverse(Quaternion.Euler(0f, _bodyYaw, 0f)) * velocity;
            var move = new Vector2(local.x / _clipStrafeSpeed, local.z / (local.z >= 0f ? _clipWalkSpeed : _clipBackSpeed));
            _move = Vector2.Lerp(_move, move, 1f - Mathf.Exp(-8f * deltaTime));
            float pace = _move.magnitude;
            Vector2 blend = pace > 1f ? _move / pace : _move;
            if (pace < 0.08f)
                blend = Vector2.zero;
            _animator.SetFloat(MoveXParameter, blend.x);
            _animator.SetFloat(MoveZParameter, blend.y);
            _animator.SetFloat(WalkRateParameter, Mathf.Clamp(pace, 1f, 1.8f));

            foreach (var bone in _hiddenBones)
            {
                if (bone != null)
                    bone.localScale = Vector3.one * 0.0001f;
            }

            SolveArms();
        }

        private void UpdateYaw(float deltaTime, Vector3 moved)
        {
            float headYaw = Yaw(_rig.FlatForward);
            float difference = Mathf.DeltaAngle(_bodyYaw, headYaw);
            bool walking = _speed > 0.2f;
            if (walking || Mathf.Abs(difference) > _turnThreshold)
                _bodyYaw = Mathf.LerpAngle(_bodyYaw, headYaw, 1f - Mathf.Exp(-_turnSpeed * deltaTime));
        }

        private void SnapToHead()
        {
            Vector3 head = _rig.Head.position;
            Quaternion rotation = Quaternion.Euler(0f, _bodyYaw, 0f);
            Vector3 position = new Vector3(head.x, _floor.position.y, head.z) - rotation * Vector3.forward * _backOffset;
            transform.SetPositionAndRotation(position, rotation);
        }

        private void FitHeight()
        {
            if (_standingEye < 1f)
                return;

            float scale = Mathf.Clamp(_standingEye / _modelEyeHeight, 0.8f, 1.2f);
            transform.localScale = Vector3.one * scale;
        }

        private void ApplyShown(float eye = -1f)
        {
            bool crouching = eye > 0f && _standingEye > 1f && eye < _standingEye * _hideBelow;
            bool shown = _visible && !crouching;
            if (shown == _shown)
                return;

            _shown = shown;
            foreach (var renderer in _renderers)
                renderer.enabled = shown;
        }

        private static float Yaw(Vector3 forward) => Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
    }
}
