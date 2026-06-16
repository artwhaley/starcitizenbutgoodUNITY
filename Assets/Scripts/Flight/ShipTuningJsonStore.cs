using System.IO;
using UnityEngine;

namespace FlightModel
{
    [System.Serializable]
    public class ShipTuningDto
    {
        public float massKg;
        public Vector3 maxThrustNewtons;
        public Vector3 maxTorque;
        public float boostMultiplier;
        public float angularDampingStrength;
        public float brakeLinearDampingStrength;
        public float brakeAngularDampingStrength;
        public float coupledLateralDampingStrength;
        public float frameLockLinearDampingStrength;

        public static ShipTuningDto From(ShipTuning tuning)
        {
            return new ShipTuningDto
            {
                massKg = tuning.massKg,
                maxThrustNewtons = tuning.maxThrustNewtons,
                maxTorque = tuning.maxTorque,
                boostMultiplier = tuning.boostMultiplier,
                angularDampingStrength = tuning.angularDampingStrength,
                brakeLinearDampingStrength = tuning.brakeLinearDampingStrength,
                brakeAngularDampingStrength = tuning.brakeAngularDampingStrength,
                coupledLateralDampingStrength = tuning.coupledLateralDampingStrength,
                frameLockLinearDampingStrength = tuning.frameLockLinearDampingStrength
            };
        }

        public void ApplyTo(ShipTuning tuning)
        {
            tuning.massKg = massKg;
            tuning.maxThrustNewtons = maxThrustNewtons;
            tuning.maxTorque = maxTorque;
            tuning.boostMultiplier = boostMultiplier;
            tuning.angularDampingStrength = angularDampingStrength;
            tuning.brakeLinearDampingStrength = brakeLinearDampingStrength;
            tuning.brakeAngularDampingStrength = brakeAngularDampingStrength;
            tuning.coupledLateralDampingStrength = coupledLateralDampingStrength;
            tuning.frameLockLinearDampingStrength = frameLockLinearDampingStrength;
        }
    }

    public static class ShipTuningJsonStore
    {
        static string FilePath => Path.Combine(Application.persistentDataPath, "ship_tuning_override.json");

        public static void Save(ShipTuning tuning)
        {
            string json = JsonUtility.ToJson(ShipTuningDto.From(tuning), true);
            File.WriteAllText(FilePath, json);
        }

        public static bool TryLoad(ShipTuning tuning)
        {
            if (!File.Exists(FilePath))
            {
                return false;
            }

            ShipTuningDto dto = JsonUtility.FromJson<ShipTuningDto>(File.ReadAllText(FilePath));
            dto?.ApplyTo(tuning);
            return dto != null;
        }
    }
}
