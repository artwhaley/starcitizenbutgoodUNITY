using System.Collections.Generic;
using FlightModel.Asteroids;
using FlightModel.World;
using NUnit.Framework;
using UnityEngine;

namespace FlightModel.Tests
{
    public class AsteroidDescriptorGeneratorTests
    {
        [Test]
        public void SameSeedSectorAndSettingsProducesIdenticalDescriptors()
        {
            var settings = TestSettings();
            var seed = new AsteroidWorldSeed(12345);
            var sector = new AsteroidSectorCoord(1, -2, 3);

            List<AsteroidDescriptor> first = AsteroidDescriptorGenerator.GenerateSector(seed, sector, settings);
            List<AsteroidDescriptor> second = AsteroidDescriptorGenerator.GenerateSector(seed, sector, settings);

            Assert.AreEqual(first.Count, second.Count);
            for (int i = 0; i < first.Count; i++)
            {
                Assert.AreEqual(first[i].id, second[i].id);
                Assert.AreEqual(first[i].sector, second[i].sector);
                Assert.AreEqual(first[i].localIndex, second[i].localIndex);
                Assert.AreEqual(first[i].position, second[i].position);
                Assert.AreEqual(first[i].rotation, second[i].rotation);
                Assert.AreEqual(first[i].radius, second[i].radius);
                Assert.AreEqual(first[i].nonUniformScale, second[i].nonUniformScale);
                Assert.AreEqual(first[i].visualVariant, second[i].visualVariant);
                Assert.AreEqual(first[i].resourceSeed, second[i].resourceSeed);
                Assert.AreEqual(first[i].frameId, second[i].frameId);
            }
        }

        [Test]
        public void DifferentSectorProducesDifferentIds()
        {
            var settings = TestSettings();
            var seed = new AsteroidWorldSeed(12345);

            List<AsteroidDescriptor> first = AsteroidDescriptorGenerator.GenerateSector(seed, new AsteroidSectorCoord(0, 0, 0), settings);
            List<AsteroidDescriptor> second = AsteroidDescriptorGenerator.GenerateSector(seed, new AsteroidSectorCoord(1, 0, 0), settings);

            Assert.AreNotEqual(first[0].id, second[0].id);
        }

        [Test]
        public void IdsAreUniqueWithinSector()
        {
            List<AsteroidDescriptor> descriptors = AsteroidDescriptorGenerator.GenerateSector(
                new AsteroidWorldSeed(12345),
                new AsteroidSectorCoord(0, 0, 0),
                TestSettings());

            var ids = new HashSet<AsteroidDescriptorId>();
            foreach (AsteroidDescriptor descriptor in descriptors)
            {
                Assert.IsTrue(ids.Add(descriptor.id));
            }
        }

        [Test]
        public void DescriptorValuesStayInsideConfiguredRanges()
        {
            var settings = TestSettings();
            var sector = new AsteroidSectorCoord(-1, 2, 3);
            Vector3 min = sector.GetMinCorner(settings.sectorSizeMeters);
            Vector3 max = min + Vector3.one * settings.sectorSizeMeters;

            List<AsteroidDescriptor> descriptors = AsteroidDescriptorGenerator.GenerateSector(
                new AsteroidWorldSeed(12345),
                sector,
                settings);

            foreach (AsteroidDescriptor descriptor in descriptors)
            {
                Assert.GreaterOrEqual(descriptor.position.x, min.x);
                Assert.Less(descriptor.position.x, max.x);
                Assert.GreaterOrEqual(descriptor.position.y, min.y);
                Assert.Less(descriptor.position.y, max.y);
                Assert.GreaterOrEqual(descriptor.position.z, min.z);
                Assert.Less(descriptor.position.z, max.z);
                Assert.GreaterOrEqual(descriptor.radius, settings.minRadiusMeters);
                Assert.LessOrEqual(descriptor.radius, settings.maxRadiusMeters);
                Assert.GreaterOrEqual(descriptor.nonUniformScale.x, settings.minRadiusMeters * (1f - settings.nonUniformScaleJitter));
                Assert.LessOrEqual(descriptor.nonUniformScale.x, settings.maxRadiusMeters * (1f + settings.nonUniformScaleJitter));
                Assert.GreaterOrEqual(descriptor.visualVariant, 0);
                Assert.Less(descriptor.visualVariant, settings.visualVariantCount);
                Assert.AreEqual(ReferenceFrameId.LocalZone, descriptor.frameId);
            }
        }

        [Test]
        public void GenerationDoesNotCreateSceneObjects()
        {
            int beforeCount = Object.FindObjectsByType<GameObject>().Length;

            AsteroidDescriptorGenerator.GenerateSector(
                new AsteroidWorldSeed(12345),
                new AsteroidSectorCoord(0, 0, 0),
                TestSettings());

            int afterCount = Object.FindObjectsByType<GameObject>().Length;
            Assert.AreEqual(beforeCount, afterCount);
        }

        static AsteroidGenerationSettings TestSettings()
        {
            return new AsteroidGenerationSettings
            {
                sectorSizeMeters = 1000f,
                asteroidsPerSector = 96,
                minRadiusMeters = 8f,
                maxRadiusMeters = 80f,
                visualVariantCount = 8,
                radiusDistributionPower = 2f,
                nonUniformScaleJitter = 0.22f,
                sectorDensityJitter = 0f
            };
        }
    }
}
