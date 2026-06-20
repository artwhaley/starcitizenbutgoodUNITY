namespace FlightModel
{
    public struct ShipCollisionMask
    {
        public int layerMask;

        public static ShipCollisionMask StationAndMineableAsteroid =>
            new() { layerMask = (1 << 8) | (1 << 11) };
    }
}
