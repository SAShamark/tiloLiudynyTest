using Gameplay.Entities.Interactive.Station;
using TMPro;
using UnityEngine;

namespace Gameplay.Entities.Environment
{
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

        public void Initialize(HeartStationControl station)
        {
            _station = station;
            _station.StateChanged += HandleStateChanged;
            _station.Shocked += HandleShocked;
            HandleStateChanged(_station.State);
        }

        private void HandleShocked()
        {
            _shockLeft = 0.35f;
            _shockLabelLeft = 1.2f;
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
            _station.StateChanged -= HandleStateChanged;
            _station.Shocked -= HandleShocked;
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
                    float shockProgress = 1f - _shockLeft / 0.35f;
                    value = Mathf.Sin(shockProgress * Mathf.PI * 7f) * (1f - shockProgress) * 1.8f;
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

        private static float Waveform(float phase)
        {
            float pWave = Bump(phase, 0.12f, 0.035f) * 0.15f;
            float qWave = -Bump(phase, 0.27f, 0.012f) * 0.25f;
            float rWave = Bump(phase, 0.30f, 0.014f) * 1f;
            float sWave = -Bump(phase, 0.33f, 0.014f) * 0.35f;
            float tWave = Bump(phase, 0.55f, 0.06f) * 0.3f;
            return pWave + qWave + rWave + sWave + tWave;
        }

        private static float Bump(float phase, float center, float width)
        {
            float normalizedOffset = (phase - center) / width;
            return Mathf.Exp(-normalizedOffset * normalizedOffset);
        }

        private void HandleStateChanged(StationState state)
        {
            _active = state == StationState.Active;
            _rateLabel.text = _active ? string.Format(_activeFormat, 60f / _beatPeriod) : _idleText;
        }
    }
}
