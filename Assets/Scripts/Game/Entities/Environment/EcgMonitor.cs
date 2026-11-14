using Game.Entities.Station;
using TMPro;
using UnityEngine;

namespace Game.Entities.Environment
{
    /// <summary>
    /// Wall monitor with a scrolling ECG trace: flat line while the station is idle,
    /// a heartbeat in the station's rhythm while it runs.
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public sealed class EcgMonitor : MonoBehaviour
    {
        [SerializeField] private Vector2 _size = new(0.5f, 0.18f);
        [SerializeField, Range(32, 512)] private int _samples = 160;
        [SerializeField, Min(0.1f)] private float _sweepDuration = 3f;
        [SerializeField, Min(0.3f)] private float _beatPeriod = 0.9f;
        [SerializeField] private TMP_Text _rateLabel;
        [SerializeField] private string _idleText = "-- BPM";
        [SerializeField] private string _activeFormat = "{0:0} BPM";
        [SerializeField] private string _shockText = "SHOCK!";

        private LineRenderer _line;
        private Vector3[] _points;
        private float[] _values;
        private float _cursor;
        private float _beatTime;
        private bool _active;
        private HeartStationControl _station;
        private float _shockLeft;
        private float _shockLabelLeft;

        public void Init(HeartStationControl station)
        {
            _station = station;
            _station.OnStateChanged += HandleStateChanged;
            _station.OnShocked += HandleShocked;
            HandleStateChanged(_station.State);
        }

        // A defibrillator shock: a tall spike, a brief wobble, and "SHOCK" on the readout.
        private void HandleShocked()
        {
            _shockLeft = 0.35f;
            _shockLabelLeft = 1.2f;
            if (_rateLabel != null)
                _rateLabel.text = _shockText;
        }

        private void Awake()
        {
            _line = GetComponent<LineRenderer>();
            _line.useWorldSpace = false;
            _line.positionCount = _samples;
            _points = new Vector3[_samples];
            _values = new float[_samples];
            for (int i = 0; i < _samples; i++)
                _points[i] = new Vector3(Mathf.Lerp(-0.5f, 0.5f, (float)i / (_samples - 1)) * _size.x, 0f, 0f);
            _line.SetPositions(_points);
        }

        private void OnDestroy()
        {
            if (_station == null)
                return;

            _station.OnStateChanged -= HandleStateChanged;
            _station.OnShocked -= HandleShocked;
        }

        private void Update()
        {
            float step = _sweepDuration / _samples;
            _cursor += Time.deltaTime;
            while (_cursor >= step)
            {
                _cursor -= step;
                _beatTime += step;
                float value = _active ? Waveform(_beatTime % _beatPeriod / _beatPeriod) : 0f;
                if (_shockLeft > 0f)
                {
                    float t = 1f - _shockLeft / 0.35f;
                    value = Mathf.Sin(t * Mathf.PI * 7f) * (1f - t) * 1.8f;
                    _shockLeft -= step;
                }

                Shift(value);
            }

            if (_shockLabelLeft > 0f)
            {
                _shockLabelLeft -= Time.deltaTime;
                if (_shockLabelLeft <= 0f)
                    HandleStateChanged(_station.State);
            }

            for (int i = 0; i < _samples; i++)
                _points[i].y = _values[i] * _size.y * 0.5f;
            _line.SetPositions(_points);
        }

        private void Shift(float value)
        {
            System.Array.Copy(_values, 1, _values, 0, _samples - 1);
            _values[_samples - 1] = value;
        }

        // Stylised P-QRS-T complex over one beat (phase 0..1).
        private static float Waveform(float phase)
        {
            float p = Bump(phase, 0.12f, 0.035f) * 0.15f;
            float q = -Bump(phase, 0.27f, 0.012f) * 0.25f;
            float r = Bump(phase, 0.30f, 0.014f) * 1f;
            float s = -Bump(phase, 0.33f, 0.014f) * 0.35f;
            float t = Bump(phase, 0.55f, 0.06f) * 0.3f;
            return p + q + r + s + t;
        }

        private static float Bump(float x, float center, float width)
        {
            float d = (x - center) / width;
            return Mathf.Exp(-d * d);
        }

        private void HandleStateChanged(StationState state)
        {
            _active = state == StationState.Active;
            if (_rateLabel != null)
                _rateLabel.text = _active ? string.Format(_activeFormat, 60f / _beatPeriod) : _idleText;
        }
    }
}
