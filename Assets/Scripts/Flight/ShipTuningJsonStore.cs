using System.IO;
using UnityEngine;

namespace FlightModel
{
    [System.Serializable]
    public class ShipTuningDto
    {
        public float dryMassKg;
        public float mainEngineForwardAccel;
        public float maneuverForwardAccel;
        public float reverseAccel;
        public float rightAccel;
        public float leftAccel;
        public float upAccel;
        public float downAccel;
        public float pitchPositiveAccel;
        public float pitchNegativeAccel;
        public float yawPositiveAccel;
        public float yawNegativeAccel;
        public float rollPositiveAccel;
        public float rollNegativeAccel;
        public float maxLinearSpeedMps;
        public float boostMaxLinearSpeedMps;
        public float boostAccelMultiplier;
        public float maxPitchSpeedRad;
        public float maxYawSpeedRad;
        public float maxRollSpeedRad;
        public float boostAngularSpeedMultiplier;
        public float fineControlLinearAccelMultiplier;
        public float fineControlMaxLinearSpeedMps;
        public float fineControlAngularAccelMultiplier;
        public float fuelCapacityKg;
        public float hypergolicCapacityKg;
        public float fuelBurnRatePerNewtonSecond;
        public float hypergolicBurnRatePerNewtonSecond;
        public float attitudeAssistResponsiveness;
        public float coupledAssistResponsiveness;
        public float frameLockAssistResponsiveness;
        public float brakeResponsiveness;

        public static ShipTuningDto From(ShipTuning tuning)
        {
            return new ShipTuningDto
            {
                dryMassKg = tuning.dryMassKg,
                mainEngineForwardAccel = tuning.mainEngineForwardAccel,
                maneuverForwardAccel = tuning.maneuverForwardAccel,
                reverseAccel = tuning.reverseAccel,
                rightAccel = tuning.rightAccel,
                leftAccel = tuning.leftAccel,
                upAccel = tuning.upAccel,
                downAccel = tuning.downAccel,
                pitchPositiveAccel = tuning.pitchPositiveAccel,
                pitchNegativeAccel = tuning.pitchNegativeAccel,
                yawPositiveAccel = tuning.yawPositiveAccel,
                yawNegativeAccel = tuning.yawNegativeAccel,
                rollPositiveAccel = tuning.rollPositiveAccel,
                rollNegativeAccel = tuning.rollNegativeAccel,
                maxLinearSpeedMps = tuning.maxLinearSpeedMps,
                boostMaxLinearSpeedMps = tuning.boostMaxLinearSpeedMps,
                boostAccelMultiplier = tuning.boostAccelMultiplier,
                maxPitchSpeedRad = tuning.maxPitchSpeedRad,
                maxYawSpeedRad = tuning.maxYawSpeedRad,
                maxRollSpeedRad = tuning.maxRollSpeedRad,
                boostAngularSpeedMultiplier = tuning.boostAngularSpeedMultiplier,
                fineControlLinearAccelMultiplier = tuning.fineControlLinearAccelMultiplier,
                fineControlMaxLinearSpeedMps = tuning.fineControlMaxLinearSpeedMps,
                fineControlAngularAccelMultiplier = tuning.fineControlAngularAccelMultiplier,
                fuelCapacityKg = tuning.fuelCapacityKg,
                hypergolicCapacityKg = tuning.hypergolicCapacityKg,
                fuelBurnRatePerNewtonSecond = tuning.fuelBurnRatePerNewtonSecond,
                hypergolicBurnRatePerNewtonSecond = tuning.hypergolicBurnRatePerNewtonSecond,
                attitudeAssistResponsiveness = tuning.attitudeAssistResponsiveness,
                coupledAssistResponsiveness = tuning.coupledAssistResponsiveness,
                frameLockAssistResponsiveness = tuning.frameLockAssistResponsiveness,
                brakeResponsiveness = tuning.brakeResponsiveness
            };
        }

        public void ApplyTo(ShipTuning tuning)
        {
            tuning.dryMassKg = dryMassKg;
            tuning.mainEngineForwardAccel = mainEngineForwardAccel;
            tuning.maneuverForwardAccel = maneuverForwardAccel;
            tuning.reverseAccel = reverseAccel;
            tuning.rightAccel = rightAccel;
            tuning.leftAccel = leftAccel;
            tuning.upAccel = upAccel;
            tuning.downAccel = downAccel;
            tuning.pitchPositiveAccel = pitchPositiveAccel;
            tuning.pitchNegativeAccel = pitchNegativeAccel;
            tuning.yawPositiveAccel = yawPositiveAccel;
            tuning.yawNegativeAccel = yawNegativeAccel;
            tuning.rollPositiveAccel = rollPositiveAccel;
            tuning.rollNegativeAccel = rollNegativeAccel;
            tuning.maxLinearSpeedMps = maxLinearSpeedMps;
            tuning.boostMaxLinearSpeedMps = boostMaxLinearSpeedMps;
            tuning.boostAccelMultiplier = boostAccelMultiplier;
            tuning.maxPitchSpeedRad = maxPitchSpeedRad;
            tuning.maxYawSpeedRad = maxYawSpeedRad;
            tuning.maxRollSpeedRad = maxRollSpeedRad;
            tuning.boostAngularSpeedMultiplier = boostAngularSpeedMultiplier;
            tuning.fineControlLinearAccelMultiplier = fineControlLinearAccelMultiplier;
            tuning.fineControlMaxLinearSpeedMps = fineControlMaxLinearSpeedMps;
            tuning.fineControlAngularAccelMultiplier = fineControlAngularAccelMultiplier;
            tuning.fuelCapacityKg = fuelCapacityKg;
            tuning.hypergolicCapacityKg = hypergolicCapacityKg;
            tuning.fuelBurnRatePerNewtonSecond = fuelBurnRatePerNewtonSecond;
            tuning.hypergolicBurnRatePerNewtonSecond = hypergolicBurnRatePerNewtonSecond;
            tuning.attitudeAssistResponsiveness = attitudeAssistResponsiveness;
            tuning.coupledAssistResponsiveness = coupledAssistResponsiveness;
            tuning.frameLockAssistResponsiveness = frameLockAssistResponsiveness;
            tuning.brakeResponsiveness = brakeResponsiveness;
        }
    }

    public static class ShipTuningJsonStore
    {
        public static string GetProfileKey(ShipTuning tuning)
            => tuning != null ? tuning.name : "UnknownShip";

        static string FilePathFor(string profileKey)
        {
            string safeKey = string.IsNullOrWhiteSpace(profileKey) ? "UnknownShip" : profileKey;
            foreach (char invalid in Path.GetInvalidFileNameChars())
            {
                safeKey = safeKey.Replace(invalid, '_');
            }

            return Path.Combine(Application.persistentDataPath, $"ship_tuning_{safeKey}.json");
        }

        public static void Save(ShipTuning tuning, string profileKey = null)
        {
            string key = profileKey ?? GetProfileKey(tuning);
            string json = JsonUtility.ToJson(ShipTuningDto.From(tuning), true);
            File.WriteAllText(FilePathFor(key), json);
        }

        public static bool TryLoad(ShipTuning tuning, string profileKey = null)
        {
            string path = FilePathFor(profileKey ?? GetProfileKey(tuning));
            if (!File.Exists(path))
            {
                return false;
            }

            ShipTuningDto dto = JsonUtility.FromJson<ShipTuningDto>(File.ReadAllText(path));
            dto?.ApplyTo(tuning);
            return dto != null;
        }
    }
}
