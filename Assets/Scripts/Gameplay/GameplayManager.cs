using System;
using Gameplay.Entities.Environment;
using Gameplay.Entities.Interactive.Lab;
using Gameplay.Entities.Interactive.Organs;
using Gameplay.Entities.Interactive.Power;
using Gameplay.Entities.Interactive.Station;
using Gameplay.Entities.Participant;
using Gameplay.Entities.Player;
using UnityEngine;

namespace Gameplay
{
    [Serializable]
    public class GameplayManager
    {
        [Header("Station")]
        [SerializeField] private HeartStationControl _heartStation;
        [SerializeField] private Probe _probe;
        [SerializeField] private PowerUnit _powerUnit;
        [SerializeField] private EcgMonitor[] _ecgMonitors;

        [Header("Organs")]
        [SerializeField] private FlaskButtonPanel _flaskButtons;
        [SerializeField] private FlaskDispenser _flaskDispenser;

        [Header("Lab")]
        [SerializeField] private Defibrillator _defibrillator;

        [Header("Characters")]
        [SerializeField] private ParticipantControl _participant;

        private OrganSelectionController _organSelection;

        public void Initialize(PlayerControl player)
        {
            _participant.Initialize(player.Rig, player.Settings);

            _heartStation.Initialize(_probe, _powerUnit, _participant);
            foreach (var monitor in _ecgMonitors)
            {
                monitor.Initialize(_heartStation);
            }

            _defibrillator.Initialize(_heartStation);

            _flaskDispenser.Initialize();
            _organSelection = new OrganSelectionController(_flaskButtons, _flaskDispenser, _heartStation);
            _organSelection.Initialize();
        }

        public void Dispose()
        {
            _organSelection.Dispose();
        }
    }
}
