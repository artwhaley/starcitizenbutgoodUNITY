using UnityEngine;

namespace FlightModel.Asteroids
{
    [System.Serializable]
    public class AsteroidPromotionSettings
    {
        public float promoteRadiusMeters = 250f;
        public float demoteRadiusMeters = 325f;
        public int maxPromotedAsteroids = 128;
        public int prewarmCount = 16;

        public float SafePromoteRadius => Mathf.Max(0f, promoteRadiusMeters);
        public float SafeDemoteRadius => Mathf.Max(SafePromoteRadius + 1f, demoteRadiusMeters);
        public int SafeMaxPromoted => Mathf.Max(1, maxPromotedAsteroids);
        public int SafePrewarmCount => Mathf.Clamp(prewarmCount, 0, SafeMaxPromoted);
    }
}
