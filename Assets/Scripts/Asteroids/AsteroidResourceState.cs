using System;

namespace FlightModel.Asteroids
{
    [Serializable]
    public struct AsteroidResourceState
    {
        public AsteroidDescriptorId descriptorId;
        public int resourceSeed;
        public int totalResourceUnits;
        public int remainingResourceUnits;
        public bool depleted;

        public bool IsValid => descriptorId.IsValid;
    }
}
