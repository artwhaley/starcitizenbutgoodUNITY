using System;

namespace FlightModel
{
    public readonly struct ShipCollisionShapeSet
    {
        public static readonly ShipCollisionShapeSet Empty = new(Array.Empty<ShipCollisionShape>(), Array.Empty<int>());

        public ShipCollisionShape[] shapes { get; }
        public int[] meshCastColliderIndices { get; }

        public ShipCollisionShapeSet(ShipCollisionShape[] shapes, int[] meshCastColliderIndices)
        {
            this.shapes = shapes ?? Array.Empty<ShipCollisionShape>();
            this.meshCastColliderIndices = meshCastColliderIndices ?? Array.Empty<int>();
        }

        public int Count => shapes.Length;
        public bool IsEmpty => shapes.Length == 0;
    }
}
