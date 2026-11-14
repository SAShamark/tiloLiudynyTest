using System;
using Gameplay.Entities.Participant.Proximity;
using Gameplay.Entities.Player;
using UnityEngine;

namespace Gameplay.Entities.Participant
{
    public sealed class ParticipantControl : MonoBehaviour
    {
        private static readonly int IdleParameter = Animator.StringToHash("Idle");

        [SerializeField] private RouteMovementLogic _movement = new();
        [SerializeField] private ProximityLogic _proximity = new();
        [SerializeField] private ProximityWarningEffects _warningEffects = new();
        [SerializeField] private ProximityAlertView _alertView = new();

        [Header("Avatar")]
        [SerializeField] private Animator _animator;
        [SerializeField, Min(0.1f)] private float _clipWalkSpeed = 1.2f;

        private PlayerRig _rig;
        private bool _monitoring;
        private bool _idle;

        public bool IsTooClose => _proximity.Level == ProximityLevel.Stop;

        public event Action<bool> TooCloseChanged;

        public void Initialize(PlayerRig rig)
        {
            _rig = rig;
            _movement.Initialize(transform);
            _warningEffects.Initialize(rig, _proximity.StopDistance);
            _alertView.Initialize(_warningEffects.NearbyColor, _warningEffects.StopColor);
        }

        public void SetIdle(bool idle)
        {
            _idle = idle;
            _animator.SetBool(IdleParameter, idle);
        }

        public void SetMonitoring(bool monitoring) => _monitoring = monitoring;

        private void Update()
        {
            if (!_idle)
            {
                _movement.Tick(Time.deltaTime);
            }

            _animator.speed = _idle ? 1f : _movement.CurrentSpeed / _clipWalkSpeed;
        }

        private void LateUpdate()
        {
            Vector3 offset = _rig.FlatOffsetTo(transform.position);
            _alertView.FaceViewer(offset);

            bool wasTooClose = IsTooClose;
            if (_proximity.Tick(offset.magnitude, _monitoring, Time.unscaledDeltaTime))
            {
                _alertView.ShowLevel(_proximity.Level);
                if (IsTooClose != wasTooClose)
                {
                    TooCloseChanged?.Invoke(IsTooClose);
                }
            }

            float intensity = _proximity.Intensity;
            float angle = Vector3.SignedAngle(_rig.FlatForward, offset, Vector3.up);
            _warningEffects.Apply(intensity, angle, IsTooClose);
            _alertView.ShowAlert(intensity, _proximity.Level, _rig.FlatForward, offset);
        }

        private void OnDisable()
        {
            bool wasTooClose = IsTooClose;
            _proximity.Reset();
            _warningEffects.Reset();
            _alertView.Hide();
            if (wasTooClose)
            {
                TooCloseChanged?.Invoke(false);
            }
        }
    }
}
