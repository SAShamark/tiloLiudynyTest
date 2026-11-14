namespace Game.Entities.Station
{
    public enum StationState
    {
        /// <summary>Probe is not plugged in; the station cannot start.</summary>
        Unpowered,

        /// <summary>Probe is plugged in; pulling the lever starts the station.</summary>
        Ready,

        /// <summary>The heart beats in a loop until the lever is pulled again or the probe is removed.</summary>
        Active,
    }
}
