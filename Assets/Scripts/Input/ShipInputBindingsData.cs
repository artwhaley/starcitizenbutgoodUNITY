using System;

namespace FlightModel
{
    [Serializable]
    public class ShipInputBindingsData
    {
        public int schemaVersion = ShipInputBindingsStore.CurrentSchemaVersion;
        public HardwareAxisBinding[] axisBindings = CreateDefaultAxisBindings();
        public HardwareButtonBinding firePrimary = new();

        static HardwareAxisBinding[] CreateDefaultAxisBindings()
        {
            var bindings = new HardwareAxisBinding[8];
            for (int i = 0; i < bindings.Length; i++)
            {
                bindings[i] = new HardwareAxisBinding();
            }

            return bindings;
        }
    }
}
