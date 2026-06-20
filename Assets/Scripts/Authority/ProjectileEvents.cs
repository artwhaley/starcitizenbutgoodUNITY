using UnityEngine;

namespace FlightModel.Authority
{
    public struct ProjectileSpawnEvent
    {
        public int projectileId;
        public int ownerEntityId;
        public int weaponSlot;
        public uint serverTick;
        public Vector3 position;
        public Vector3 velocity;
    }

    public struct ProjectileImpactEvent
    {
        public int projectileId;
        public int ownerEntityId;
        public uint serverTick;
        public Vector3 point;
        public Vector3 normal;
        public int hitEntityId;
    }

    public struct ProjectileDespawnEvent
    {
        public int projectileId;
        public int ownerEntityId;
        public uint serverTick;
        public Vector3 position;
        public bool expired;
    }
}
