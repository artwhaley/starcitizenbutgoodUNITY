using System.Collections.Generic;
using UnityEngine;

namespace FlightModel.Asteroids
{
    public class AsteroidSectorTracker : MonoBehaviour
    {
        [HideInInspector, SerializeField] Transform target;
        [HideInInspector, SerializeField] ShipFlightController flightTarget;
        [HideInInspector, SerializeField] AsteroidGenerationSettings generationSettings = new();
        [HideInInspector, SerializeField] int visibleSectorRadius = 2;

        readonly List<AsteroidSectorCoord> activeSectors = new();
        AsteroidSectorCoord currentSector;
        bool hasCurrentSector;

        public IReadOnlyList<AsteroidSectorCoord> ActiveSectors => activeSectors;
        public AsteroidSectorCoord CurrentSector => currentSector;
        public int VisibleSectorRadius
        {
            get => visibleSectorRadius;
            set => visibleSectorRadius = Mathf.Max(0, value);
        }

        public Transform Target
        {
            get => target;
            set
            {
                if (target == value)
                {
                    return;
                }

                target = value;
                hasCurrentSector = false;
            }
        }

        public ShipFlightController FlightTarget
        {
            get => flightTarget;
            set
            {
                if (flightTarget == value)
                {
                    return;
                }

                flightTarget = value;
                hasCurrentSector = false;
            }
        }

        public AsteroidGenerationSettings GenerationSettings
        {
            get => generationSettings;
            set
            {
                generationSettings = value ?? new AsteroidGenerationSettings();
                hasCurrentSector = false;
            }
        }

        public bool UpdateTrackedSectors()
        {
            if (flightTarget == null && target == null)
            {
                return false;
            }

            generationSettings ??= new AsteroidGenerationSettings();
            AsteroidSectorCoord nextSector = AsteroidSectorCoord.FromWorldPosition(
                GetTrackedPosition(),
                generationSettings.SafeSectorSize);

            if (hasCurrentSector && nextSector == currentSector)
            {
                return false;
            }

            currentSector = nextSector;
            hasCurrentSector = true;
            RebuildActiveSectors();
            return true;
        }

        void Awake()
        {
            if (target == null)
            {
                flightTarget = FindAnyObjectByType<ShipFlightController>();
                if (flightTarget != null)
                {
                    target = flightTarget.transform;
                }
            }

            UpdateTrackedSectors();
        }

        Vector3 GetTrackedPosition()
        {
            return flightTarget != null ? flightTarget.State.position : target.position;
        }

        void RebuildActiveSectors()
        {
            activeSectors.Clear();
            int radius = Mathf.Max(0, visibleSectorRadius);
            for (int x = currentSector.x - radius; x <= currentSector.x + radius; x++)
            {
                for (int y = currentSector.y - radius; y <= currentSector.y + radius; y++)
                {
                    for (int z = currentSector.z - radius; z <= currentSector.z + radius; z++)
                    {
                        activeSectors.Add(new AsteroidSectorCoord(x, y, z));
                    }
                }
            }
        }
    }
}
