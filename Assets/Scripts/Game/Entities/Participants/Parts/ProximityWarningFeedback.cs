using System;
using Services.Audio;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace Game.Entities.Participants.Parts
{
    /// <summary>
    /// Directional danger cues: a 3D alarm that comes from the participant's head, a red glow on the side of the
    /// view the participant is on, and a pulsing ring on the floor that marks the zone to keep out of.
    /// </summary>
    public sealed class ProximityWarningFeedback : IDisposable
    {
        private const string VignetteShader = "ProximityVignette";
        private const int RingSegments = 48;
        private static readonly int StrengthId = Shader.PropertyToID("_Strength");
        private static readonly int DirectionId = Shader.PropertyToID("_Direction");
        private static readonly int FocusId = Shader.PropertyToID("_Focus");
        private static readonly int PulseId = Shader.PropertyToID("_Pulse");
        private static readonly Color NearbyColor = new(1f, 0.75f, 0.2f);
        private static readonly Color StopColor = new(1f, 0.12f, 0.08f);

        private readonly AudioSource _alarm;
        private readonly AudioClip _clip;
        private readonly Material _vignette;
        private readonly GameObject _overlay;
        private readonly LineRenderer _ring;
        private readonly Material _ringMaterial;

        /// <param name="soundSource">Where the alarm plays from (the participant's head).</param>
        /// <param name="floorAnchor">Participant root; the zone ring is drawn around it on the floor.</param>
        public ProximityWarningFeedback(Transform soundSource, Transform floorAnchor, Transform head, float zoneRadius)
        {
            _clip = ProceduralTones.CreateTwoToneAlarm("Participant proximity alarm");
            _alarm = soundSource.gameObject.AddComponent<AudioSource>();
            _alarm.playOnAwake = false;
            _alarm.loop = true;
            _alarm.spatialBlend = 1f;
            _alarm.spread = 0f;
            _alarm.dopplerLevel = 0f;
            _alarm.minDistance = 0.6f;
            _alarm.maxDistance = 6f;
            _alarm.rolloffMode = AudioRolloffMode.Logarithmic;
            _alarm.volume = 0f;
            _alarm.clip = _clip;

            _ring = CreateRing(floorAnchor, zoneRadius, out _ringMaterial);

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

        /// <param name="angle">Signed yaw from the viewer's forward to the participant (degrees, + = right).</param>
        public void Apply(float intensity, float angle, bool stop)
        {
            bool active = intensity > 0.005f;
            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * (stop ? 14f : 7f));

            _alarm.volume = 0.85f * intensity;
            _alarm.pitch = stop ? 1.12f : 1f;
            if (active && !_alarm.isPlaying)
                _alarm.Play();
            else if (!active && _alarm.isPlaying)
                _alarm.Stop();

            UpdateRing(intensity, stop, pulse);

            if (_overlay == null)
                return;

            _overlay.SetActive(active);
            if (!active)
                return;

            // Ahead or to a side: light that edge. Behind: a ring around the whole view, because no edge is right.
            float radians = angle * Mathf.Deg2Rad;
            var direction = new Vector4(Mathf.Sin(radians), 0f, 0f, 0f);
            float behind = Mathf.InverseLerp(120f, 170f, Mathf.Abs(angle));
            float ahead = 1f - Mathf.InverseLerp(10f, 35f, Mathf.Abs(angle));
            _vignette.SetFloat(StrengthId, intensity);
            _vignette.SetVector(DirectionId, direction);
            _vignette.SetFloat(FocusId, Mathf.Clamp01(1f - behind - ahead * 0.6f));
            _vignette.SetFloat(PulseId, stop ? pulse : 0f);
        }

        public void Dispose()
        {
            if (_overlay != null)
                Object.Destroy(_overlay);
            if (_vignette != null)
                Object.Destroy(_vignette);
            if (_clip != null)
                Object.Destroy(_clip);
            if (_ringMaterial != null)
                Object.Destroy(_ringMaterial);
        }

        private void UpdateRing(float intensity, bool stop, float pulse)
        {
            if (_ring == null)
                return;

            bool visible = intensity > 0.01f;
            if (_ring.enabled != visible)
                _ring.enabled = visible;
            if (!visible)
                return;

            Color color = stop ? StopColor : NearbyColor;
            color.a = Mathf.Clamp01(intensity * 1.5f) * (stop ? Mathf.Lerp(0.55f, 1f, pulse) : 0.7f);
            _ring.startColor = color;
            _ring.endColor = color;
            _ring.widthMultiplier = stop ? Mathf.Lerp(0.035f, 0.06f, pulse) : 0.03f;
        }

        private static LineRenderer CreateRing(Transform anchor, float radius, out Material material)
        {
            var ringObject = new GameObject("Proximity zone ring");
            ringObject.transform.SetParent(anchor, false);
            ringObject.transform.localPosition = Vector3.up * 0.015f;
            ringObject.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            var ring = ringObject.AddComponent<LineRenderer>();
            ring.useWorldSpace = false;
            ring.loop = true;
            ring.alignment = LineAlignment.TransformZ;
            ring.positionCount = RingSegments;
            ring.shadowCastingMode = ShadowCastingMode.Off;
            ring.receiveShadows = false;
            for (int i = 0; i < RingSegments; i++)
            {
                float a = i * Mathf.PI * 2f / RingSegments;
                ring.SetPosition(i, new Vector3(Mathf.Cos(a) * radius, Mathf.Sin(a) * radius, 0f));
            }

            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            material = shader != null ? new Material(shader) : null;
            if (material != null)
            {
                material.SetFloat("_Surface", 1f);
                material.SetFloat("_Blend", 0f);
                material.SetOverrideTag("RenderType", "Transparent");
                material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                material.SetFloat("_ZWrite", 0f);
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                material.renderQueue = (int)RenderQueue.Transparent;
                ring.sharedMaterial = material;
            }

            ring.enabled = false;
            return ring;
        }
    }
}
