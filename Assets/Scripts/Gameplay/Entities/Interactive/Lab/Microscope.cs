using Gameplay.Effects;
using UnityEngine;
using Gameplay.Entities.Interactive.Base.Items;

namespace Gameplay.Entities.Interactive.Lab
{
    public sealed class Microscope : MonoBehaviour
    {
        [SerializeField] private AudioSource _audio;
        [SerializeField] private AudioClip _click;
        [SerializeField] private ItemSocket<BloodSample> _stage = new();
        [SerializeField] private BloodSample _sample;
        [SerializeField] private GameObject _stageHint;
        [SerializeField] private ParticleSystem _cells;
        [SerializeField] private Camera _viewCamera;
        [SerializeField] private Renderer[] _screens;
        [SerializeField] private Light _stageLight;
        [SerializeField, Min(0.1f)] private float _fadeSpeed = 2.5f;

        private float _brightness = -1f;
        private bool _hasSample;
        private readonly RendererTint _tint = new();

        private void Awake()
        {
            SetBrightness(0f);
        }

        private void OnEnable()
        {
            _stage.Initialize();
            _stage.Inserted += HandleSampleInserted;
            _stage.Removed += HandleSampleRemoved;
        }

        private void OnDisable()
        {
            _stage.Inserted -= HandleSampleInserted;
            _stage.Removed -= HandleSampleRemoved;
            _stage.Dispose();
        }

        private void Update()
        {
            bool showHint = _sample.IsHeld && !_hasSample;
            if (_stageHint.activeSelf != showHint)
            {
                _stageHint.SetActive(showHint);
            }

            float target = _hasSample ? 1f : 0f;
            if (!Mathf.Approximately(_brightness, target))
            {
                SetBrightness(Mathf.MoveTowards(_brightness, target, Time.deltaTime * _fadeSpeed));
            }
        }

        private void HandleSampleInserted(BloodSample sample)
        {
            _hasSample = true;
            sample.ConfirmInsertion(true);

            _audio.PlayOneShot(_click, 0.6f);
            _cells.Clear(true);
            _cells.Simulate(4f, true, true);
            _cells.Play(true);
        }

        private void HandleSampleRemoved(BloodSample _)
        {
            _hasSample = false;
        }

        private void SetBrightness(float brightness)
        {
            _brightness = brightness;
            bool lit = brightness > 0.01f;
            _viewCamera.enabled = lit;
            _stageLight.enabled = lit;
            _stageLight.intensity = 0.6f * brightness;
            if (!lit)
            {
                _cells.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }

            foreach (var screen in _screens)
            {
                _tint.SetBase(screen, Color.white * brightness);
            }
        }
    }
}
