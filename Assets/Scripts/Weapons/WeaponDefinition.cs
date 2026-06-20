using UnityEngine;

namespace FlightModel
{
    [CreateAssetMenu(fileName = "WeaponDefinition", menuName = "Flight/Weapon Definition", order = 100)]
    public class WeaponDefinition : ScriptableObject
    {
        [Tooltip("Rounds per second at maximum sustained fire.")]
        public float fireRatePerSecond = 20f;

        [Tooltip("Muzzle velocity in meters per second.")]
        public float projectileSpeedMetersPerSecond = 650f;

        [Tooltip("Projectile is despawned after this many seconds regardless of range.")]
        public float maxLifetimeSeconds = 1.25f;

        [Tooltip("Projectile is despawned after traveling this far in meters.")]
        public float maxRangeMeters = 800f;

        [Tooltip("Damage dealt when the projectile hits an IHitReceiver.")]
        public float damage = 1f;

        [Tooltip("Layers the projectile raycasts against.")]
        public LayerMask hitMask = ~0;
    }
}
