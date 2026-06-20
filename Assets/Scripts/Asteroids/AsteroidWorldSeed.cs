namespace FlightModel.Asteroids
{
    [System.Serializable]
    public struct AsteroidWorldSeed : System.IEquatable<AsteroidWorldSeed>
    {
        public int Value;

        public AsteroidWorldSeed(int value) => Value = value;

        public bool Equals(AsteroidWorldSeed other) => Value == other.Value;
        public override bool Equals(object obj) => obj is AsteroidWorldSeed other && Equals(other);
        public override int GetHashCode() => Value;
        public override string ToString() => Value.ToString();

        public static bool operator ==(AsteroidWorldSeed left, AsteroidWorldSeed right) => left.Equals(right);
        public static bool operator !=(AsteroidWorldSeed left, AsteroidWorldSeed right) => !left.Equals(right);
    }
}
