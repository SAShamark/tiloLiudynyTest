namespace Game.Safety
{
    /// <summary>
    /// Reports whether interactions must pause because someone is dangerously close.
    /// </summary>
    public interface ISafetyGate
    {
        bool IsStopActive { get; }
    }
}
