namespace FlightModel
{
    /// <summary>
    /// Placeholder hook for future autodock/autopilot request submission.
    /// </summary>
    public sealed class ShipAutopilotRequestSource : IShipControlRequestSource
    {
        public ShipControlRequestSourceId SourceId => ShipControlRequestSourceId.External;

        public bool TryBuildRequest(
            in ShipState state,
            in ShipTuning tuning,
            in ShipControlRequest pilotRequest,
            out ShipControlRequest request)
        {
            request = default;
            return false;
        }
    }
}
