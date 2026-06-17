namespace FlightModel
{
    public enum ShipControlRequestSourceId
    {
        Pilot,
        Assist,
        Brake,
        External
    }

    public struct ShipControlRequest
    {
        public float linearRight;
        public float linearUp;
        public float linearForward;
        public float angularPitch;
        public float angularYaw;
        public float angularRoll;
        public bool boost;
        public bool fineControl;
        public bool brake;

        public static ShipControlRequest FromPilot(in ShipInputCommand input)
        {
            return new ShipControlRequest
            {
                linearForward = input.thrustForward,
                linearRight = input.thrustRight,
                linearUp = input.thrustUp,
                angularPitch = input.pitch,
                angularYaw = input.yaw,
                angularRoll = input.roll,
                boost = input.boost,
                fineControl = input.fineControl,
                brake = input.brake
            };
        }

        public static ShipControlRequest Merge(params ShipControlRequest[] requests)
        {
            ShipControlRequest merged = default;
            if (requests == null || requests.Length == 0)
            {
                return merged;
            }

            for (int i = 0; i < requests.Length; i++)
            {
                merged = MergePair(merged, requests[i]);
            }

            return merged;
        }

        static ShipControlRequest MergePair(in ShipControlRequest left, in ShipControlRequest right)
        {
            return new ShipControlRequest
            {
                linearRight = ClampAxis(left.linearRight + right.linearRight),
                linearUp = ClampAxis(left.linearUp + right.linearUp),
                linearForward = ClampAxis(left.linearForward + right.linearForward),
                angularPitch = ClampAxis(left.angularPitch + right.angularPitch),
                angularYaw = ClampAxis(left.angularYaw + right.angularYaw),
                angularRoll = ClampAxis(left.angularRoll + right.angularRoll),
                boost = left.boost || right.boost,
                fineControl = left.fineControl || right.fineControl,
                brake = left.brake || right.brake
            };
        }

        static float ClampAxis(float value) => UnityEngine.Mathf.Clamp(value, -1f, 1f);
    }
}
