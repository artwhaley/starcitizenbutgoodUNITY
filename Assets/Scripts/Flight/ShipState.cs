using UnityEngine;

namespace FlightModel
{
    public struct ShipAppliedOutput
    {
        public Vector3 requestedLocalLinear;
        public Vector3 appliedLocalLinear;
        public Vector3 requestedLocalAngular;
        public Vector3 appliedLocalAngular;
    }

    public struct ShipState
    {
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 linearVelocity;
        public Vector3 angularVelocityRadians;
        public FlightAssistMode assistMode;
        public string frameId;

        public bool boostActive;
        public bool fineControlActive;

        public float currentMassKg;
        public float remainingFuelKg;
        public float remainingHypergolicKg;

        public ShipAppliedOutput appliedOutput;
    }
}
