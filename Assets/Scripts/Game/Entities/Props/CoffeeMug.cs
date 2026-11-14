using Game.Entities.Pickups;
using UnityEngine;

namespace Game.Entities.Props
{
    /// <summary>
    /// Mug that fills under the coffee machine and empties when tipped over. The liquid is a disc
    /// that rises inside the mug; steam shows while it holds coffee.
    /// </summary>
    public sealed class CoffeeMug : Pickup
    {
        [SerializeField] private Transform _liquid;
        [Tooltip("Local Y of the liquid surface when empty and when full.")]
        [SerializeField] private Vector2 _liquidHeight = new(0.005f, 0.075f);
        [SerializeField] private ParticleSystem _steam;
        [SerializeField, Range(30f, 150f)] private float _pourAngle = 95f;
        [SerializeField, Min(0.05f)] private float _pourRate = 0.6f;

        private float _fill;

        public float Fill => _fill;

        /// <summary>World height of the coffee surface (the mug bottom when empty).</summary>
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
            if (_liquid != null)
            {
                _liquid.gameObject.SetActive(_fill > 0.01f);
                Vector3 position = _liquid.localPosition;
                position.y = Mathf.Lerp(_liquidHeight.x, _liquidHeight.y, _fill);
                _liquid.localPosition = position;
            }

            if (_steam == null)
                return;

            bool steaming = _fill > 0.3f;
            if (steaming && !_steam.isPlaying)
                _steam.Play();
            else if (!steaming && _steam.isPlaying)
                _steam.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
    }
}
