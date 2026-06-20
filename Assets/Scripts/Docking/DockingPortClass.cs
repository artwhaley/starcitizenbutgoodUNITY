namespace FlightModel.Docking
{
    /// <summary>
    /// Coarse classification of station docking port sizes/categories. The first
    /// implementation does not enforce compatibility between ship and port class;
    /// the enum exists so future logic can filter without a broad migration.
    /// </summary>
    public enum DockingPortClass
    {
        Unknown = 0,
        SmallShip = 1,
        MediumShip = 2,
        LargeShip = 3,
        Cargo = 4,
        Maintenance = 5
    }
}
