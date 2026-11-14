using Gameplay.Effects;
using Gameplay.Entities.Environment.Background;
using UnityEngine;
using Gameplay.Entities.Interactive.BaseLogic.Items;

namespace Gameplay.Entities.Interactive.Other.Lab
{
    public sealed class Microscope : MonoBehaviour
    {

        [SerializeField] private AudioSource _audio;
        [SerializeField] private AudioClip _click;
        [SerializeField] private ItemSocket<BloodSample> _stage = new();
        [SerializeField] private BloodSample _sample;
        [Tooltip("Ring on the stage that shows where to put the sample while it is in the hand.")]
        [SerializeField] private GameObject _stageHint;
        [Tooltip("Cells drifting in front of the view camera, far away from the room.")]
        [SerializeField] private ParticleSystem _cells;
        [Tooltip("Renders the magnified cells into the texture shown on the monitor and in the eyepieces.")]
        [SerializeField] private Camera _viewCamera;
        [Tooltip("Monitor screen and eyepiece lenses that show the view texture.")]
        [SerializeField] private Renderer[] _screens;
        [SerializeField] private Light _stageLight;
        [SerializeField, Min(0.1f)] private float _fadeSpeed = 2.5f;

        private Mesh _cellMesh;
        private float _brightness = -1f;
        private bool _hasSample;
        private readonly RendererTint _tint = new();

        private void Awake()
        {
            _cellMesh = BloodCellMesh.Create(6, 14);
            var cellsRenderer = _cells.GetComponent<ParticleSystemRenderer>();
            cellsRenderer.renderMode = ParticleSystemRenderMode.Mesh;
            cellsRenderer.mesh = _cellMesh;
            _cells.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

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

        private void OnDestroy()
        {
            Destroy(_cellMesh);
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
