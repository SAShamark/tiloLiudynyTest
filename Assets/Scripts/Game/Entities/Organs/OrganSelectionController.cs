using System;
using Game.Entities.Station;

namespace Game.Entities.Organs
{
    /// <summary>
    /// Organ panel works as a toggle group over the flask dispenser:
    /// a button rolls that organ's flask out, pressing it again rolls it back in.
    /// The highlighted button is the flask waiting on the tray, otherwise the organ in the station.
    /// Bringing a new flask to the station ejects the old one, which flies back into the dispenser.
    /// </summary>
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

        public void Init()
        {
            _station.SetSwappableFlasks(_dispenser.Flasks);
            _panel.OnOrganSelected += _dispenser.Toggle;
            _dispenser.OnChanged += RefreshHighlight;
            _station.OnOrganChanged += HandleOrganChanged;
            _station.OnFlaskEjected += _dispenser.Return;
            RefreshHighlight();
        }

        public void Dispose()
        {
            _panel.OnOrganSelected -= _dispenser.Toggle;
            _dispenser.OnChanged -= RefreshHighlight;
            _station.OnOrganChanged -= HandleOrganChanged;
            _station.OnFlaskEjected -= _dispenser.Return;
        }

        private void HandleOrganChanged(OrganConfig _) => RefreshHighlight();

        private void RefreshHighlight() => _panel.Highlight(_dispenser.PresentedOrgan ?? _station.SelectedOrgan);
    }
}
