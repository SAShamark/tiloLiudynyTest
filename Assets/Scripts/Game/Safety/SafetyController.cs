using System;
using Game.Comfort;
using Game.Entities.Participants;

namespace Game.Safety
{
    /// <summary>
    /// Enables proximity warnings only while the player walks physically.
    /// Artificial locomotion keeps the body still, so warnings would only distract.
    /// </summary>
    public sealed class SafetyController : IDisposable
    {
        private readonly LocomotionController _locomotion;
        private readonly ParticipantControl _participant;

        public SafetyController(LocomotionController locomotion, ParticipantControl participant)
        {
            _locomotion = locomotion;
            _participant = participant;
        }

        public void Init()
        {
            _locomotion.OnModeChanged += HandleModeChanged;
            HandleModeChanged(_locomotion.Mode);
        }

        public void Dispose()
        {
            _locomotion.OnModeChanged -= HandleModeChanged;
        }

        private void HandleModeChanged(MovementMode mode)
        {
            _participant.SetMonitoring(mode == MovementMode.Physical);
        }
    }
}
