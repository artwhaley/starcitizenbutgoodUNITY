using System.Collections.Generic;
using UnityEngine;

namespace FlightModel.Asteroids
{
    public class AsteroidSceneryRenderer : MonoBehaviour
    {
        const int MaxInstancesPerDraw = 1023;

        [SerializeField] AsteroidWorldSeed worldSeed = new(12345);
        [SerializeField] AsteroidGenerationSettings generationSettings = new();
        [SerializeField] AsteroidVisualLibrary visualLibrary;
        [SerializeField] AsteroidSectorTracker sectorTracker;
        [SerializeField] Transform target;
        [SerializeField] ShipFlightController flightTarget;
        [SerializeField] int visibleSectorRadius = 2;
        [SerializeField] int drawLayer;
        [SerializeField] bool castShadows = true;
        [SerializeField] bool receiveShadows = true;

        readonly Dictionary<AsteroidSectorCoord, List<AsteroidDescriptor>> descriptorCache = new();
        readonly Dictionary<BatchKey, List<Matrix4x4>> batchMatrices = new();
        readonly HashSet<AsteroidDescriptorId> suppressedDescriptors = new();
        readonly List<AsteroidSectorCoord> activeSectorScratch = new();
        readonly Matrix4x4[] drawBuffer = new Matrix4x4[MaxInstancesPerDraw];
        Mesh fallbackMesh;
        Material fallbackMaterial;

        public AsteroidGenerationSettings GenerationSettings => generationSettings;
        public AsteroidVisualLibrary VisualLibrary => visualLibrary;
        public AsteroidWorldSeed WorldSeed
        {
            get => worldSeed;
            set
            {
                if (worldSeed == value)
                {
                    return;
                }

                worldSeed = value;
                descriptorCache.Clear();
                RebuildBatches();
            }
        }

        void OnValidate()
        {
            generationSettings ??= new AsteroidGenerationSettings();
            visibleSectorRadius = Mathf.Max(0, visibleSectorRadius);

            if (sectorTracker == null)
            {
                sectorTracker = GetComponent<AsteroidSectorTracker>();
            }

            if (sectorTracker != null)
            {
                sectorTracker.Target = target;
                sectorTracker.FlightTarget = flightTarget;
                sectorTracker.GenerationSettings = generationSettings;
                sectorTracker.VisibleSectorRadius = visibleSectorRadius;
            }
        }

        public void SetDescriptorSuppressed(AsteroidDescriptorId descriptorId, bool suppressed)
        {
            bool changed = suppressed ? suppressedDescriptors.Add(descriptorId) : suppressedDescriptors.Remove(descriptorId);
            if (changed)
            {
                RebuildBatches();
            }
        }

        public void CollectActiveDescriptors(List<AsteroidDescriptor> results)
        {
            results.Clear();
            if (sectorTracker == null)
            {
                return;
            }

            activeSectorScratch.Clear();
            foreach (AsteroidSectorCoord sector in sectorTracker.ActiveSectors)
            {
                activeSectorScratch.Add(sector);
            }

            foreach (AsteroidSectorCoord sector in activeSectorScratch)
            {
                List<AsteroidDescriptor> descriptors = GetDescriptors(sector);
                results.AddRange(descriptors);
            }
        }

        public void ResolveVisualForDescriptor(int visualVariant, out Mesh mesh, out Material material)
        {
            ResolveVisual(visualVariant, out mesh, out material);
        }

        void Awake()
        {
            EnsureRuntimeReferences();
            RebuildBatches();
        }

        void LateUpdate()
        {
            EnsureRuntimeReferences();
            if (sectorTracker != null && sectorTracker.UpdateTrackedSectors())
            {
                RebuildBatches();
            }

            DrawBatches();
        }

        void EnsureRuntimeReferences()
        {
            generationSettings ??= new AsteroidGenerationSettings();
            generationSettings.visualVariantCount = visualLibrary != null
                ? Mathf.Max(generationSettings.visualVariantCount, visualLibrary.VariantCount)
                : generationSettings.visualVariantCount;

            if (flightTarget == null)
            {
                flightTarget = FindAnyObjectByType<ShipFlightController>();
            }

            if (target == null && flightTarget != null)
            {
                target = flightTarget.transform;
            }

            if (sectorTracker == null)
            {
                sectorTracker = GetComponent<AsteroidSectorTracker>();
                if (sectorTracker == null)
                {
                    sectorTracker = gameObject.AddComponent<AsteroidSectorTracker>();
                }
            }

            sectorTracker.Target = target;
            sectorTracker.FlightTarget = flightTarget;
            sectorTracker.GenerationSettings = generationSettings;
            sectorTracker.VisibleSectorRadius = visibleSectorRadius;
        }

