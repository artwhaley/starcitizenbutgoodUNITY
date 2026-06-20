using UnityEngine;

namespace FlightModel.Asteroids
{
    [System.Serializable]
    public class AsteroidGenerationSettings
    {
        public float sectorSizeMeters = 1000f;
        public int asteroidsPerSector = 96;
        public float minRadiusMeters = 8f;
        public float maxRadiusMeters = 80f;
        public int visualVariantCount = 8;

        [Header("Distribution Tuning")]
        [Min(0f)] public float radiusDistributionPower = 2f;
        [Range(0f, 1f)] public float nonUniformScaleJitter = 0.22f;
        [Range(0f, 1f)] public float sectorDensityJitter = 0f;

        public int GetAsteroidCountForSector(AsteroidWorldSeed seed, AsteroidSectorCoord sector)
        {
            int baseCount = Mathf.Max(0, asteroidsPerSector);
            if (sectorDensityJitter <= 0f || baseCount == 0)
            {
                return baseCount;
            }

            var random = new AsteroidDeterministicRandom(AsteroidHash.Combine(seed.Value, sector.x, sector.y, sector.z, 0x51A7));
            float multiplier = Mathf.Lerp(1f - sectorDensityJitter, 1f + sectorDensityJitter, random.Next01());
            return Mathf.Max(0, Mathf.RoundToInt(baseCount * multiplier));
        }

        public float SafeSectorSize => Mathf.Max(0.001f, sectorSizeMeters);
        public float SafeMinRadius => Mathf.Max(0.001f, Mathf.Min(minRadiusMeters, maxRadiusMeters));
        public float SafeMaxRadius => Mathf.Max(SafeMinRadius, Mathf.Max(minRadiusMeters, maxRadiusMeters));
        public int SafeVisualVariantCount => Mathf.Max(1, visualVariantCount);
    }
}
