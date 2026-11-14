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

        private PlayerRig _rig;
        private ProximityAlertView _alertView;
        private ProximityWarningFeedback _feedback;
        private bool _monitoring;
        private float _nextHaptic;

        public bool IsStopActive =>
            _monitoring && _rig != null && isActiveAndEnabled
            && _proximity.Evaluate(_rig.FlatOffsetTo(transform.position).magnitude) == ProximityLevel.Stop;

        public void Init(PlayerRig rig)
        {
            _rig = rig;
            _movement.Init(transform, _walkingSpeed);
            _feedback = new ProximityWarningFeedback(gameObject, rig.Head);
            _alertView = new ProximityAlertView(transform, rig.Head, _warning, _font, _displayName);
        }

        public void SetMonitoring(bool monitoring) => _monitoring = monitoring;

        private void Update()
        {
            if (_rig == null)
                return;

            _movement.Tick(Time.deltaTime);
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
            _feedback.Apply(intensity);
            _alertView.ShowAlert(intensity, _proximity.Level, _rig.FlatForward, offset);
            PulseHaptics(intensity);
        }

        private void PulseHaptics(float intensity)
        {
            if (intensity <= 0.08f || Time.unscaledTime < _nextHaptic)
                return;

            float interval = Mathf.Lerp(0.33f, 0.12f, intensity);
            _rig.SendHaptics(intensity, interval * Mathf.Lerp(0.35f, 1f, intensity));
            _nextHaptic = Time.unscaledTime + interval;
        }

        private void OnDisable()
        {
            _proximity.Reset();
            _nextHaptic = 0f;
            _feedback?.Apply(0f);
            _alertView?.Hide();
            if (_alertView == null && _warning != null)
                _warning.SetActive(false);
        }

        private void OnDestroy() => _feedback?.Dispose();
    }
}
