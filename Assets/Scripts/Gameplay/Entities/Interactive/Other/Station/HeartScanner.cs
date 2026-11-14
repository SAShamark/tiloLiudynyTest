using UnityEngine;

namespace Gameplay.Entities.Interactive.Other.Station
{
    public sealed class HeartScanner : MonoBehaviour
    {
        private const float ScanDuration = 0.8f;

        [SerializeField] private LineRenderer _ring;
        [SerializeField] private Light _scanLight;

        private float _elapsed = ScanDuration;

        public void Play() => _elapsed = 0f;

        public void UpdateEffect(bool suppressed)
        {
            _elapsed = Mathf.Min(ScanDuration, _elapsed + Time.deltaTime);
            bool scanning = !suppressed && _elapsed < ScanDuration;
            _ring.enabled = scanning;
            _scanLight.enabled = scanning;
            if (!scanning)
                return;

            float progress = _elapsed / ScanDuration;
            float envelope = Mathf.Sin(progress * Mathf.PI);
            transform.localPosition = new Vector3(0f, Mathf.Lerp(0.93f, 1.65f, progress), 0.08f);
            _ring.widthMultiplier = Mathf.Lerp(0.006f, 0.017f, envelope);
            _scanLight.intensity = 2.6f * envelope;
        }
    }
}
