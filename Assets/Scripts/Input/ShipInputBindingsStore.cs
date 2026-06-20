using System.IO;
using UnityEngine;

namespace FlightModel
{
    public static class ShipInputBindingsStore
    {
        public const int CurrentSchemaVersion = 3;
        static string FilePath => Path.Combine(Application.persistentDataPath, "ship_input_bindings.json");

        public static ShipInputBindingsData Load()
        {
            if (!File.Exists(FilePath))
            {
                return new ShipInputBindingsData();
            }

            try
            {
                string json = File.ReadAllText(FilePath);
                ShipInputBindingsData data = JsonUtility.FromJson<ShipInputBindingsData>(json);
                ShipInputBindingsData migrated = Migrate(data ?? new ShipInputBindingsData(), out bool changed);
                if (changed)
                {
                    Save(migrated);
                }

                return migrated;
            }
            catch
            {
                return new ShipInputBindingsData();
            }
        }

        public static void Save(ShipInputBindingsData data)
        {
            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(FilePath, json);
        }

        static ShipInputBindingsData Migrate(ShipInputBindingsData data, out bool changed)
        {
            changed = false;
            EnsureAxisArray(data, ref changed);
            EnsureButtonBindings(data, ref changed);

            if (data.schemaVersion < 2)
            {
                foreach (HardwareAxisBinding binding in data.axisBindings)
                {
                    if (binding?.calibration == null)
                    {
                        continue;
                    }

                    binding.calibration.scale *= 0.5f;
                }

                changed = true;
            }

            if (data.schemaVersion != CurrentSchemaVersion)
            {
                data.schemaVersion = CurrentSchemaVersion;
                changed = true;
            }

            return data;
        }

        static void EnsureAxisArray(ShipInputBindingsData data, ref bool changed)
        {
            if (data.axisBindings == null || data.axisBindings.Length != 8)
            {
                data.axisBindings = new ShipInputBindingsData().axisBindings;
                changed = true;
                return;
            }

            for (int i = 0; i < data.axisBindings.Length; i++)
            {
                if (data.axisBindings[i] == null)
                {
                    data.axisBindings[i] = new HardwareAxisBinding();
                    changed = true;
                }

                if (data.axisBindings[i].calibration == null)
                {
                    data.axisBindings[i].calibration = new AxisCalibration();
                    changed = true;
                }
            }
        }

        static void EnsureButtonBindings(ShipInputBindingsData data, ref bool changed)
        {
            if (data.firePrimary == null)
            {
                data.firePrimary = new HardwareButtonBinding();
                changed = true;
            }

            if (data.boost == null)
            {
                data.boost = new HardwareButtonBinding();
                changed = true;
            }

            if (data.brake == null)
            {
                data.brake = new HardwareButtonBinding();
                changed = true;
            }
        }
    }
}
