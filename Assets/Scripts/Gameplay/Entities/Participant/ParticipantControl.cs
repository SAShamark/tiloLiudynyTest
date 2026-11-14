using Gameplay.Entities.Player;
using Gameplay.Entities.Player.Movement;
using TMPro;
using UI.Settings;
using UnityEngine;

namespace Gameplay.Entities.Participant
{
    public sealed class ParticipantControl : MonoBehaviour
    {
        [SerializeField] private string _displayName = "Participant 2";
        [SerializeField] private GameObject _warning;
        [SerializeField] private TextMeshPro _nameplate;
        [SerializeField] private TextMeshPro _alert;
        [SerializeField] private float _walkingSpeed = 0.65f;
        [SerializeField] private RouteMovementLogic _movement = new();
        [SerializeField] private ProximityLogic _proximity = new();
        [SerializeField] private ProximityWarningEffects _warningEffects = new();

        [Header("Avatar")]
        [SerializeField] private Animator _animator;
        [Tooltip("Walking speed (m/s) at which the walk clip plays at normal speed, so feet do not slide.")]
        [SerializeField, Min(0.1f)] private float _clipWalkSpeed = 1.2f;

        private static readonly int IdleParameter = Animator.StringToHash("Idle");

        private PlayerRig _rig;
        private SettingsMenu _settings;
        private ProximityAlertView _alertView;
        private bool _monitoring;
        private bool _idle;
        private float _nextHaptic;

        public bool IsTooClose => _monitoring && isActiveAndEnabled
                                              && _proximity.Evaluate(_rig.FlatOffsetTo(transform.position).magnitude) == ProximityLevel.Stop;

        public void Initialize(PlayerRig rig, SettingsMenu settings)
        {
            _rig = rig;
            _settings = settings;
            _movement.Initialize(transform, _walkingSpeed);
            _warningEffects.Initialize(_proximity.StopDistance);
            _alertView = new ProximityAlertView(_nameplate, _alert, _warning, _displayName);

            SetIdle(settings.ParticipantIdle);
            settings.ParticipantIdleChanged += SetIdle;
            settings.StickWarningsChanged += HandleStickWarningsChanged;
            settings.Locomotion.ModeChanged += HandleMovementModeChanged;
            RefreshMonitoring();
        }

        private void HandleStickWarningsChanged(bool enabled) => RefreshMonitoring();

        private void HandleMovementModeChanged(MovementMode mode) => RefreshMonitoring();

        private void RefreshMonitoring()
        {
            MovementMode mode = _settings.Locomotion.Mode;
            _monitoring = mode == MovementMode.Physical || (mode == MovementMode.Stick && _settings.StickWarnings);
        }

        private void SetIdle(bool idle)
        {
            _idle = idle;
            _animator.SetBool(IdleParameter, idle);
        }

        private void Update()
        {
            if (!_idle)
                _movement.Tick(Time.deltaTime);

            _animator.speed = _idle ? 1f : _movement.CurrentSpeed / _clipWalkSpeed;
        }

        private void LateUpdate()
        {
            Vector3 offset = _rig.FlatOffsetTo(transform.position);
            _alertView.FaceViewer(offset);

            if (_proximity.Tick(offset.magnitude, _monitoring, Time.unscaledDeltaTime))
                _alertView.ShowLevel(_proximity.Level);

            float intensity = _proximity.Intensity;
            float angle = Vector3.SignedAngle(_rig.FlatForward, offset, Vector3.up);
            bool stop = _proximity.Level == ProximityLevel.Stop;
            _warningEffects.Apply(intensity, angle, stop);
            _alertView.ShowAlert(intensity, _proximity.Level, _rig.FlatForward, offset);
            PulseHaptics(intensity, angle);
        }

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
            _warningEffects.Apply(0f, 0f, false);
            _alertView.Hide();
        }

        private void OnDestroy()
        {
            _warningEffects.Dispose();
            _settings.ParticipantIdleChanged -= SetIdle;
            _settings.StickWarningsChanged -= HandleStickWarningsChanged;
            _settings.Locomotion.ModeChanged -= HandleMovementModeChanged;
        }
    }
}
