using UnityEngine;

namespace FlightModel.Asteroids
{
    [System.Serializable]
    public struct AsteroidSectorCoord : System.IEquatable<AsteroidSectorCoord>
    {
        public int x;
        public int y;
        public int z;

        public AsteroidSectorCoord(int x, int y, int z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public static AsteroidSectorCoord FromWorldPosition(Vector3 worldPosition, float sectorSizeMeters)
        {
            float safeSectorSize = Mathf.Max(0.001f, sectorSizeMeters);
            return new AsteroidSectorCoord(
                Mathf.FloorToInt(worldPosition.x / safeSectorSize),
                Mathf.FloorToInt(worldPosition.y / safeSectorSize),
                Mathf.FloorToInt(worldPosition.z / safeSectorSize));
        }

        public Vector3 GetMinCorner(float sectorSizeMeters)
        {
            return new Vector3(x * sectorSizeMeters, y * sectorSizeMeters, z * sectorSizeMeters);
        }

        public bool Equals(AsteroidSectorCoord other) => x == other.x && y == other.y && z == other.z;
        public override bool Equals(object obj) => obj is AsteroidSectorCoord other && Equals(other);
        public override int GetHashCode() => unchecked((x * 397) ^ (y * 251) ^ z);
        public override string ToString() => $"({x}, {y}, {z})";

        public static bool operator ==(AsteroidSectorCoord left, AsteroidSectorCoord right) => left.Equals(right);
        public static bool operator !=(AsteroidSectorCoord left, AsteroidSectorCoord right) => !left.Equals(right);
    }
}
