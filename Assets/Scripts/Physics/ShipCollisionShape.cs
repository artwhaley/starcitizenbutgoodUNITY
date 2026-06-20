using UnityEngine;

namespace FlightModel
{
    /// <summary>
    /// Pure-data collision primitive in ship-root (COG) local space.
    /// Convex mesh primitives reference an authored collider by index in
    /// <see cref="ShipCollisionShapeSet.meshCastColliderIndices"/>.
    /// </summary>
    public struct ShipCollisionShape
    {
        public ShipCollisionPrimitiveKind kind;
        public Vector3 localCenter;
        public Quaternion localRotation;
        public Vector3 halfExtents;
        public float radius;
        public float height;
        public int capsuleAxis;
        public int meshCastColliderIndex;
    }
}
