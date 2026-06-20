using UnityEngine;

namespace FlightModel.Asteroids
{
    [CreateAssetMenu(menuName = "Flight Model/Asteroids/Asteroid Visual Library")]
    public class AsteroidVisualLibrary : ScriptableObject
    {
        [SerializeField] GameObject[] asteroidPrefabs;
        [SerializeField] Mesh[] asteroidMeshes;
        [SerializeField] Material[] asteroidMaterials;
        [SerializeField] Mesh fallbackMesh;
        [SerializeField] Material fallbackMaterial;

        public int VariantCount
        {
            get
            {
                int prefabCount = asteroidPrefabs != null ? asteroidPrefabs.Length : 0;
                int meshCount = asteroidMeshes != null ? asteroidMeshes.Length : 0;
                return Mathf.Max(1, Mathf.Max(prefabCount, meshCount));
            }
        }

        public bool TryGetVisual(int visualVariant, out Mesh mesh, out Material material)
        {
            mesh = null;
            material = null;

            if (asteroidPrefabs != null && asteroidPrefabs.Length > 0)
            {
                GameObject prefab = asteroidPrefabs[Mathf.Abs(visualVariant) % asteroidPrefabs.Length];
                if (prefab != null)
                {
                    MeshFilter filter = prefab.GetComponentInChildren<MeshFilter>(true);
                    MeshRenderer renderer = prefab.GetComponentInChildren<MeshRenderer>(true);
                    mesh = filter != null ? filter.sharedMesh : null;
                    material = renderer != null ? renderer.sharedMaterial : null;
                }
            }

            if (mesh == null && asteroidMeshes != null && asteroidMeshes.Length > 0)
            {
                mesh = asteroidMeshes[Mathf.Abs(visualVariant) % asteroidMeshes.Length];
            }

            if (material == null && asteroidMaterials != null && asteroidMaterials.Length > 0)
            {
                material = asteroidMaterials[Mathf.Abs(visualVariant) % asteroidMaterials.Length];
            }

            mesh ??= fallbackMesh;
            material ??= fallbackMaterial;
            return mesh != null && material != null;
        }
    }
}
