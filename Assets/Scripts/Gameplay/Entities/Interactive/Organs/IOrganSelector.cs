using System;

namespace Gameplay.Entities.Interactive.Organs
{
    public interface IOrganSelector
    {
        event Action<OrganConfig> OrganSelected;

        void Highlight(OrganConfig organ);
    }
}
