using Game.Entities.Participants.Parts;
using Game.Entities.Player;
using Game.Safety;
using TMPro;
using UnityEngine;

namespace Game.Entities.Participants
{
    /// <summary>
    /// Simulated second visitor sharing the room. Walks a route and warns the player when too close.
    /// </summary>
    public sealed class ParticipantControl : MonoBehaviour, ISafetyGate
    {
        [SerializeField] private GameObject _warning;
        [SerializeField] private TMP_FontAsset _font;
        [SerializeField] private string _displayName = "Participant 2";
        [SerializeField] private float _walkingSpeed = 0.65f;
        [SerializeField] private RouteMovement _movement = new();
        [SerializeField] private ProximityMonitor _proximity = new();

        [Header("Avatar")]
        [SerializeField] private Animator _animator;
        [Tooltip("Walking speed (m/s) at which the walk clip plays at normal speed, so feet do not slide.")]
        [SerializeField, Min(0.1f)] private float _clipWalkSpeed = 1.2f;

        private static readonly int IdleParameter = Animator.StringToHash("Idle");

        private PlayerRig _rig;
        private ProximityAlertView _alertView;
        private ProximityWarningFeedback _feedback;
        private bool _monitoring;
        private bool _idle;
        private float _nextHaptic;

        public bool IsIdle => _idle;

        public bool IsStopActive =>
            _monitoring && _rig != null && isActiveAndEnabled
            && _proximity.Evaluate(_rig.FlatOffsetTo(transform.position).magnitude) == ProximityLevel.Stop;

        public void Init(PlayerRig rig)
        {
            _rig = rig;
            _movement.Init(transform, _walkingSpeed);
            Transform head = transform.Find("Head");
            _feedback = new ProximityWarningFeedback(head != null ? head : transform, transform, rig.Head, _proximity.StopDistance);
            _alertView = new ProximityAlertView(transform, rig.Head, _warning, _font, _displayName);
        }

        public void SetMonitoring(bool monitoring) => _monitoring = monitoring;

        /// <summary>Stops on the spot and plays the idle clip; resuming continues the route from here.</summary>
        public void SetIdle(bool idle)
        {
            _idle = idle;
            if (_animator != null)
                _animator.SetBool(IdleParameter, idle);
        }

        private void Update()
        {
            if (_rig == null)
                return;

            if (!_idle)
                _movement.Tick(Time.deltaTime);

            if (_animator != null)
                _animator.speed = _idle ? 1f : _movement.CurrentSpeed / _clipWalkSpeed;
        }

        private void LateUpdate()
        {
            if (_rig == null)
                return;

            Vector3 offset = _rig.FlatOffsetTo(transform.position);
            _alertView.FaceViewer(offset);

            if (_proximity.Tick(offset.magnitude, _monitoring, Time.unscaledDeltaTime))
                _alertView.ShowLevel(_proximity.Level);

            float intensity = _proximity.Intensity;
            float angle = Vector3.SignedAngle(_rig.FlatForward, offset, Vector3.up);
            bool stop = _proximity.Level == ProximityLevel.Stop;
            _feedback.Apply(intensity, angle, stop);
            _alertView.ShowAlert(intensity, _proximity.Level, _rig.FlatForward, offset);
            PulseHaptics(intensity, angle);
        }

        // The hand on the participant's side buzzes hardest, so the player can feel where they are.
        private void PulseHaptics(float intensity, float angle)
        {
            if (intensity <= 0.08f || Time.unscaledTime < _nextHaptic)
                return;

            float interval = Mathf.Lerp(0.33f, 0.12f, intensity);
            float side = Mathf.Sin(angle * Mathf.Deg2Rad);
            float right = Mathf.Lerp(0.08f, 1f, Mathf.Pow(Mathf.Clamp01(0.5f + 0.5f * side), 1.5f));
            float left = Mathf.Lerp(0.08f, 1f, Mathf.Pow(Mathf.Clamp01(0.5f - 0.5f * side), 1.5f));
            float duration = interval * Mathf.Lerp(0.35f, 1f, intensity);
            _rig.SendHaptics(intensity * left, intensity * right, duration);
            _nextHaptic = Time.unscaledTime + interval;
        }

        private void OnDisable()
        {
            _proximity.Reset();
            _nextHaptic = 0f;
            _feedback?.Apply(0f, 0f, false);
            _alertView?.Hide();
            if (_alertView == null && _warning != null)
                _warning.SetActive(false);
        }

        private void OnDestroy() => _feedback?.Dispose();
    }
}
