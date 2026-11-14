using Gameplay.Entities.Interactive.BaseLogic.Items;
using UnityEngine;

namespace Gameplay.Entities.Interactive.Other.Props
{
    public sealed class CoffeeMug : ReturningItem
    {
        [SerializeField] private Transform _liquid;
        [Tooltip("Local Y of the liquid surface when empty and when full.")]
        [SerializeField] private Vector2 _liquidHeight = new(0.005f, 0.075f);
        [SerializeField] private ParticleSystem _steam;
        [SerializeField, Range(30f, 150f)] private float _pourAngle = 95f;
        [SerializeField, Min(0.05f)] private float _pourRate = 0.6f;

        private float _fill;

        public float Fill => _fill;

        public float SurfaceHeight => transform.TransformPoint(0f, Mathf.Lerp(_liquidHeight.x, _liquidHeight.y, _fill), 0f).y;
        public bool IsFull => _fill >= 0.999f;

        protected override void Awake()
        {
            base.Awake();
            Apply();
        }

        public void AddCoffee(float amount)
        {
            _fill = Mathf.Clamp01(_fill + amount);
            Apply();
        }

        private void Update()
        {
            if (_fill <= 0f)
                return;

            float tilt = Vector3.Angle(transform.up, Vector3.up);
            if (tilt < _pourAngle)
                return;

            _fill = Mathf.Max(0f, _fill - _pourRate * Time.deltaTime);
            Apply();
        }

        private void Apply()
        {
            _liquid.gameObject.SetActive(_fill > 0.01f);
            Vector3 position = _liquid.localPosition;
            position.y = Mathf.Lerp(_liquidHeight.x, _liquidHeight.y, _fill);
            _liquid.localPosition = position;
            bool steaming = _fill > 0.3f;
            if (steaming && !_steam.isPlaying)
                _steam.Play();
            else if (!steaming && _steam.isPlaying)
                _steam.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
    }
}
