using System;
using Game.Entities.Station;

namespace Game.Entities.Organs
{
    /// <summary>
    /// Connects the organ panel to the flask dispenser and the station: a click slides that organ's flask out
    /// of the cabinet, and the organ currently projected by the station is highlighted on the panel.
    /// </summary>
    public sealed class OrganSelectionController : IDisposable
    {
        private readonly OrganPanel _panel;
        private readonly FlaskDispenser _dispenser;
        private readonly HeartStationControl _station;

        public OrganSelectionController(OrganPanel panel, FlaskDispenser dispenser, HeartStationControl station)
        {
            _panel = panel;
            _dispenser = dispenser;
            _station = station;
        }

        public void Init()
        {
            _panel.OnOrganSelected += _dispenser.Present;
            _station.OnOrganChanged += _panel.Highlight;
            _panel.Highlight(_station.SelectedOrgan);
        }

        public void Dispose()
        {
            _panel.OnOrganSelected -= _dispenser.Present;
            _station.OnOrganChanged -= _panel.Highlight;
        }
    }
}
