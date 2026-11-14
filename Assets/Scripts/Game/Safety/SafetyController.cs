using System;
using Game.Comfort;
using Game.Entities.Participants;

namespace Game.Safety
{
    /// <summary>
    /// Enables proximity warnings while the player walks physically, and also during stick
    /// movement when the player turns that on in the comfort menu (the virtual body can still
    /// walk into the other participant). Teleport never warns.
    /// </summary>
    public sealed class SafetyController : IDisposable
    {
        private readonly ComfortMenu _menu;
        private readonly ParticipantControl _participant;

        public SafetyController(ComfortMenu menu, ParticipantControl participant)
        {
            _menu = menu;
            _participant = participant;
        }

        public void Init()
        {
            _menu.Locomotion.OnModeChanged += HandleModeChanged;
            _menu.OnStickWarningsChanged += HandleStickWarningsChanged;
            Refresh();
        }

        public void Dispose()
        {
            _menu.Locomotion.OnModeChanged -= HandleModeChanged;
            _menu.OnStickWarningsChanged -= HandleStickWarningsChanged;
        }

        private void HandleModeChanged(MovementMode mode) => Refresh();

        private void HandleStickWarningsChanged(bool enabled) => Refresh();

        private void Refresh()
        {
            MovementMode mode = _menu.Locomotion.Mode;
            bool monitoring = mode == MovementMode.Physical
                || (mode == MovementMode.Stick && _menu.StickWarnings);
            _participant.SetMonitoring(monitoring);
        }
    }
}
