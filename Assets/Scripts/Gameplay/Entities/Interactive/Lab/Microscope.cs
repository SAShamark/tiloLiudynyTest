using Gameplay.Entities.Environment.Background;
using Gameplay.Entities.Interactive.Pickups;
using Services.Audio;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace Gameplay.Entities.Interactive.Lab
{
    public sealed class Microscope : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [SerializeField] private AudioSource _audio;
        [SerializeField] private XRSocketInteractor _stage;
        [SerializeField] private Pickup _sample;
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

        private AudioClip _click;
        private Mesh _cellMesh;
        private float _brightness = -1f;
        private bool _hasSample;
        private MaterialPropertyBlock _propertyBlock;

        private void Awake()
        {
            _click = ProceduralTones.CreateBeep("Microscope focus", 880f, 0.08f);

            _cellMesh = BloodCellMesh.Create(6, 14);
            var cellsRenderer = _cells.GetComponent<ParticleSystemRenderer>();
            cellsRenderer.renderMode = ParticleSystemRenderMode.Mesh;
            cellsRenderer.mesh = _cellMesh;
            _cells.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            SetBrightness(0f);
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

        private void HandleSampleInserted(SelectEnterEventArgs args)
        {
            _hasSample = true;
            if (args.interactableObject.transform.TryGetComponent(out Pickup sample))
            {
                sample.ConfirmInsertion(true);
            }

            _audio.PlayOneShot(_click, 0.6f);
            _cells.Clear(true);
            _cells.Simulate(4f, true, true);
            _cells.Play(true);
        }

        private void HandleSampleRemoved(SelectExitEventArgs _)
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

            _propertyBlock ??= new MaterialPropertyBlock();
            foreach (var screen in _screens)
            {
                screen.GetPropertyBlock(_propertyBlock);
                _propertyBlock.SetColor(BaseColorId, Color.white * brightness);
                screen.SetPropertyBlock(_propertyBlock);
            }
        }
    }
}
