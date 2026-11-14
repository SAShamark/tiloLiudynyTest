using System.Collections.Generic;
using UnityEngine;

namespace Gameplay.Entities.Interactive.Organs
{
    [CreateAssetMenu(fileName = "OrganCatalog", menuName = "ScriptableObjects/Organ Catalog")]
    public sealed class OrganCatalog : ScriptableObject
    {
        [SerializeField] private List<OrganConfig> _organs = new();

        public IReadOnlyList<OrganConfig> Organs => _organs;
    }
}
