using System;

namespace Game.Entities.Organs
{
    /// <summary>
    /// Anything the player uses to pick an organ: the world-space panel or the buttons on the dispenser.
    /// </summary>
    public interface IOrganSelector
    {
        event Action<OrganConfig> OnOrganSelected;

        void Highlight(OrganConfig organ);
    }
}
