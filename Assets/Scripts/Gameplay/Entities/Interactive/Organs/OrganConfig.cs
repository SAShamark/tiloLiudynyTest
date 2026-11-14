using UnityEngine;

namespace Gameplay.Entities.Interactive.Organs
{
    [CreateAssetMenu(fileName = "Organ", menuName = "ScriptableObjects/Organ")]
    public sealed class OrganConfig : ScriptableObject
    {
        [SerializeField] private string _displayName = "Organ";
        [SerializeField] private GameObject _model;
        [SerializeField] private Material _hologramMaterial;
        [SerializeField, Min(0.05f)] private float _projectionSize = 0.4f;
        [SerializeField] private Vector3 _rotationOffset;
        [SerializeField] private bool _beats;

        public string DisplayName => _displayName;
        public GameObject Model => _model;
        public Material HologramMaterial => _hologramMaterial;
        public float ProjectionSize => _projectionSize;
        public Quaternion RotationOffset => Quaternion.Euler(_rotationOffset);
        public bool Beats => _beats;
    }
}
