using System;
using UnityEngine;

namespace Gameplay.Entities.Participant
{
    [Serializable]
    public sealed class ProximityZoneRing
    {
        private const int Segments = 48;

        [SerializeField] private LineRenderer _line;
        [SerializeField] private Color _nearbyColor = new(1f, 0.75f, 0.2f);
        [SerializeField] private Color _stopColor = new(1f, 0.12f, 0.08f);

        public void Initialize(float radius)
        {
            _line.useWorldSpace = false;
            _line.loop = true;
            _line.positionCount = Segments;
            for (int i = 0; i < Segments; i++)
            {
                float angle = i * Mathf.PI * 2f / Segments;
                _line.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f));
            }

            _line.enabled = false;
        }

        public void Show(float intensity, bool stop, float pulse)
        {
            bool visible = intensity > 0.01f;
            _line.enabled = visible;
            if (!visible)
            {
                return;
            }

            Color color = stop ? _stopColor : _nearbyColor;
            color.a = Mathf.Clamp01(intensity * 1.5f) * (stop ? Mathf.Lerp(0.55f, 1f, pulse) : 0.7f);
            _line.startColor = color;
            _line.endColor = color;
            _line.widthMultiplier = stop ? Mathf.Lerp(0.035f, 0.06f, pulse) : 0.03f;
        }
    }
}
