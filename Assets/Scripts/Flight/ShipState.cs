using FlightModel.World;
using UnityEngine;

namespace FlightModel
{
    public struct ShipAppliedOutput
    {
        public Vector3 requestedLocalLinear;
        public Vector3 appliedLocalLinear;
        public Vector3 requestedLocalAngular;
        public Vector3 appliedLocalAngular;
        public ShipThrusterOutput thrusters;
        public bool mainEngineFuelBlocked;
        public bool hypergolicBlocked;
        public bool linearSpeedCapped;
    }

    public struct ShipState
    {
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 linearVelocity;
        public Vector3 angularVelocityRadians;
        public FlightAssistMode assistMode;
        public ReferenceFrameId frameId;

        public bool boostActive;
        public bool fineControlActive;

        public float currentMassKg;
        public float remainingFuelKg;
        public float remainingHypergolicKg;

        public ShipAppliedOutput appliedOutput;
    }
}
