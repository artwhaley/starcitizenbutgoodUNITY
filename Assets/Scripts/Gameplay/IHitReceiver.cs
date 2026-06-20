using UnityEngine;

namespace FlightModel
{
    public struct HitEvent
    {
        public int projectileId;
        public int ownerEntityId;
        public Vector3 point;
        public Vector3 normal;
        public float damage;
    }

    public interface IHitReceiver
    {
        void ApplyHit(in HitEvent hit);
    }
}
