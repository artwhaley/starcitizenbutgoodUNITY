namespace FlightModel.Authority
{
    public struct ClientInputCommand
    {
        public int clientId;
        public int controlledEntityId;
        public uint inputTick;
        public ShipInputCommand shipInput;
    }
}
