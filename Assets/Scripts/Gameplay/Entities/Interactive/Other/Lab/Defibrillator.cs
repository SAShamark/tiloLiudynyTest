using Gameplay.Effects;
using Gameplay.Entities.Interactive.Other.Station;
using TMPro;
using UnityEngine;

namespace Gameplay.Entities.Interactive.Other.Lab
{
    public sealed class Defibrillator : MonoBehaviour
    {

        private enum Phase { Idle, Charging, Cooldown }

        [SerializeField] private AudioSource _audio;
        [SerializeField] private AudioClip _charge;
        [SerializeField] private AudioClip _zap;
        [SerializeField] private AudioClip _noTarget;

        [SerializeField] private DefibrillatorPaddle _left;
        [SerializeField] private DefibrillatorPaddle _right;
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
        private Phase _phase;
        private float _timer;
        private float _kick;
        private float _flashLeft;
        private string _shownText;
        private float _barWidth;
        private Vector3 _lastRubOffset;
        private bool _rubValid;
        private readonly RendererTint _tint = new();

        public void Initialize(HeartStationControl station)
        {
            _station = station;
            _heartModel = station.ModelRoot;
            _heartRestScale = _heartModel.localScale;
        }

        private void Awake()
        {
            _flash.enabled = false;

            ShowText("READY");
            SetBar(0f, _readyColor);
        }

        private void Update()
        {
            AnimateEffects(Time.deltaTime);
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

        private void UpdateRubbing(bool bothHeld)
        {
            Vector3 offset = _left.ContactPoint - _right.ContactPoint;
            bool wasValid = _rubValid;
            _rubValid = bothHeld;
            Vector3 previous = _lastRubOffset;
            _lastRubOffset = offset;
            if (!bothHeld || !wasValid || Time.deltaTime <= 0f)
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

        private bool IsTouching(DefibrillatorPaddle paddle) =>
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
            _sparks.transform.position = heart;
            _sparks.Play(true);


            _flash.transform.position = heart;
            _flashLeft = 0.25f;


            _kick = 1f;
            _station.ReceiveShock();
        }

        private void AnimateEffects(float deltaTime)
        {
            _flashLeft = Mathf.Max(0f, _flashLeft - deltaTime);
            _flash.enabled = _flashLeft > 0f;
            _flash.intensity = _flashIntensity * (_flashLeft / 0.25f);


            if (_kick <= 0f)
                return;

            _kick = Mathf.Max(0f, _kick - deltaTime * 2.2f);
            float wobble = Mathf.Sin((1f - _kick) * Mathf.PI * 4f) * _kick;
            _heartModel.localScale = _heartRestScale * (1f + (_heartKick - 1f) * wobble);
        }

        private void ShowText(string text)
        {
            if (_shownText == text)
                return;

            _shownText = text;
            _screen.text = text;
        }

        private void SetBar(float amount, Color color)
        {
            if (_barWidth <= 0f)
                _barWidth = _chargeBar.transform.localScale.x;

            Vector3 scale = _chargeBar.transform.localScale;
            scale.x = Mathf.Max(0.001f, amount) * _barWidth;
            _chargeBar.transform.localScale = scale;
            _tint.SetEmission(_chargeBar, color * 2.5f);
        }
    }
}
