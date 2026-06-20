namespace FlightModel.World
{
    [System.Serializable]
    public struct ReferenceFrameId : System.IEquatable<ReferenceFrameId>
    {
        public int Value;
        public bool IsValid => Value > 0;

        public ReferenceFrameId(int value) => Value = value;

        public bool Equals(ReferenceFrameId other) => Value == other.Value;
        public override bool Equals(object obj) => obj is ReferenceFrameId other && Equals(other);
        public override int GetHashCode() => Value;
        public override string ToString() => IsValid ? Value.ToString() : "Invalid";

        public static bool operator ==(ReferenceFrameId left, ReferenceFrameId right) => left.Equals(right);
        public static bool operator !=(ReferenceFrameId left, ReferenceFrameId right) => !left.Equals(right);

        public static readonly ReferenceFrameId Invalid = new(0);
        public static readonly ReferenceFrameId LocalZone = new(1);
    }
}
