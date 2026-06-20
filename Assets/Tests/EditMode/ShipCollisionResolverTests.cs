using NUnit.Framework;
using UnityEngine;

namespace FlightModel.Tests
{
    public class ShipCollisionResolverTests
    {
        sealed class FakeCollisionWorld : ICollisionWorld
        {
            public bool shouldHit;
            public ShipCollisionHit hit;
            public bool shouldPenetrate;
            public ShipCollisionHit penetrationHit;
            public int penetrationCallCount;
            public int sweepCallCount;
            public int lastShapeCount;

            public bool SweepShip(
                in ShipCollisionShapeSet shapes,
                Vector3 fromPosition,
                Quaternion fromRotation,
                Vector3 toPosition,
                Quaternion toRotation,
                in ShipCollisionMask mask,
                out ShipCollisionHit outHit)
            {
                sweepCallCount++;
                lastShapeCount = shapes.Count;
                outHit = hit;
                return shouldHit;
            }

            public bool ComputeShipPenetration(
                in ShipCollisionShapeSet shapes,
                Vector3 position,
                Quaternion rotation,
                in ShipCollisionMask mask,
                out ShipCollisionHit outHit)
            {
                penetrationCallCount++;
                outHit = penetrationHit;
                return shouldPenetrate;
            }
        }

        static ShipState CreateState(Vector3 position, Vector3 velocity)
        {
            return new ShipState
            {
                position = position,
                rotation = Quaternion.identity,
                linearVelocity = velocity,
                angularVelocityRadians = Vector3.zero
            };
        }

        static ShipCollisionShapeSet SingleCapsuleShape()
        {
            return new ShipCollisionShapeSet(
                new[]
                {
                    new ShipCollisionShape
                    {
                        kind = ShipCollisionPrimitiveKind.Capsule,
                        localCenter = Vector3.zero,
                        radius = 4f,
                        height = 12f,
                        capsuleAxis = 1
                    }
                },
                System.Array.Empty<int>());
        }

        [Test]
        public void NoHit_LeavesPositionAndVelocityUnchanged()
        {
            var world = new FakeCollisionWorld { shouldHit = false };
            ShipState state = CreateState(new Vector3(1f, 2f, 3f), new Vector3(4f, 0f, 0f));
            Vector3 expectedPosition = state.position;
            Vector3 expectedVelocity = state.linearVelocity;

            bool resolved = ShipCollisionResolver.ResolveMovement(
                ref state,
                SingleCapsuleShape(),
                world,
                ShipCollisionMask.StationAndMineableAsteroid,
                expectedPosition,
                Quaternion.identity,
                0.35f,
                0.85f,
                0.05f,
                0.02f,
                0.25f,
                3,
                out ShipCollisionHit hit,
                out bool depenetrated,
                out float depenetrationDistance);

            Assert.IsFalse(resolved);
            Assert.IsFalse(hit.hasHit);
            Assert.IsFalse(depenetrated);
            Assert.AreEqual(0f, depenetrationDistance);
            Assert.AreEqual(expectedPosition, state.position);
            Assert.AreEqual(expectedVelocity, state.linearVelocity);
        }

        [Test]
        public void HeadOnHit_ClampPositionAndReflectVelocity()
        {
            var world = new FakeCollisionWorld
            {
                shouldHit = true,
                hit = new ShipCollisionHit
                {
                    hasHit = true,
                    point = new Vector3(99f, 99f, 99f),
                    normal = Vector3.right,
                    distance = 1f
                }
            };

            ShipState state = CreateState(new Vector3(1f, 0f, 0f), new Vector3(-10f, 0f, 0f));

            bool resolved = ShipCollisionResolver.ResolveMovement(
                ref state,
                SingleCapsuleShape(),
                world,
                ShipCollisionMask.StationAndMineableAsteroid,
                Vector3.zero,
                Quaternion.identity,
                0.35f,
                0.85f,
                0.05f,
                0.02f,
                0.25f,
                3,
                out _,
                out _,
                out _);

            Assert.IsTrue(resolved);
            Assert.AreEqual(new Vector3(0.95f, 0f, 0f), state.position);
            Assert.AreEqual(new Vector3(3.5f, 0f, 0f), state.linearVelocity);
        }

        [Test]
        public void TangentialVelocity_IsDampedNotFullyReflected()
        {
            var world = new FakeCollisionWorld
            {
                shouldHit = true,
                hit = new ShipCollisionHit
                {
                    hasHit = true,
                    point = Vector3.zero,
                    normal = Vector3.right,
                    distance = 1f
                }
            };

            ShipState state = CreateState(new Vector3(1f, 0f, 0f), new Vector3(-10f, 5f, 0f));

            ShipCollisionResolver.ResolveMovement(
                ref state,
                SingleCapsuleShape(),
                world,
                ShipCollisionMask.StationAndMineableAsteroid,
                Vector3.zero,
                Quaternion.identity,
                0.35f,
                0.85f,
                0.05f,
                0.02f,
                0.25f,
                3,
                out _,
                out _,
                out _);

            Assert.AreEqual(new Vector3(3.5f, 4.25f, 0f), state.linearVelocity);
        }

