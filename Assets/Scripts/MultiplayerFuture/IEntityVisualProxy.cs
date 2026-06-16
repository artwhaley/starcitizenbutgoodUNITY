namespace FlightModel.MultiplayerFuture
{
    public interface IEntityVisualProxy
    {
        void ApplySnapshot(in EntitySnapshot snapshot);
        void SetRelevant(bool relevant);
    }
}
