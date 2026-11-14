using System;
using UnityEngine;

namespace Gameplay.Entities.Participant
{
    [Serializable]
    public class ProximityLogic
    {
        [Header("Levels (metres, enter / release)")]
        [SerializeField] private float _stopDistance = 0.8f;
        [SerializeField] private float _stopRelease = 0.95f;
        [SerializeField] private float _nearbyDistance = 1.4f;
        [SerializeField] private float _nearbyRelease = 1.55f;

        [Header("Feedback intensity")]
        [SerializeField] private float _intensityStart = 1.8f;
        [SerializeField] private float _intensityFull = 0.45f;
        [SerializeField] private float _riseSpeed = 5f;
        [SerializeField] private float _fallSpeed = 2.2f;

        public float StopDistance => _stopDistance;
        public ProximityLevel Level { get; private set; }
        public float Intensity { get; private set; }

        public ProximityLevel Evaluate(float distance)
        {
            if (distance <= (Level == ProximityLevel.Stop ? _stopRelease : _stopDistance))
                return ProximityLevel.Stop;

            return distance <= (Level != ProximityLevel.Clear ? _nearbyRelease : _nearbyDistance)
                ? ProximityLevel.Nearby
                : ProximityLevel.Clear;
        }

        public bool Tick(float distance, bool monitoring, float deltaTime)
        {
            ProximityLevel level = monitoring ? Evaluate(distance) : ProximityLevel.Clear;
            float target = monitoring ? Mathf.InverseLerp(_intensityStart, _intensityFull, distance) : 0f;
            float speed = target > Intensity ? _riseSpeed : _fallSpeed;
            Intensity = Mathf.MoveTowards(Intensity, target, deltaTime * speed);

            if (level == Level)
                return false;

            Level = level;
            return true;
        }

        public void Reset()
        {
            Level = ProximityLevel.Clear;
            Intensity = 0f;
        }
    }
}
