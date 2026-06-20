using FlightModel.World;
using UnityEngine;

namespace FlightModel.Asteroids
{
    [System.Serializable]
    public struct AsteroidDescriptor
    {
        public AsteroidDescriptorId id;
        public AsteroidSectorCoord sector;
        public int localIndex;
        public Vector3 position;
        public Quaternion rotation;
        public float radius;
        public Vector3 nonUniformScale;
        public int visualVariant;
        public int resourceSeed;
        public ReferenceFrameId frameId;
    }
}
