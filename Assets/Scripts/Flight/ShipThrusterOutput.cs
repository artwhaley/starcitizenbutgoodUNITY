namespace FlightModel
{
    public struct ShipThrusterOutput
    {
        public float mainEngineForward;
        public float maneuverForward;
        public float maneuverRight;
        public float maneuverUp;
        public float rcsPitch;
        public float rcsYaw;
        public float rcsRoll;
        public bool boostActive;
        public bool fineControlActive;
        public bool brakeActive;

        public ShipInputCommand ToMatcherCommand()
        {
            return new ShipInputCommand
            {
                thrustForward = maneuverForward + mainEngineForward,
                thrustRight = maneuverRight,
                thrustUp = maneuverUp,
                pitch = rcsPitch,
                yaw = rcsYaw,
                roll = rcsRoll,
                boost = boostActive,
                fineControl = fineControlActive,
                brake = brakeActive
            };
        }
    }
}
