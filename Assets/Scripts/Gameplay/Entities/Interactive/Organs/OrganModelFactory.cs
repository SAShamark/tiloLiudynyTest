using UnityEngine;
using UnityEngine.Rendering;

namespace Gameplay.Entities.Interactive.Organs
{
    public static class OrganModelFactory
    {
        public static GameObject Spawn(OrganConfig organ, Transform parent, float size, out Renderer[] renderers)
        {
            var model = Object.Instantiate(organ.Model, parent, false);
            model.name = organ.DisplayName;
            model.transform.localRotation = organ.RotationOffset;
            model.transform.localScale = Vector3.one;

            foreach (var collider in model.GetComponentsInChildren<Collider>(true))
                Object.Destroy(collider);
            foreach (var animation in model.GetComponentsInChildren<Animation>(true))
                animation.enabled = false;
            foreach (var animator in model.GetComponentsInChildren<Animator>(true))
                animator.enabled = false;

            renderers = model.GetComponentsInChildren<Renderer>(true);
            foreach (var target in renderers)
            {
                target.enabled = true;
                var materials = new Material[target.sharedMaterials.Length];
                for (int i = 0; i < materials.Length; i++)
                    materials[i] = organ.HologramMaterial;
                target.sharedMaterials = materials;
                target.shadowCastingMode = ShadowCastingMode.Off;
                target.receiveShadows = false;
            }

            Fit(model.transform, parent, renderers, size);
            return model;
        }

        private static void Fit(Transform model, Transform parent, Renderer[] renderers, float size)
        {
            if (renderers.Length == 0)
                return;

            Bounds bounds = GetBounds(renderers);
            float largest = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
            if (largest <= 0f)
                return;

            model.localScale *= size / largest;
            bounds = GetBounds(renderers);
            model.position += parent.position - bounds.center;
        }

        private static Bounds GetBounds(Renderer[] renderers)
        {
            Bounds bounds = renderers[0].bounds;
            foreach (var target in renderers)
                bounds.Encapsulate(target.bounds);
            return bounds;
        }
    }
}
