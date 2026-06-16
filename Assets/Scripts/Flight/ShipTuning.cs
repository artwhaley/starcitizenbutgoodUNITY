using UnityEngine;

namespace FlightModel
{
    [CreateAssetMenu(menuName = "Flight/Ship Tuning", fileName = "ShipTuning")]
    public class ShipTuning : ScriptableObject
    {
        [Header("Mass")]
        public float massKg = 10000f;

        [Header("Thrust (right, up, forward)")]
        public Vector3 maxThrustNewtons = new(3500000f, 3500000f, 6000000f);

        [Header("Torque (pitch, yaw, roll)")]
        public Vector3 maxTorque = new(45000f, 45000f, 45000f);

        [Header("Boost")]
        public float boostMultiplier = 1.75f;

        [Header("Damping - Angular Assist")]
        public float angularDampingStrength = 2f;

        [Header("Damping - Brake")]
        public float brakeLinearDampingStrength = 6f;
        public float brakeAngularDampingStrength = 12f;

        [Header("Damping - Coupled")]
        public float coupledLateralDampingStrength = 1.5f;

        [Header("Damping - Frame Lock")]
        public float frameLockLinearDampingStrength = 1.5f;
    }
}
