using UnityEngine;

namespace FlightModel
{
    public struct ShipState
    {
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 linearVelocity;
        public Vector3 angularVelocityRadians;
        public FlightAssistMode assistMode;
        public string frameId;
    }
}
