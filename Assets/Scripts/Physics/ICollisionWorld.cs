using UnityEngine;

namespace FlightModel
{
    public interface ICollisionWorld
    {
        bool SweepShip(
            in ShipCollisionShapeSet shapes,
            Vector3 fromPosition,
            Quaternion fromRotation,
            Vector3 toPosition,
            Quaternion toRotation,
            in ShipCollisionMask mask,
            out ShipCollisionHit hit);

        bool ComputeShipPenetration(
            in ShipCollisionShapeSet shapes,
            Vector3 position,
            Quaternion rotation,
            in ShipCollisionMask mask,
            out ShipCollisionHit hit);
    }
}
