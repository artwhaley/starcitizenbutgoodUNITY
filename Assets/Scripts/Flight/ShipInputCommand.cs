namespace FlightModel
{
    public struct ShipInputCommand
    {
        public float thrustForward;
        public float thrustRight;
        public float thrustUp;
        public float pitch;
        public float yaw;
        public float roll;
        public bool boost;
        public bool fineControl;
        public bool brake;
        public bool firePrimary;
    }
}
