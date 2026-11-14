using Game.Entities.Environment;
using Game.Entities.Pickups;
using Services.Audio;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace Game.Entities.Lab
{
    /// <summary>
    /// Digital microscope: put a blood sample on the stage and the round viewer shows magnified
    /// red blood cells drifting past. The viewer stays dark until a sample is in place.
    /// </summary>
    public sealed class Microscope : MonoBehaviour
    {
        private static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");

        [SerializeField] private XRSocketInteractor _stage;
        [Tooltip("Particle system that renders the magnified cells inside the viewer.")]
        [SerializeField] private ParticleSystem _cells;
        [SerializeField] private Renderer _viewerScreen;
        [SerializeField] private Color _screenOff = new(0.02f, 0.03f, 0.04f);
        [SerializeField] private Color _screenOn = new(0.9f, 0.55f, 0.45f);
        [SerializeField] private Light _stageLight;
        [SerializeField, Min(0.1f)] private float _fadeSpeed = 2.5f;

        private MaterialPropertyBlock _block;
        private AudioSource _audio;
        private AudioClip _click;
        private Mesh _cellMesh;
        private float _brightness;
        private bool _hasSample;

        private void Awake()
        {
            _block = new MaterialPropertyBlock();
            _click = ProceduralTones.CreateBeep("Microscope focus", 880f, 0.08f);
            _audio = gameObject.AddComponent<AudioSource>();
            _audio.playOnAwake = false;
            _audio.spatialBlend = 1f;
            _audio.maxDistance = 4f;

            if (_cells != null)
            {
                _cellMesh = BloodCellMesh.Create(6, 14);
                var renderer = _cells.GetComponent<ParticleSystemRenderer>();
                renderer.renderMode = ParticleSystemRenderMode.Mesh;
                renderer.mesh = _cellMesh;
                _cells.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }

            Apply();
        }

        private void OnEnable()
        {
            _stage.selectEntered.AddListener(HandleSampleInserted);
            _stage.selectExited.AddListener(HandleSampleRemoved);
        }

        private void OnDisable()
        {
            _stage.selectEntered.RemoveListener(HandleSampleInserted);
            _stage.selectExited.RemoveListener(HandleSampleRemoved);
        }

        private void OnDestroy()
        {
            Destroy(_click);
            if (_cellMesh != null)
                Destroy(_cellMesh);
        }

        private void Update()
        {
            float target = _hasSample ? 1f : 0f;
            if (Mathf.Approximately(_brightness, target))
                return;

            _brightness = Mathf.MoveTowards(_brightness, target, Time.deltaTime * _fadeSpeed);
            Apply();
        }

        private void HandleSampleInserted(SelectEnterEventArgs args)
        {
            _hasSample = true;
            if (args.interactableObject.transform.TryGetComponent(out Pickup sample))
                sample.ConfirmInsertion(true);

            _audio.PlayOneShot(_click, 0.6f);
            if (_cells != null)
            {
                _cells.Clear(true);
                _cells.Simulate(2f, true, true);
                _cells.Play(true);
            }
        }

        private void HandleSampleRemoved(SelectExitEventArgs _)
        {
            _hasSample = false;
            if (_cells != null)
                _cells.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        private void Apply()
        {
            if (_stageLight != null)
            {
                _stageLight.enabled = _brightness > 0.01f;
                _stageLight.intensity = 0.6f * _brightness;
            }

            if (_viewerScreen == null)
                return;

            _viewerScreen.GetPropertyBlock(_block);
            _block.SetColor(EmissionId, Color.Lerp(_screenOff, _screenOn, _brightness) * (0.3f + 0.7f * _brightness));
            _viewerScreen.SetPropertyBlock(_block);
        }
    }
}
