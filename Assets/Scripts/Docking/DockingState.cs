namespace FlightModel.Docking
{
    /// <summary>
    /// High-level docking state shared by the capture controller and HUD.
    /// Phase 0.1 transitions are: FreeFlight/DockingMode -> MagneticCapture
    /// -> Docked -> Undocking -> RecaptureLockout.
    /// </summary>
    public enum DockingState
    {
        FreeFlight = 0,
        DockingMode = 1,
        MagneticCapture = 2,
        Docked = 3,
        Undocking = 4,
        RecaptureLockout = 5,
    }
}
