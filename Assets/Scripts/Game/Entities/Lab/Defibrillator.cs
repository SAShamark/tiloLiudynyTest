using Game.Entities.Environment;
using Game.Entities.Station;
using Services.Audio;
using TMPro;
using UnityEngine;

namespace Game.Entities.Lab
{
    /// <summary>
    /// Defibrillator cart. Grab both paddles and press them to the heart hologram: the unit charges
    /// with a rising whine, then discharges — zap, flash, sparks, both hands buzz, the heart jolts
    /// and the ECG monitors spike. Pulling a paddle away while charging cancels the shock.
    /// </summary>
    public sealed class Defibrillator : MonoBehaviour
    {
        private enum Phase { Idle, Charging, Cooldown }

        private static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");

        [SerializeField] private DefibPaddle _left;
        [SerializeField] private DefibPaddle _right;
        [SerializeField] private TMP_Text _screen;
        [SerializeField] private Renderer _chargeBar;
        [SerializeField] private Color _chargeColor = new(1f, 0.55f, 0.1f);
        [SerializeField] private Color _readyColor = new(0.2f, 1f, 0.5f);

        [Header("Shock")]
        [Tooltip("How close both paddle faces must be to the heart (metres).")]
        [SerializeField, Min(0.05f)] private float _contactDistance = 0.3f;
        [SerializeField, Min(0.2f)] private float _chargeTime = 1.3f;
        [SerializeField, Min(0.2f)] private float _cooldown = 2.5f;
        [SerializeField] private ParticleSystem _sparks;
        [SerializeField] private Light _flash;
        [SerializeField, Min(0f)] private float _flashIntensity = 6f;
        [SerializeField, Min(1f)] private float _heartKick = 1.3f;

        [Header("Rubbing the paddles together")]
        [SerializeField] private SparkBurst _rubSparks;
        [Tooltip("Paddle faces closer than this count as touching.")]
        [SerializeField, Min(0.02f)] private float _rubDistance = 0.1f;
        [Tooltip("Relative speed (m/s) the faces must slide at to throw sparks.")]
        [SerializeField, Min(0.05f)] private float _rubSpeed = 0.25f;

        private HeartStationControl _station;
        private Transform _heartModel;
        private Vector3 _heartRestScale;
        private AudioSource _audio;
        private AudioClip _charge;
        private AudioClip _zap;
        private AudioClip _noTarget;
        private MaterialPropertyBlock _block;
        private Phase _phase;
        private float _timer;
        private float _kick;
        private float _flashLeft;
        private string _shownText;
        private float _barWidth;
        private Vector3 _lastRubOffset;
        private bool _rubValid;

        public void Init(HeartStationControl station)
        {
            _station = station;
            _heartModel = station.ProjectionPoint.Find("Model Root");
            if (_heartModel != null)
                _heartRestScale = _heartModel.localScale;
        }

        private void Awake()
        {
            _block = new MaterialPropertyBlock();
            _charge = ProceduralTones.CreatePowerUp("Defib charge");
            _zap = ProceduralTones.CreateZap("Defib zap");
            _noTarget = ProceduralTones.CreateBeep("Defib no target", 440f, 0.25f);
            _audio = gameObject.AddComponent<AudioSource>();
            _audio.playOnAwake = false;
            _audio.spatialBlend = 0.7f;
            _audio.minDistance = 1f;
            _audio.maxDistance = 8f;
            if (_flash != null)
                _flash.enabled = false;

            ShowText("READY");
            SetBar(0f, _readyColor);
        }

        private void OnDestroy()
        {
            Destroy(_charge);
            Destroy(_zap);
            Destroy(_noTarget);
        }

        private void Update()
        {
            AnimateEffects(Time.deltaTime);
            if (_station == null || _left == null || _right == null)
                return;

            bool bothHeld = _left.IsHeld && _right.IsHeld;
            UpdateRubbing(bothHeld);
            bool onHeart = bothHeld && _station.IsOrganVisible && IsTouching(_left) && IsTouching(_right);

            switch (_phase)
            {
                case Phase.Idle:
                    ShowText(!bothHeld ? "READY" : _station.IsOrganVisible ? "PLACE PADDLES" : "NO PATIENT");
                    SetBar(0f, _readyColor);
                    if (onHeart)
                        BeginCharge();
                    break;

                case Phase.Charging:
                    if (!onHeart)
                    {
                        CancelCharge();
                        break;
                    }

                    _timer += Time.deltaTime;
                    float progress = Mathf.Clamp01(_timer / _chargeTime);
                    SetBar(progress, Color.Lerp(_chargeColor, _readyColor, progress));
                    _left.SendHaptics(0.1f + 0.3f * progress, 0.05f);
                    _right.SendHaptics(0.1f + 0.3f * progress, 0.05f);
                    if (progress >= 1f)
                        Discharge();
                    break;

                case Phase.Cooldown:
                    _timer -= Time.deltaTime;
                    SetBar(Mathf.Clamp01(_timer / _cooldown), _chargeColor);
                    if (_timer <= 0f)
                        _phase = Phase.Idle;
                    break;
            }
        }

