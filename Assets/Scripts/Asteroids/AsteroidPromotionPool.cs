using System.Collections.Generic;
using FlightModel.World;
using UnityEngine;

namespace FlightModel.Asteroids
{
    public class AsteroidPromotionPool : MonoBehaviour
    {
        [SerializeField] AsteroidSceneryRenderer sceneryRenderer;
        [SerializeField] AsteroidSectorTracker sectorTracker;
        [SerializeField] AsteroidVisualLibrary visualLibrary;
        [SerializeField] Transform target;
        [SerializeField] ShipFlightController flightTarget;
        [SerializeField] AsteroidPromotionSettings settings = new();
        [SerializeField] Transform poolRoot;

        readonly Dictionary<AsteroidDescriptorId, AsteroidInstance> promotedById = new();
        readonly Stack<AsteroidInstance> availableInstances = new();
        readonly List<AsteroidInstance> demoteScratch = new();
        readonly List<AsteroidDescriptor> descriptorScratch = new();
        readonly List<PromotionCandidate> promoteScratch = new();
        int createdInstanceCount;

        struct PromotionCandidate
        {
            public AsteroidDescriptor descriptor;
            public float distanceSqr;
        }

        public int PromotedCount => promotedById.Count;

        void OnValidate()
        {
            settings ??= new AsteroidPromotionSettings();

            if (sceneryRenderer == null)
            {
                sceneryRenderer = GetComponent<AsteroidSceneryRenderer>();
            }

            if (sectorTracker == null)
            {
                sectorTracker = GetComponent<AsteroidSectorTracker>();
            }
        }

        void Awake()
        {
            settings ??= new AsteroidPromotionSettings();
            EnsureReferences();
            EnsurePoolRoot();
            PrewarmPool(settings.SafePrewarmCount);
        }

        void LateUpdate()
        {
            EnsureReferences();
            if ((flightTarget == null && target == null) || sceneryRenderer == null)
            {
                return;
            }

            UpdatePromotion(GetPlayerPosition());
        }

        void EnsureReferences()
        {
            if (sceneryRenderer == null)
            {
                sceneryRenderer = GetComponent<AsteroidSceneryRenderer>();
            }

            if (sectorTracker == null)
            {
                sectorTracker = GetComponent<AsteroidSectorTracker>();
            }

            if (visualLibrary == null && sceneryRenderer != null)
            {
                visualLibrary = sceneryRenderer.VisualLibrary;
            }

            if (flightTarget == null)
            {
                flightTarget = FindAnyObjectByType<ShipFlightController>();
            }

            if (target == null && flightTarget != null)
            {
                target = flightTarget.transform;
            }
        }

        Vector3 GetPlayerPosition()
        {
            return flightTarget != null ? flightTarget.State.position : target.position;
        }

        void EnsurePoolRoot()
        {
            if (poolRoot != null)
            {
                return;
            }

            var rootObject = new GameObject("PromotedAsteroidPool");
            rootObject.transform.SetParent(transform, false);
            poolRoot = rootObject.transform;
        }

        void PrewarmPool(int count)
        {
            for (int i = 0; i < count; i++)
            {
                ReturnToPool(CreateInstance());
                createdInstanceCount++;
            }
        }

        void UpdatePromotion(Vector3 playerPosition)
        {
            float demoteRadiusSqr = settings.SafeDemoteRadius * settings.SafeDemoteRadius;
            float promoteRadiusSqr = settings.SafePromoteRadius * settings.SafePromoteRadius;

            demoteScratch.Clear();
            foreach (KeyValuePair<AsteroidDescriptorId, AsteroidInstance> entry in promotedById)
            {
                AsteroidInstance instance = entry.Value;
                if (instance == null || !instance.IsPromoted)
                {
                    continue;
                }

                float distanceSqr = (instance.Descriptor.position - playerPosition).sqrMagnitude;
                if (distanceSqr > demoteRadiusSqr)
                {
                    demoteScratch.Add(instance);
                }
            }

            for (int i = 0; i < demoteScratch.Count; i++)
            {
                if (demoteScratch[i] != null)
                {
                    DemoteInstance(demoteScratch[i]);
                }
            }

            sceneryRenderer.CollectActiveDescriptors(descriptorScratch);
            promoteScratch.Clear();

            for (int i = 0; i < descriptorScratch.Count; i++)
            {
                AsteroidDescriptor descriptor = descriptorScratch[i];
                if (!descriptor.id.IsValid || promotedById.ContainsKey(descriptor.id))
                {
                    continue;
                }

                float distanceSqr = (descriptor.position - playerPosition).sqrMagnitude;
                if (distanceSqr > promoteRadiusSqr)
                {
                    continue;
                }

                promoteScratch.Add(new PromotionCandidate
                {
                    descriptor = descriptor,
                    distanceSqr = distanceSqr
                });
            }

            promoteScratch.Sort((left, right) => left.distanceSqr.CompareTo(right.distanceSqr));

            int slots = settings.SafeMaxPromoted - promotedById.Count;
            for (int i = 0; i < promoteScratch.Count && slots > 0; i++, slots--)
            {
                PromoteDescriptor(promoteScratch[i].descriptor);
            }
        }

