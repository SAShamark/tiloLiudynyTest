using Gameplay.Entities.Player.Hands;
using UnityEngine;

namespace Gameplay.Entities.Player.Body
{
    [DefaultExecutionOrder(100)]
    public sealed class PlayerBodyView : MonoBehaviour
    {
        private static readonly int MoveXParameter = Animator.StringToHash("MoveX");
        private static readonly int MoveZParameter = Animator.StringToHash("MoveZ");
        private static readonly int WalkRateParameter = Animator.StringToHash("WalkRate");

        [SerializeField] private Animator _animator;
        [SerializeField, Min(0f)] private float _backOffset = 0.13f;
        [SerializeField, Range(10f, 120f)] private float _turnThreshold = 45f;
        [SerializeField, Min(0.5f)] private float _turnSpeed = 6f;
        [SerializeField, Min(0.1f)] private float _clipWalkSpeed = 1.1f;
        [SerializeField, Min(0.1f)] private float _clipStrafeSpeed = 0.9f;
        [SerializeField, Min(0.1f)] private float _clipBackSpeed = 0.9f;
        [SerializeField, Min(0.5f)] private float _modelEyeHeight = 1.62f;
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
        private PhysicalHandsControl _hands;
        private TwoBoneArmIK _leftArm;
        private TwoBoneArmIK _rightArm;

        public void Initialize(PlayerRig rig, PhysicalHandsControl hands)
        {
            _rig = rig;
            _hands = hands;
            _floor = rig.TrackingSpace.parent;
            _renderers = GetComponentsInChildren<Renderer>(true);
            foreach (var renderer in _renderers)
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;

            _hiddenBones = new[]
            {
                _animator.GetBoneTransform(HumanBodyBones.Neck),
                _animator.GetBoneTransform(HumanBodyBones.LeftHand),
                _animator.GetBoneTransform(HumanBodyBones.RightHand),
            };

            _leftArm = new TwoBoneArmIK(_animator, HumanBodyBones.LeftUpperArm, HumanBodyBones.LeftLowerArm, HumanBodyBones.LeftHand, -1f);
            _rightArm = new TwoBoneArmIK(_animator, HumanBodyBones.RightUpperArm, HumanBodyBones.RightLowerArm, HumanBodyBones.RightHand, 1f);
            Application.onBeforeRender += SolveArms;

            _lastHead = rig.Head.position;
            _bodyYaw = Yaw(rig.FlatForward);
            SnapToHead();
        }

        public void SetVisible(bool visible)
        {
            _visible = visible;
            ApplyShown();
        }

        private void OnDestroy() => Application.onBeforeRender -= SolveArms;

        private void LateUpdate()
        {
            Transform head = _rig.Head;
            float deltaTime = Mathf.Max(Time.deltaTime, 0.0001f);

            Vector3 moved = head.position - _lastHead;
            _lastHead = head.position;
            moved.y = 0f;
            float rawSpeed = moved.magnitude / deltaTime;
            if (rawSpeed > 6f)
                rawSpeed = 0f;
            _speed = Mathf.Lerp(_speed, rawSpeed, 1f - Mathf.Exp(-8f * deltaTime));

            float eye = head.position.y - _floor.position.y;
            if (_standingEye <= 0f || Mathf.Abs(eye - _lastEye) > 0.15f)
                _standingEye = eye;
            else if (eye > _standingEye)
                _standingEye = Mathf.Lerp(_standingEye, eye, 0.02f);
            else
                _standingEye = Mathf.MoveTowards(_standingEye, eye, 0.03f * deltaTime);
            _lastEye = eye;
            FitHeight();
            ApplyShown(eye);

            UpdateYaw(deltaTime);
            SnapToHead();

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
                bone.localScale = Vector3.one * 0.0001f;
            }

            SolveArms();
        }

        private void UpdateYaw(float deltaTime)
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

        private void SolveArms()
        {
            if (!_shown)
                return;

            Quaternion body = Quaternion.Euler(0f, _bodyYaw, 0f);
            _leftArm.Solve(_hands.GetWrist(true), body);
            _rightArm.Solve(_hands.GetWrist(false), body);
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
