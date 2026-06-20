using System.Collections.Generic;
using FlightModel.World;
using UnityEngine;

namespace FlightModel.Asteroids
{
    public static class AsteroidDescriptorGenerator
    {
        public static List<AsteroidDescriptor> GenerateSector(
            AsteroidWorldSeed worldSeed,
            AsteroidSectorCoord sector,
            AsteroidGenerationSettings settings)
        {
            var descriptors = new List<AsteroidDescriptor>();
            GenerateSector(worldSeed, sector, settings, descriptors);
            return descriptors;
        }

        public static void GenerateSector(
            AsteroidWorldSeed worldSeed,
            AsteroidSectorCoord sector,
            AsteroidGenerationSettings settings,
            List<AsteroidDescriptor> results)
        {
            results.Clear();
            settings ??= new AsteroidGenerationSettings();

            int count = settings.GetAsteroidCountForSector(worldSeed, sector);
            float sectorSize = settings.SafeSectorSize;
            Vector3 sectorMin = sector.GetMinCorner(sectorSize);

            for (int i = 0; i < count; i++)
            {
                ulong seed = AsteroidHash.Combine(worldSeed.Value, sector.x, sector.y, sector.z, i);
                var random = new AsteroidDeterministicRandom(seed);

                Vector3 position = sectorMin + new Vector3(
                    random.Next01() * sectorSize,
                    random.Next01() * sectorSize,
                    random.Next01() * sectorSize);

                float radiusT = Mathf.Pow(random.Next01(), Mathf.Max(0.001f, settings.radiusDistributionPower));
                float radius = Mathf.Lerp(settings.SafeMinRadius, settings.SafeMaxRadius, radiusT);
                float jitter = settings.nonUniformScaleJitter;
                Vector3 nonUniformScale = new Vector3(
                    radius * RandomScaleAxis(random, jitter),
                    radius * RandomScaleAxis(random, jitter),
                    radius * RandomScaleAxis(random, jitter));

                results.Add(new AsteroidDescriptor
                {
                    id = AsteroidDescriptorId.From(worldSeed, sector, i),
                    sector = sector,
                    localIndex = i,
                    position = position,
                    rotation = random.NextRotation(),
                    radius = radius,
                    nonUniformScale = nonUniformScale,
                    visualVariant = random.NextInt(settings.SafeVisualVariantCount),
                    resourceSeed = unchecked((int)random.NextUInt()),
                    frameId = ReferenceFrameId.LocalZone
                });
            }
        }

        static float RandomScaleAxis(AsteroidDeterministicRandom random, float jitter)
        {
            return 1f + (random.Next01() * 2f - 1f) * Mathf.Clamp01(jitter);
        }
    }

    internal static class AsteroidHash
    {
        public static ulong Combine(int a, int b, int c, int d, int e)
        {
            ulong hash = 14695981039346656037ul;
            hash = Mix(hash, (uint)a);
            hash = Mix(hash, (uint)b);
            hash = Mix(hash, (uint)c);
            hash = Mix(hash, (uint)d);
            hash = Mix(hash, (uint)e);
            return SplitMix64(hash);
        }

        static ulong Mix(ulong hash, uint value)
        {
            hash ^= value;
            hash *= 1099511628211ul;
            return hash;
        }

        public static ulong SplitMix64(ulong value)
        {
            value += 0x9E3779B97F4A7C15ul;
            value = (value ^ (value >> 30)) * 0xBF58476D1CE4E5B9ul;
            value = (value ^ (value >> 27)) * 0x94D049BB133111EBul;
            return value ^ (value >> 31);
        }
    }

    internal struct AsteroidDeterministicRandom
    {
        ulong state;

        public AsteroidDeterministicRandom(ulong seed)
        {
            state = seed == 0 ? 0xD1B54A32D192ED03ul : seed;
        }

        public uint NextUInt()
        {
            state = AsteroidHash.SplitMix64(state);
            return (uint)(state >> 32);
        }

        public float Next01()
        {
            return (NextUInt() & 0x00FFFFFFu) / 16777216f;
        }

        public int NextInt(int exclusiveMax)
        {
            return exclusiveMax <= 1 ? 0 : (int)(NextUInt() % (uint)exclusiveMax);
        }

        public Quaternion NextRotation()
        {
            return Quaternion.Euler(Next01() * 360f, Next01() * 360f, Next01() * 360f);
        }
    }
}
