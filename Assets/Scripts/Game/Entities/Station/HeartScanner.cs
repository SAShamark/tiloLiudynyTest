using UnityEngine;

namespace Game.Entities.Station
{
    public sealed class HeartScanner : MonoBehaviour
    {
        private const float ScanDuration = 0.8f;

        [SerializeField] private Material _glow;

        private LineRenderer _ring;
        private Light _scanLight;
        private float _elapsed = ScanDuration;

        private void Awake()
        {
            _ring = gameObject.AddComponent<LineRenderer>();
            _ring.useWorldSpace = false;
            _ring.loop = true;
            _ring.widthMultiplier = 0.009f;
            _ring.sharedMaterial = _glow;
            _ring.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _ring.enabled = false;
            _ring.positionCount = 48;
            for (int index = 0; index < _ring.positionCount; index++)
            {
                float angle = index * 2f * Mathf.PI / _ring.positionCount;
                _ring.SetPosition(index, new Vector3(Mathf.Cos(angle) * 0.27f, 0f, Mathf.Sin(angle) * 0.27f));
            }

            _scanLight = gameObject.AddComponent<Light>();
            _scanLight.type = LightType.Point;
            _scanLight.color = new Color(0.18f, 0.78f, 1f);
            _scanLight.range = 0.85f;
            _scanLight.shadows = LightShadows.None;
            _scanLight.enabled = false;
        }

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
