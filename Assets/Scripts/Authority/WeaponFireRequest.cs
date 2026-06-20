namespace FlightModel.Authority
{
    public struct WeaponFireRequest
    {
        public int clientId;
        public int shooterEntityId;
        public int weaponSlot;
        public uint inputTick;
        public bool fireHeld;
    }
}
