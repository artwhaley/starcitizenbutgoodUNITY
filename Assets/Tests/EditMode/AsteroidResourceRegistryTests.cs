using FlightModel.Asteroids;
using NUnit.Framework;

namespace FlightModel.Tests
{
    public class AsteroidResourceRegistryTests
    {
        AsteroidDescriptor CreateDescriptor(int resourceSeed, long idValue = 42)
        {
            return new AsteroidDescriptor
            {
                id = new AsteroidDescriptorId(idValue),
                resourceSeed = resourceSeed
            };
        }

        [Test]
        public void SameDescriptor_CreatesSameInitialResourceAmount()
        {
            var registryObject = new UnityEngine.GameObject("registry");
            var registry = registryObject.AddComponent<AsteroidResourceRegistry>();

            AsteroidDescriptor descriptor = CreateDescriptor(1337, 99);
            AsteroidResourceState first = registry.GetOrCreate(descriptor);
            AsteroidResourceState second = registry.GetOrCreate(descriptor);

            Assert.AreEqual(first.totalResourceUnits, second.totalResourceUnits);
            Assert.AreEqual(first.remainingResourceUnits, second.remainingResourceUnits);
            Assert.AreEqual(first.totalResourceUnits, AsteroidResourceRegistry.ComputeTotalResourceUnits(1337));
            Assert.GreaterOrEqual(first.totalResourceUnits, AsteroidResourceRegistry.MinResourceUnits);
            Assert.LessOrEqual(first.totalResourceUnits, AsteroidResourceRegistry.MaxResourceUnits);

            UnityEngine.Object.DestroyImmediate(registryObject);
        }

        [Test]
        public void Depletion_CannotGoBelowZero()
        {
            var registryObject = new UnityEngine.GameObject("registry");
            var registry = registryObject.AddComponent<AsteroidResourceRegistry>();
            AsteroidDescriptor descriptor = CreateDescriptor(7, 100);

            registry.GetOrCreate(descriptor);
            Assert.IsTrue(registry.ApplyMiningHit(descriptor.id, 9999f, out AsteroidResourceState depleted));
            Assert.AreEqual(0, depleted.remainingResourceUnits);
            Assert.IsTrue(depleted.depleted);

            UnityEngine.Object.DestroyImmediate(registryObject);
        }

        [Test]
        public void DepletedFlag_BecomesTrueAtZero()
        {
            var registryObject = new UnityEngine.GameObject("registry");
            var registry = registryObject.AddComponent<AsteroidResourceRegistry>();
            AsteroidDescriptor descriptor = CreateDescriptor(11, 101);
            AsteroidResourceState created = registry.GetOrCreate(descriptor);

            Assert.IsFalse(created.depleted);
            Assert.IsTrue(registry.ApplyMiningHit(descriptor.id, created.remainingResourceUnits, out AsteroidResourceState depleted));
            Assert.IsTrue(depleted.depleted);

            UnityEngine.Object.DestroyImmediate(registryObject);
        }

        [Test]
        public void StateSurvivesLogicalDemotionAndPromotion_ByDescriptorId()
        {
            var registryObject = new UnityEngine.GameObject("registry");
            var registry = registryObject.AddComponent<AsteroidResourceRegistry>();
            AsteroidDescriptor descriptor = CreateDescriptor(55, 202);

            registry.GetOrCreate(descriptor);
            Assert.IsTrue(registry.ApplyMiningHit(descriptor.id, 3f, out AsteroidResourceState afterHit));

            AsteroidResourceState afterRebind = registry.GetOrCreate(descriptor);
            Assert.AreEqual(afterHit.remainingResourceUnits, afterRebind.remainingResourceUnits);
            Assert.AreEqual(afterHit.depleted, afterRebind.depleted);
            Assert.AreEqual(afterHit.totalResourceUnits, afterRebind.totalResourceUnits);

            UnityEngine.Object.DestroyImmediate(registryObject);
        }
    }
}
