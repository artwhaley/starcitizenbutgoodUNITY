namespace FlightModel
{
    public interface IShipControlRequestSource
    {
        ShipControlRequestSourceId SourceId { get; }

        bool TryBuildRequest(
            in ShipState state,
            in ShipTuning tuning,
            in ShipControlRequest pilotRequest,
            out ShipControlRequest request);
    }
}
