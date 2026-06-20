namespace FlightModel
{
    /// <summary>
    /// Telemetry produced by a single ShipSimulator.Step() call,
    /// consumed by HUD and VFX presentation.
    /// </summary>
    public struct ShipSimulationTelemetry
    {
        public ShipControlRequest pilotRequest;
        public ShipControlRequest assistRequest;
        public ShipControlRequest brakeRequest;
        public ShipControlRequest mergedControlRequest;
        public ShipThrusterOutput thrusterOutput;
        public ShipInputCommand appliedThrusterCommand;
        public bool collisionResolved;
        public bool collisionDepenetrated;
        public float collisionDepenetrationDistance;
        public ShipCollisionHit collisionHit;
    }
}
