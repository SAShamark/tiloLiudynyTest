using TMPro;
using UnityEngine;

namespace Gameplay.Entities.Participant
{
    public sealed class ProximityAlertView
    {
        private static readonly Color NearbyColor = new(1f, 0.82f, 0.32f);
        private static readonly Color StopColor = new(1f, 0.32f, 0.28f);

        private readonly string _participantName;
        private readonly GameObject _stopMarker;
        private readonly TextMeshPro _nameplate;
        private readonly TextMeshPro _alert;
        private string _lastSide;

        public ProximityAlertView(TextMeshPro nameplate, TextMeshPro alert, GameObject stopMarker, string participantName)
        {
            _nameplate = nameplate;
            _alert = alert;
            _stopMarker = stopMarker;
            _participantName = participantName;
            _nameplate.text = participantName;
            _alert.gameObject.SetActive(false);
        }

        public void FaceViewer(Vector3 flatOffsetFromViewer)
        {
            if (flatOffsetFromViewer.sqrMagnitude <= 0.0001f)
                return;

            _nameplate.transform.rotation = Quaternion.LookRotation(flatOffsetFromViewer);
            if (_stopMarker.activeSelf)
                _stopMarker.transform.rotation = _nameplate.transform.rotation;
        }

        public void ShowLevel(ProximityLevel level)
        {
            _lastSide = null;
            _stopMarker.SetActive(level == ProximityLevel.Stop);
            _nameplate.color = level switch
            {
                ProximityLevel.Stop => StopColor,
                ProximityLevel.Nearby => NearbyColor,
                _ => Color.white,
            };
        }

        public void ShowAlert(float intensity, ProximityLevel level, Vector3 viewerForward, Vector3 flatOffset)
        {
            bool visible = intensity > 0.005f;
            _alert.gameObject.SetActive(visible);
            if (!visible)
                return;

            Color color = Color.Lerp(NearbyColor, StopColor, intensity);
            color.a = Mathf.Clamp01(intensity * 2f);
            _alert.color = color;

            string side = GetSide(viewerForward, flatOffset);
            if (side == _lastSide)
                return;

            _lastSide = side;
            string arrowed = Arrow(side, _participantName);
            _alert.text = level == ProximityLevel.Stop
                ? $"<b>STOP</b>\n{arrowed}"
                : arrowed;
        }

        private static string Arrow(string side, string name) => side switch
        {
            "left" => $"<<  {name} on the left",
            "right" => $"{name} on the right  >>",
            "behind" => $"{name} behind you",
            _ => $"{name} ahead",
        };

        public void Hide()
        {
            _lastSide = null;
            if (_stopMarker != null)
                _stopMarker.SetActive(false);
            if (_alert != null)
                _alert.gameObject.SetActive(false);
            if (_nameplate != null)
                _nameplate.color = Color.white;
        }

        private static string GetSide(Vector3 forward, Vector3 offset)
        {
            float angle = Vector3.SignedAngle(forward, offset, Vector3.up);
            float absolute = Mathf.Abs(angle);
            if (absolute <= 30f)
                return "ahead";
            if (absolute >= 150f)
                return "behind";
            return angle > 0f ? "right" : "left";
        }
    }
}
