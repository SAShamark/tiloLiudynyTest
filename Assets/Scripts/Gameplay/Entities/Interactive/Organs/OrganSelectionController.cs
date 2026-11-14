using System;
using Gameplay.Entities.Interactive.Station;

namespace Gameplay.Entities.Interactive.Organs
{
    public sealed class OrganSelectionController : IDisposable
    {
        private readonly IOrganSelector _panel;
        private readonly FlaskDispenser _dispenser;
        private readonly HeartStationControl _station;

        public OrganSelectionController(IOrganSelector panel, FlaskDispenser dispenser, HeartStationControl station)
        {
            _panel = panel;
            _dispenser = dispenser;
            _station = station;
        }

        public void Initialize()
        {
            _station.SetSwappableFlasks(_dispenser.Flasks);
            _panel.OrganSelected += _dispenser.Toggle;
            _dispenser.Changed += RefreshHighlight;
            _station.OrganChanged += HandleOrganChanged;
            _station.FlaskEjected += _dispenser.Return;
            RefreshHighlight();
        }

        public void Dispose()
        {
            _panel.OrganSelected -= _dispenser.Toggle;
            _dispenser.Changed -= RefreshHighlight;
            _station.OrganChanged -= HandleOrganChanged;
            _station.FlaskEjected -= _dispenser.Return;
        }

        private void HandleOrganChanged(OrganConfig _) => RefreshHighlight();

        private void RefreshHighlight() => _panel.Highlight(_dispenser.PresentedOrgan ?? _station.SelectedOrgan);
    }
}
