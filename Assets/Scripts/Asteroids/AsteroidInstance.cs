using UnityEngine;

namespace FlightModel.Asteroids
{
    public class AsteroidInstance : MonoBehaviour
    {
        [SerializeField] MeshRenderer meshRenderer;
        [SerializeField] SphereCollider sphereCollider;
        [SerializeField] MineableAsteroid mineableAsteroid;

        AsteroidDescriptor descriptor;
        bool isPromoted;

        public AsteroidDescriptorId DescriptorId => descriptor.id;
        public AsteroidDescriptor Descriptor => descriptor;
        public bool IsPromoted => isPromoted;
        public SphereCollider SphereCollider => sphereCollider;
        public MeshRenderer MeshRenderer => meshRenderer;
        public MineableAsteroid Mineable => mineableAsteroid;

        void Awake()
        {
            if (meshRenderer == null)
            {
                meshRenderer = GetComponentInChildren<MeshRenderer>(true);
            }

            if (sphereCollider == null)
            {
                sphereCollider = GetComponent<SphereCollider>();
            }

            if (mineableAsteroid == null)
            {
                mineableAsteroid = GetComponent<MineableAsteroid>();
            }
        }

        public void Promote(in AsteroidDescriptor nextDescriptor, Mesh mesh, Material material)
        {
            descriptor = nextDescriptor;
            isPromoted = true;

            transform.SetPositionAndRotation(descriptor.position, descriptor.rotation);
            transform.localScale = Vector3.one;

            Transform visualRoot = meshRenderer != null ? meshRenderer.transform : transform;
            if (meshRenderer != null)
            {
                MeshFilter filter = meshRenderer.GetComponent<MeshFilter>();
                if (filter != null)
                {
                    filter.sharedMesh = mesh;
                }

                meshRenderer.sharedMaterial = material;
                meshRenderer.enabled = true;
            }

            visualRoot.localPosition = Vector3.zero;
            visualRoot.localRotation = Quaternion.identity;
            visualRoot.localScale = descriptor.nonUniformScale;

            if (sphereCollider != null)
            {
                ConfigureColliderFromVisualMesh(mesh, visualRoot.localScale);
                sphereCollider.enabled = true;
            }

            mineableAsteroid?.Bind(this);
            gameObject.SetActive(true);
        }

        public void Demote()
        {
            isPromoted = false;
            descriptor = default;

            if (sphereCollider != null)
            {
                sphereCollider.enabled = false;
            }

            if (meshRenderer != null)
            {
                meshRenderer.enabled = false;
            }

            mineableAsteroid?.Unbind();
            gameObject.SetActive(false);
        }

        void ConfigureColliderFromVisualMesh(Mesh mesh, Vector3 visualScale)
        {
            if (sphereCollider == null)
            {
                return;
            }

            if (mesh == null)
            {
                sphereCollider.center = Vector3.zero;
                sphereCollider.radius = Mathf.Max(0.1f, descriptor.radius);
                return;
            }

            Bounds bounds = mesh.bounds;
            Vector3 scaledCenter = Vector3.Scale(bounds.center, visualScale);
            Vector3 scaledExtents = Vector3.Scale(bounds.extents, Abs(visualScale));
            sphereCollider.center = scaledCenter;
            sphereCollider.radius = Mathf.Max(0.1f, scaledExtents.magnitude);
        }

        static Vector3 Abs(Vector3 value)
        {
            return new Vector3(Mathf.Abs(value.x), Mathf.Abs(value.y), Mathf.Abs(value.z));
        }
    }
}
