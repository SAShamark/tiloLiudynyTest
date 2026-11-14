using Gameplay.Entities.Interactive.Station;
using UnityEngine;

namespace Gameplay.Entities.Interactive.Organs
{
    [RequireComponent(typeof(HologramProjection))]
    public sealed class OrganProjector : MonoBehaviour
    {
        [SerializeField] private Transform _modelRoot;

        private HologramProjection _hologram;
        private OrganConfig _shown;
        private OrganConfig _requested;
        private GameObject _model;
        private bool _powered;
        private bool _materialized;

        public OrganConfig Shown => _shown;
        public Transform ModelRoot => _modelRoot;

        private void Awake()
        {
            _hologram = GetComponent<HologramProjection>();
        }

        public void SetOrgan(OrganConfig organ) => _requested = organ;

        public void SetPowered(bool powered) => _powered = powered;

        public void SetMaterialized(bool materialized)
        {
            _materialized = materialized;
            _hologram.SetMaterialized(materialized);
        }

        private void Update()
        {
            if (_requested != _shown)
            {
                _hologram.Hide();
                if (_hologram.IsHidden)
                    Build(_requested);
                return;
            }

            if (_powered && _shown != null)
                _hologram.Show();
            else
                _hologram.Hide();
        }

        private void Build(OrganConfig organ)
        {
            if (_model != null)
                Destroy(_model);

            _model = null;
            _shown = organ;
            if (organ == null || organ.Model == null)
            {
                _hologram.SetRenderers(System.Array.Empty<Renderer>());
                return;
            }

            transform.localRotation = Quaternion.identity;
            _model = OrganModelFactory.Spawn(organ, _modelRoot, organ.ProjectionSize, out var renderers);
            _hologram.SetRenderers(renderers);
            _hologram.SetMaterialized(_materialized);
        }
    }
}
