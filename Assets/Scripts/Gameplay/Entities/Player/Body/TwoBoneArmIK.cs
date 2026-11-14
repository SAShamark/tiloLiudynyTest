using UnityEngine;

namespace Gameplay.Entities.Player.Body
{
    internal sealed class TwoBoneArmIK
    {
        private readonly Transform _shoulder;
        private readonly Transform _upper;
        private readonly Transform _lower;
        private readonly Transform _hand;
        private readonly float _side;

        public TwoBoneArmIK(Animator animator, HumanBodyBones upper, HumanBodyBones lower, HumanBodyBones hand, float side)
        {
            _shoulder = animator.GetBoneTransform(side < 0f ? HumanBodyBones.LeftShoulder : HumanBodyBones.RightShoulder);
            _upper = animator.GetBoneTransform(upper);
            _lower = animator.GetBoneTransform(lower);
            _hand = animator.GetBoneTransform(hand);
            _side = side;
        }

        public void Solve(Transform target, Quaternion body)
        {
            if (target == null)
                return;

            float upperLength = Vector3.Distance(_upper.position, _lower.position);
            float lowerLength = Vector3.Distance(_lower.position, _hand.position);
            float reach = upperLength + lowerLength;

            float shortfall = Vector3.Distance(_upper.position, target.position) - reach * 0.92f;
            if (shortfall > 0f)
            {
                Vector3 current = _upper.position - _shoulder.position;
                Vector3 wanted = target.position - _shoulder.position;
                float swingAmount = Mathf.Clamp01(shortfall / 0.12f);
                Quaternion swing = Quaternion.FromToRotation(current, Vector3.Slerp(current.normalized, wanted.normalized, 0.35f * swingAmount));
                _shoulder.rotation = Quaternion.RotateTowards(Quaternion.identity, swing, 25f) * _shoulder.rotation;
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

            float cosine = Mathf.Clamp((upperLength * upperLength + distance * distance - lowerLength * lowerLength)
                / (2f * upperLength * distance), -1f, 1f);
            float sine = Mathf.Sqrt(1f - cosine * cosine);
            Vector3 elbow = shoulder + direction * (upperLength * cosine) + bend * (upperLength * sine);

            _upper.rotation = Quaternion.FromToRotation(_lower.position - shoulder, elbow - shoulder) * _upper.rotation;
            Vector3 wristGoal = shoulder + direction * distance;
            _lower.rotation = Quaternion.FromToRotation(_hand.position - _lower.position, wristGoal - _lower.position) * _lower.rotation;
        }
    }
}
