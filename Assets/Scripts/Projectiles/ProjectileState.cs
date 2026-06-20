using UnityEngine;

namespace FlightModel
{
    public struct ProjectileState
    {
        public int projectileId;
        public int ownerEntityId;
        public Vector3 position;
        public Vector3 velocity;
        public float remainingLifetime;
        public float maxRangeMeters;
        public float damage;
        public bool alive;
        public Vector3 spawnPosition;

        public ProjectileState(
            int projectileId,
            int ownerEntityId,
            Vector3 position,
            Vector3 velocity,
            float maxLifetimeSeconds,
            float maxRangeMeters,
            float damage)
        {
            this.projectileId = projectileId;
            this.ownerEntityId = ownerEntityId;
            this.position = position;
            this.velocity = velocity;
            this.remainingLifetime = maxLifetimeSeconds;
            this.maxRangeMeters = maxRangeMeters;
            this.damage = damage;
            this.alive = true;
            this.spawnPosition = position;
        }
    }
}
