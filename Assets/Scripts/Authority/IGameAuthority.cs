namespace FlightModel.Authority
{
    public interface IGameAuthority
    {
        void SubmitInput(in ClientInputCommand command);
        void SubmitWeaponFire(in WeaponFireRequest request);
        void Tick(float deltaTime);
    }
}
