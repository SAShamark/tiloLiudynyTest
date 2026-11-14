using System;
using Services.Audio;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace Game.Entities.Participants.Parts
{
    /// <summary>
    /// Spatial alarm at the participant and a red vignette in front of the viewer's eyes.
    /// </summary>
    public sealed class ProximityWarningFeedback : IDisposable
    {
        private const string VignetteShader = "ProximityVignette";
        private static readonly int StrengthId = Shader.PropertyToID("_Strength");

        private readonly AudioSource _alarm;
        private readonly AudioClip _clip;
        private readonly Material _vignette;
        private readonly GameObject _overlay;

        public ProximityWarningFeedback(GameObject source, Transform head)
        {
            _clip = ProceduralTones.CreateTwoToneAlarm("Participant proximity alarm");
            _alarm = source.AddComponent<AudioSource>();
            _alarm.playOnAwake = false;
            _alarm.loop = true;
            _alarm.spatialBlend = 1f;
            _alarm.dopplerLevel = 0f;
            _alarm.minDistance = 1.8f;
            _alarm.maxDistance = 3f;
            _alarm.rolloffMode = AudioRolloffMode.Linear;
            _alarm.volume = 0f;
            _alarm.clip = _clip;

            var shader = Resources.Load<Shader>(VignetteShader);
            if (shader == null)
            {
                Debug.LogWarning($"Shader '{VignetteShader}' not found in Resources; vignette disabled.");
                return;
            }

            _vignette = new Material(shader);
            _overlay = GameObject.CreatePrimitive(PrimitiveType.Quad);
            _overlay.name = "Proximity vignette";
            _overlay.GetComponent<Collider>().enabled = false;
            _overlay.transform.SetParent(head, false);
            _overlay.transform.localPosition = Vector3.forward * 0.45f;
            _overlay.transform.localScale = new Vector3(2.16f, 2.16f, 1f);
            var renderer = _overlay.GetComponent<Renderer>();
            renderer.sharedMaterial = _vignette;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            _overlay.SetActive(false);
        }

        public void Apply(float intensity)
        {
            bool active = intensity > 0.005f;
            _alarm.volume = 0.78f * intensity;
            if (active && !_alarm.isPlaying)
                _alarm.Play();
            else if (!active && _alarm.isPlaying)
                _alarm.Stop();

            if (_overlay == null)
                return;

            _overlay.SetActive(active);
            _vignette.SetFloat(StrengthId, intensity);
        }

        public void Dispose()
        {
            if (_overlay != null)
                Object.Destroy(_overlay);
            if (_vignette != null)
                Object.Destroy(_vignette);
            if (_clip != null)
                Object.Destroy(_clip);
        }
    }
}
