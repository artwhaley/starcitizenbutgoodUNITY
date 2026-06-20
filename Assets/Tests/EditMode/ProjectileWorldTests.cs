using NUnit.Framework;
using UnityEngine;

namespace FlightModel.Tests
{
    public class ProjectileWorldTests
    {
        [Test]
        public void Tick_AdvancesProjectilePosition()
        {
            ProjectileWorld world = new ProjectileWorld();
            Vector3 startPos = new Vector3(0f, 0f, 10f);
            Vector3 velocity = new Vector3(0f, 0f, 100f);
            float dt = 0.1f;

            world.SpawnProjectile(
                projectileId: 1,
                ownerEntityId: 100,
                position: startPos,
                velocity: velocity,
                maxLifetimeSeconds: 10f,
                maxRangeMeters: 5000f,
                damage: 25f);

            world.TickProjectiles(dt, default, null);

            var list = new System.Collections.Generic.List<ProjectileState>();
            world.GetActiveProjectiles(list);

            Assert.AreEqual(1, list.Count, "Projectile should still be alive after one tick.");
            ProjectileState p = list[0];
            Assert.AreEqual(startPos + velocity * dt, p.position,
                "Projectile should have advanced by velocity * deltaTime.");
        }

        [Test]
        public void Tick_Despawns_AfterLifetime()
        {
            ProjectileWorld world = new ProjectileWorld();
            Vector3 startPos = Vector3.zero;
            Vector3 velocity = Vector3.forward * 10f;
            float lifetime = 0.5f;

            world.SpawnProjectile(1, 100, startPos, velocity, lifetime, 5000f, 10f);

            // Tick past the lifetime
            world.TickProjectiles(0.3f, default, null);
            world.TickProjectiles(0.3f, default, null);

            var list = new System.Collections.Generic.List<ProjectileState>();
            world.GetActiveProjectiles(list);

            Assert.AreEqual(0, list.Count,
                "Projectile should despawn after lifetime expires.");
        }

        [Test]
        public void Tick_Despawns_AfterMaxRange()
        {
            ProjectileWorld world = new ProjectileWorld();
            Vector3 startPos = Vector3.zero;
            Vector3 velocity = Vector3.forward * 100f;
            float maxRange = 50f;

            world.SpawnProjectile(1, 100, startPos, velocity, 10f, maxRange, 10f);

            // Tick enough that total travel exceeds maxRange
            world.TickProjectiles(1f, default, null);

            var list = new System.Collections.Generic.List<ProjectileState>();
            world.GetActiveProjectiles(list);

            Assert.AreEqual(0, list.Count,
                "Projectile should despawn after exceeding max range.");
        }

        [Test]
        public void Tick_MultipleProjectiles_AllAdvance()
        {
            ProjectileWorld world = new ProjectileWorld();
            Vector3 vel = Vector3.forward * 50f;

            world.SpawnProjectile(1, 100, Vector3.zero, vel, 10f, 5000f, 10f);
            world.SpawnProjectile(2, 100, Vector3.right, vel, 10f, 5000f, 10f);
            world.SpawnProjectile(3, 100, Vector3.up, vel, 10f, 5000f, 10f);

            world.TickProjectiles(0.1f, default, null);

            var list = new System.Collections.Generic.List<ProjectileState>();
            world.GetActiveProjectiles(list);

            Assert.AreEqual(3, list.Count, "All projectiles should be alive after one tick.");
            Assert.AreEqual(Vector3.zero + vel * 0.1f, list[0].position);
            Assert.AreEqual(Vector3.right + vel * 0.1f, list[1].position);
            Assert.AreEqual(Vector3.up + vel * 0.1f, list[2].position);
        }
    }
}
