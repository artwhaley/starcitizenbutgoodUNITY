namespace FlightModel.Asteroids
{
    [System.Serializable]
    public struct AsteroidDescriptorId : System.IEquatable<AsteroidDescriptorId>
    {
        public long Value;
        public bool IsValid => Value != 0;

        public AsteroidDescriptorId(long value) => Value = value;

        public static AsteroidDescriptorId From(AsteroidWorldSeed seed, AsteroidSectorCoord sector, int localIndex)
        {
            ulong hash = AsteroidHash.Combine(seed.Value, sector.x, sector.y, sector.z, localIndex);
            long value = (long)(hash & 0x7FFFFFFFFFFFFFFFul);
            return new AsteroidDescriptorId(value == 0 ? 1 : value);
        }

        public bool Equals(AsteroidDescriptorId other) => Value == other.Value;
        public override bool Equals(object obj) => obj is AsteroidDescriptorId other && Equals(other);
        public override int GetHashCode() => Value.GetHashCode();
        public override string ToString() => IsValid ? Value.ToString("X16") : "Invalid";

        public static bool operator ==(AsteroidDescriptorId left, AsteroidDescriptorId right) => left.Equals(right);
        public static bool operator !=(AsteroidDescriptorId left, AsteroidDescriptorId right) => !left.Equals(right);

        public static readonly AsteroidDescriptorId Invalid = new(0);
    }
}