        // Classic TV move: rub the paddles together and they spit sparks.
        private void UpdateRubbing(bool bothHeld)
        {
            Vector3 offset = _left.ContactPoint - _right.ContactPoint;
            bool wasValid = _rubValid;
            _rubValid = bothHeld;
            Vector3 previous = _lastRubOffset;
            _lastRubOffset = offset;
            if (!bothHeld || !wasValid || _rubSparks == null || Time.deltaTime <= 0f)
                return;

            if (offset.sqrMagnitude > _rubDistance * _rubDistance)
                return;

            float slide = Vector3.Distance(offset, previous) / Time.deltaTime;
            if (slide < _rubSpeed)
                return;

            Vector3 middle = (_left.ContactPoint + _right.ContactPoint) * 0.5f;
            int count = Mathf.Clamp(Mathf.RoundToInt(slide * 6f), 2, 10);
            _rubSparks.Emit(middle, count, crackle: true);
            float buzz = Mathf.Clamp01(slide * 0.4f);
            _left.SendHaptics(buzz, 0.04f);
            _right.SendHaptics(buzz, 0.04f);
        }

        private bool IsTouching(DefibPaddle paddle) =>
            (paddle.ContactPoint - _station.ProjectionPoint.position).sqrMagnitude <= _contactDistance * _contactDistance;

        private void BeginCharge()
        {
            _phase = Phase.Charging;
            _timer = 0f;
            ShowText("CHARGING...");
            _audio.clip = _charge;
            _audio.pitch = 0.9f / _chargeTime;
            _audio.Play();
        }

        private void CancelCharge()
        {
            _phase = Phase.Idle;
            _audio.Stop();
            _audio.pitch = 1f;
            _audio.PlayOneShot(_noTarget, 0.5f);
        }

        private void Discharge()
        {
            _phase = Phase.Cooldown;
            _timer = _cooldown;
            ShowText("CLEAR!");
            _audio.Stop();
            _audio.pitch = 1f;
            _audio.PlayOneShot(_zap, 1f);

            _left.SendHaptics(1f, 0.35f);
            _right.SendHaptics(1f, 0.35f);

            Vector3 heart = _station.ProjectionPoint.position;
            if (_sparks != null)
            {
                _sparks.transform.position = heart;
                _sparks.Play(true);
            }

            if (_flash != null)
            {
                _flash.transform.position = heart;
                _flashLeft = 0.25f;
            }

            _kick = 1f;
            _station.ReceiveShock();
        }

        private void AnimateEffects(float deltaTime)
        {
            if (_flash != null)
            {
                _flashLeft = Mathf.Max(0f, _flashLeft - deltaTime);
                _flash.enabled = _flashLeft > 0f;
                _flash.intensity = _flashIntensity * (_flashLeft / 0.25f);
            }

            if (_heartModel == null || _kick <= 0f)
                return;

            _kick = Mathf.Max(0f, _kick - deltaTime * 2.2f);
            float wobble = Mathf.Sin((1f - _kick) * Mathf.PI * 4f) * _kick;
            _heartModel.localScale = _heartRestScale * (1f + (_heartKick - 1f) * wobble);
        }

        private void ShowText(string text)
        {
            if (_screen == null || _shownText == text)
                return;

            _shownText = text;
            _screen.text = text;
        }

        private void SetBar(float amount, Color color)
        {
            if (_chargeBar == null)
                return;

            if (_barWidth <= 0f)
                _barWidth = _chargeBar.transform.localScale.x;

            Vector3 scale = _chargeBar.transform.localScale;
            scale.x = Mathf.Max(0.001f, amount) * _barWidth;
            _chargeBar.transform.localScale = scale;
            _chargeBar.GetPropertyBlock(_block);
            _block.SetColor(EmissionId, color * 2.5f);
            _chargeBar.SetPropertyBlock(_block);
        }
    }
}
