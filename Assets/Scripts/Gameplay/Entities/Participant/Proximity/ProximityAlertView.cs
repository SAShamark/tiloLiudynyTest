using System;
using TMPro;
using UnityEngine;

namespace Gameplay.Entities.Participant.Proximity
{
    [Serializable]
    public sealed class ProximityAlertView
    {
        private enum Side
        {
            Ahead,
            Behind,
            Left,
            Right,
        }

        [SerializeField] private string _displayName = "Participant 2";
        [SerializeField] private TextMeshPro _nameplate;
        [SerializeField] private TextMeshPro _alert;
        [SerializeField] private GameObject _stopMarker;

        private Color _nearbyColor;
        private Color _stopColor;
        private Side? _lastSide;

        public void Initialize(Color nearbyColor, Color stopColor)
        {
            _nearbyColor = nearbyColor;
            _stopColor = stopColor;
            _nameplate.text = _displayName;
            _alert.gameObject.SetActive(false);
        }

        public void FaceViewer(Vector3 flatOffsetFromViewer)
        {
            if (flatOffsetFromViewer.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            _nameplate.transform.rotation = Quaternion.LookRotation(flatOffsetFromViewer);
            if (_stopMarker.activeSelf)
            {
                _stopMarker.transform.rotation = _nameplate.transform.rotation;
            }
        }

        public void ShowLevel(ProximityLevel level)
        {
            _lastSide = null;
            _stopMarker.SetActive(level == ProximityLevel.Stop);
            _nameplate.color = level switch
            {
                ProximityLevel.Stop => _stopColor,
                ProximityLevel.Nearby => _nearbyColor,
                _ => Color.white,
            };
        }

        public void ShowAlert(float intensity, ProximityLevel level, Vector3 viewerForward, Vector3 flatOffset)
        {
            bool visible = intensity > 0.005f;
            _alert.gameObject.SetActive(visible);
            if (!visible)
            {
                return;
            }

            Color color = Color.Lerp(_nearbyColor, _stopColor, intensity);
            color.a = Mathf.Clamp01(intensity * 2f);
            _alert.color = color;

            Side side = GetSide(viewerForward, flatOffset);
            if (side == _lastSide)
            {
                return;
            }

            _lastSide = side;
            string direction = Describe(side);
            _alert.text = level == ProximityLevel.Stop ? $"<b>STOP</b>\n{direction}" : direction;
        }

        public void Hide()
        {
            _lastSide = null;
            _stopMarker.SetActive(false);
            _alert.gameObject.SetActive(false);
            _nameplate.color = Color.white;
        }

        private string Describe(Side side) => side switch
        {
            Side.Left => $"<<  {_displayName} on the left",
            Side.Right => $"{_displayName} on the right  >>",
            Side.Behind => $"{_displayName} behind you",
            _ => $"{_displayName} ahead",
        };

        private static Side GetSide(Vector3 forward, Vector3 offset)
        {
            float angle = Vector3.SignedAngle(forward, offset, Vector3.up);
            float absolute = Mathf.Abs(angle);
            if (absolute <= 30f)
            {
                return Side.Ahead;
            }

            if (absolute >= 150f)
            {
                return Side.Behind;
            }

            return angle > 0f ? Side.Right : Side.Left;
        }
    }
}
