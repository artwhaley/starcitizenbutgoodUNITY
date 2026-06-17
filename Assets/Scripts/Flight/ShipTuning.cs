using UnityEngine;
using UnityEngine.Serialization;

namespace FlightModel
{
    [CreateAssetMenu(menuName = "Flight/Ship Tuning", fileName = "ShipTuning")]
    public class ShipTuning : ScriptableObject
    {
        [Header("Mass")]
        [FormerlySerializedAs("massKg")]
        public float dryMassKg = 10000f;

        [Header("Linear Acceleration (m/s² at dry mass)")]
        public float mainEngineForwardAccel = 600f;
        public float maneuverForwardAccel = 600f;
        public float reverseAccel = 600f;
        public float rightAccel = 350f;
        public float leftAccel = 350f;
        public float upAccel = 350f;
        public float downAccel = 350f;

        [Header("Angular Acceleration (rad/s² at dry mass)")]
        public float pitchPositiveAccel = 4.5f;
        public float pitchNegativeAccel = 4.5f;
        public float yawPositiveAccel = 4.5f;
        public float yawNegativeAccel = 4.5f;
        public float rollPositiveAccel = 4.5f;
        public float rollNegativeAccel = 4.5f;

        [Header("Linear Speed Limits (m/s)")]
        public float maxLinearSpeedMps = 800f;
        public float boostMaxLinearSpeedMps = 1400f;
        public float boostAccelMultiplier = 1.75f;

        [Header("Angular Speed Limits (rad/s)")]
        public float maxPitchSpeedRad = 2f;
        public float maxYawSpeedRad = 2f;
        public float maxRollSpeedRad = 3f;
        public float boostAngularSpeedMultiplier = 1.75f;

        [Header("Fine Control Limits")]
        [Range(0.05f, 1f)]
        public float fineControlLinearAccelMultiplier = 0.25f;
        public float fineControlMaxLinearSpeedMps = 50f;
        [Range(0.05f, 1f)]
        public float fineControlAngularAccelMultiplier = 0.25f;

        [Header("Propellant")]
        public float fuelCapacityKg = 5000f;
        public float hypergolicCapacityKg = 500f;
        public float fuelBurnRatePerNewtonSecond = 1e-6f;
        public float hypergolicBurnRatePerNewtonSecond = 2e-6f;

        [Header("Assist Responsiveness")]
        public float attitudeAssistResponsiveness = 2f;
        public float coupledAssistResponsiveness = 1.5f;
        public float frameLockAssistResponsiveness = 1.5f;
        public float brakeResponsiveness = 4f;

        public void CopyFrom(ShipTuning other)
        {
            if (other == null)
            {
                return;
            }

            dryMassKg = other.dryMassKg;
            mainEngineForwardAccel = other.mainEngineForwardAccel;
            maneuverForwardAccel = other.maneuverForwardAccel;
            reverseAccel = other.reverseAccel;
            rightAccel = other.rightAccel;
            leftAccel = other.leftAccel;
            upAccel = other.upAccel;
            downAccel = other.downAccel;
            pitchPositiveAccel = other.pitchPositiveAccel;
            pitchNegativeAccel = other.pitchNegativeAccel;
            yawPositiveAccel = other.yawPositiveAccel;
            yawNegativeAccel = other.yawNegativeAccel;
            rollPositiveAccel = other.rollPositiveAccel;
            rollNegativeAccel = other.rollNegativeAccel;
            maxLinearSpeedMps = other.maxLinearSpeedMps;
            boostMaxLinearSpeedMps = other.boostMaxLinearSpeedMps;
            boostAccelMultiplier = other.boostAccelMultiplier;
            maxPitchSpeedRad = other.maxPitchSpeedRad;
            maxYawSpeedRad = other.maxYawSpeedRad;
            maxRollSpeedRad = other.maxRollSpeedRad;
            boostAngularSpeedMultiplier = other.boostAngularSpeedMultiplier;
            fineControlLinearAccelMultiplier = other.fineControlLinearAccelMultiplier;
            fineControlMaxLinearSpeedMps = other.fineControlMaxLinearSpeedMps;
            fineControlAngularAccelMultiplier = other.fineControlAngularAccelMultiplier;
            fuelCapacityKg = other.fuelCapacityKg;
            hypergolicCapacityKg = other.hypergolicCapacityKg;
            fuelBurnRatePerNewtonSecond = other.fuelBurnRatePerNewtonSecond;
            hypergolicBurnRatePerNewtonSecond = other.hypergolicBurnRatePerNewtonSecond;
            attitudeAssistResponsiveness = other.attitudeAssistResponsiveness;
            coupledAssistResponsiveness = other.coupledAssistResponsiveness;
            frameLockAssistResponsiveness = other.frameLockAssistResponsiveness;
            brakeResponsiveness = other.brakeResponsiveness;
        }
    }
}