        [Test]
        public void TinyPostCollisionVelocity_IsZeroed()
        {
            var world = new FakeCollisionWorld
            {
                shouldHit = true,
                hit = new ShipCollisionHit
                {
                    hasHit = true,
                    point = Vector3.zero,
                    normal = Vector3.right,
                    distance = 1f
                }
            };

            ShipState state = CreateState(new Vector3(1f, 0f, 0f), new Vector3(-0.02f, 0.01f, 0f));

            ShipCollisionResolver.ResolveMovement(
                ref state,
                SingleCapsuleShape(),
                world,
                ShipCollisionMask.StationAndMineableAsteroid,
                Vector3.zero,
                Quaternion.identity,
                0.35f,
                0.85f,
                0.05f,
                0.02f,
                0.25f,
                3,
                out _,
                out _,
                out _);

            Assert.AreEqual(Vector3.zero, state.linearVelocity);
        }

        [Test]
        public void CompoundShapeSet_IsPassedToCollisionWorld()
        {
            var world = new FakeCollisionWorld { shouldHit = false };
            ShipState state = CreateState(Vector3.zero, Vector3.forward);

            var compound = new ShipCollisionShapeSet(
                new[]
                {
                    new ShipCollisionShape { kind = ShipCollisionPrimitiveKind.Box, halfExtents = Vector3.one },
                    new ShipCollisionShape { kind = ShipCollisionPrimitiveKind.Capsule, radius = 2f, height = 6f }
                },
                System.Array.Empty<int>());

            ShipCollisionResolver.ResolveMovement(
                ref state,
                compound,
                world,
                ShipCollisionMask.StationAndMineableAsteroid,
                Vector3.zero,
                Quaternion.identity,
                0.35f,
                0.85f,
                0.05f,
                0.02f,
                0.25f,
                3,
                out _,
                out _,
                out _);

            Assert.AreEqual(1, world.sweepCallCount);
            Assert.AreEqual(2, world.lastShapeCount);
        }

        [Test]
        public void PenetrationCorrection_IsCappedAndRemovesInwardVelocity()
        {
            var world = new FakeCollisionWorld
            {
                shouldHit = false,
                shouldPenetrate = true,
                penetrationHit = new ShipCollisionHit
                {
                    hasHit = true,
                    normal = Vector3.up,
                    distance = 1f
                }
            };

            ShipState state = CreateState(new Vector3(0f, 0f, 0f), new Vector3(0f, -3f, 1f));

            bool resolved = ShipCollisionResolver.ResolveMovement(
                ref state,
                SingleCapsuleShape(),
                world,
                ShipCollisionMask.StationAndMineableAsteroid,
                state.position,
                Quaternion.identity,
                0.35f,
                0.85f,
                0.05f,
                0.02f,
                0.25f,
                1,
                out ShipCollisionHit hit,
                out bool depenetrated,
                out float depenetrationDistance);

            Assert.IsTrue(resolved);
            Assert.IsTrue(depenetrated);
            Assert.AreEqual(0.25f, depenetrationDistance);
            Assert.AreEqual(new Vector3(0f, 0.25f, 0f), state.position);
            Assert.AreEqual(new Vector3(0f, 0f, 1f), state.linearVelocity);
            Assert.AreEqual(Vector3.up, hit.normal);
        }

        [Test]
        public void PenetrationCorrection_StopsAfterConfiguredIterations()
        {
            var world = new FakeCollisionWorld
            {
                shouldHit = false,
                shouldPenetrate = true,
                penetrationHit = new ShipCollisionHit
                {
                    hasHit = true,
                    normal = Vector3.forward,
                    distance = 0.5f
                }
            };

            ShipState state = CreateState(Vector3.zero, Vector3.zero);

            ShipCollisionResolver.ResolveMovement(
                ref state,
                SingleCapsuleShape(),
                world,
                ShipCollisionMask.StationAndMineableAsteroid,
                state.position,
                Quaternion.identity,
                0.35f,
                0.85f,
                0.05f,
                0.02f,
                0.25f,
                3,
                out _,
                out bool depenetrated,
                out float depenetrationDistance);

            Assert.IsTrue(depenetrated);
            Assert.AreEqual(3, world.penetrationCallCount);
            Assert.AreEqual(0.75f, depenetrationDistance);
            Assert.AreEqual(new Vector3(0f, 0f, 0.75f), state.position);
        }
    }
}
