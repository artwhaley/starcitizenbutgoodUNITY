namespace FlightModel.Docking
{
    /// <summary>
    /// Lifecycle state for both ship and station docking ports. Phase 0.1 transitions
    /// instantly between <see cref="Retracted"/> and <see cref="Deployed"/>; the
    /// enum includes intermediate states so future animated ports can move through
    /// them without changing capture/dock logic.
    /// </summary>
    public enum DockingPortDeploymentState
    {
        Retracted = 0,
        Deploying = 1,
        Deployed = 2,
        Retracting = 3,
        Disabled = 4
    }
}