        void PromoteDescriptor(in AsteroidDescriptor descriptor)
        {
            if (!descriptor.id.IsValid || promotedById.ContainsKey(descriptor.id))
            {
                return;
            }

            AsteroidInstance instance = RentInstance();
            if (instance == null)
            {
                return;
            }

            if (!TryResolveVisual(descriptor.visualVariant, out Mesh mesh, out Material material))
            {
                ReturnToPool(instance);
                return;
            }

            instance.Promote(descriptor, mesh, material);
            promotedById.Add(descriptor.id, instance);
            sceneryRenderer.SetDescriptorSuppressed(descriptor.id, true);

            WorldEntity worldEntity = instance.GetComponent<WorldEntity>();
            if (worldEntity != null)
            {
                LocalEntityRegistry registry = LocalEntityRegistry.Instance;
                registry?.Register(worldEntity, EntityKind.Asteroid);
            }
        }

        void DemoteInstance(AsteroidInstance instance)
        {
            if (instance == null || !instance.IsPromoted)
            {
                return;
            }

            AsteroidDescriptorId descriptorId = instance.DescriptorId;
            promotedById.Remove(descriptorId);
            sceneryRenderer.SetDescriptorSuppressed(descriptorId, false);

            WorldEntity worldEntity = instance.GetComponent<WorldEntity>();
            if (worldEntity != null)
            {
                LocalEntityRegistry.Instance?.Unregister(worldEntity);
            }

            instance.Demote();
            ReturnToPool(instance);
        }

        AsteroidInstance RentInstance()
        {
            if (availableInstances.Count > 0)
            {
                return availableInstances.Pop();
            }

            if (createdInstanceCount >= settings.SafeMaxPromoted)
            {
                return null;
            }

            AsteroidInstance created = CreateInstance();
            createdInstanceCount++;
            return created;
        }

        void ReturnToPool(AsteroidInstance instance)
        {
            if (instance == null)
            {
                return;
            }

            availableInstances.Push(instance);
        }

        AsteroidInstance CreateInstance()
        {
            var instanceObject = new GameObject("PromotedAsteroid");
            instanceObject.transform.SetParent(poolRoot, false);
            instanceObject.layer = LayerMask.NameToLayer("MineableAsteroid");

            SphereCollider collider = instanceObject.AddComponent<SphereCollider>();
            collider.enabled = false;

            WorldEntity worldEntity = instanceObject.AddComponent<WorldEntity>();
            worldEntity.Kind = EntityKind.Asteroid;

            instanceObject.AddComponent<MineableAsteroid>();

            var visualObject = new GameObject("Visual");
            visualObject.transform.SetParent(instanceObject.transform, false);
            visualObject.AddComponent<MeshFilter>();
            visualObject.AddComponent<MeshRenderer>().enabled = false;

            AsteroidInstance instance = instanceObject.AddComponent<AsteroidInstance>();
            instanceObject.SetActive(false);
            return instance;
        }

        bool TryResolveVisual(int visualVariant, out Mesh mesh, out Material material)
        {
            if (sceneryRenderer != null)
            {
                sceneryRenderer.ResolveVisualForDescriptor(visualVariant, out mesh, out material);
                return mesh != null && material != null;
            }

            if (visualLibrary != null && visualLibrary.TryGetVisual(visualVariant, out mesh, out material))
            {
                return true;
            }

            mesh = AsteroidSceneryRenderer.CreateFallbackAsteroidMesh();
            material = AsteroidSceneryRenderer.CreateFallbackMaterial();
            return mesh != null && material != null;
        }
    }
}
