using UnityEngine;

namespace FlightModel
{
    public struct ShipCollisionHit
    {
        public bool hasHit;
        public Vector3 point;
        public Vector3 normal;
        public float distance;
        public int layer;
    }
}