        void RebuildBatches()
        {
            batchMatrices.Clear();
            if (sectorTracker == null)
            {
                return;
            }

            activeSectorScratch.Clear();
            foreach (AsteroidSectorCoord sector in sectorTracker.ActiveSectors)
            {
                activeSectorScratch.Add(sector);
            }

            foreach (AsteroidSectorCoord sector in activeSectorScratch)
            {
                List<AsteroidDescriptor> descriptors = GetDescriptors(sector);
                foreach (AsteroidDescriptor descriptor in descriptors)
                {
                    if (suppressedDescriptors.Contains(descriptor.id))
                    {
                        continue;
                    }

                    ResolveVisual(descriptor.visualVariant, out Mesh mesh, out Material material);
                    if (mesh == null || material == null)
                    {
                        continue;
                    }

                    material.enableInstancing = true;
                    var key = new BatchKey(mesh, material);
                    if (!batchMatrices.TryGetValue(key, out List<Matrix4x4> matrices))
                    {
                        matrices = new List<Matrix4x4>();
                        batchMatrices.Add(key, matrices);
                    }

                    matrices.Add(Matrix4x4.TRS(descriptor.position, descriptor.rotation, descriptor.nonUniformScale));
                }
            }
        }

        List<AsteroidDescriptor> GetDescriptors(AsteroidSectorCoord sector)
        {
            if (!descriptorCache.TryGetValue(sector, out List<AsteroidDescriptor> descriptors))
            {
                descriptors = new List<AsteroidDescriptor>();
                AsteroidDescriptorGenerator.GenerateSector(worldSeed, sector, generationSettings, descriptors);
                descriptorCache.Add(sector, descriptors);
            }

            return descriptors;
        }

        void DrawBatches()
        {
            UnityEngine.Rendering.ShadowCastingMode shadowMode = castShadows
                ? UnityEngine.Rendering.ShadowCastingMode.On
                : UnityEngine.Rendering.ShadowCastingMode.Off;

            foreach (KeyValuePair<BatchKey, List<Matrix4x4>> batch in batchMatrices)
            {
                List<Matrix4x4> matrices = batch.Value;
                for (int start = 0; start < matrices.Count; start += MaxInstancesPerDraw)
                {
                    int count = Mathf.Min(MaxInstancesPerDraw, matrices.Count - start);
                    matrices.CopyTo(start, drawBuffer, 0, count);
                    Graphics.DrawMeshInstanced(
                        batch.Key.Mesh,
                        0,
                        batch.Key.Material,
                        drawBuffer,
                        count,
                        null,
                        shadowMode,
                        receiveShadows,
                        drawLayer);
                }
            }
        }

        void ResolveVisual(int visualVariant, out Mesh mesh, out Material material)
        {
            if (visualLibrary != null && visualLibrary.TryGetVisual(visualVariant, out mesh, out material))
            {
                return;
            }

            fallbackMesh ??= CreateFallbackAsteroidMesh();
            fallbackMaterial ??= CreateFallbackMaterial();
            mesh = fallbackMesh;
            material = fallbackMaterial;
        }

        public static Mesh CreateFallbackAsteroidMesh()
        {
            var mesh = new Mesh { name = "Runtime_Fallback_Asteroid" };
            Vector3[] vertices =
            {
                new(0f, 1f, 0f),
                new(0.9f, 0.15f, 0.15f),
                new(0.25f, -0.2f, 0.95f),
                new(-0.85f, 0.05f, 0.25f),
                new(-0.2f, -0.35f, -0.95f),
                new(0f, -1f, 0f)
            };
            int[] triangles =
            {
                0, 1, 2,
                0, 2, 3,
                0, 3, 4,
                0, 4, 1,
                5, 2, 1,
                5, 3, 2,
                5, 4, 3,
                5, 1, 4
            };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        public static Material CreateFallbackMaterial()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var material = new Material(shader)
            {
                name = "Runtime_Fallback_Asteroid_Material",
                color = new Color(0.46f, 0.43f, 0.39f, 1f),
                enableInstancing = true
            };
            return material;
        }

        readonly struct BatchKey : System.IEquatable<BatchKey>
        {
            public readonly Mesh Mesh;
            public readonly Material Material;

            public BatchKey(Mesh mesh, Material material)
            {
                Mesh = mesh;
                Material = material;
            }

            public bool Equals(BatchKey other) => Mesh == other.Mesh && Material == other.Material;
            public override bool Equals(object obj) => obj is BatchKey other && Equals(other);
            public override int GetHashCode() => unchecked(((Mesh != null ? Mesh.GetHashCode() : 0) * 397) ^ (Material != null ? Material.GetHashCode() : 0));
        }
    }
}
