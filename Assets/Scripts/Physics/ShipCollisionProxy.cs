using UnityEngine;

namespace FlightModel
{
    /// <summary>
    /// Authored ship collision root under COG. Collects child box/capsule/convex-mesh
    /// colliders, bakes them for custom sweep queries, and exposes bounce tuning.
    /// </summary>
    public class ShipCollisionProxy : MonoBehaviour
    {
        [SerializeField] Transform shipRoot;
        [SerializeField] Transform collisionRoot;
        [SerializeField] LayerMask obstacleMask = ~0;
        [SerializeField] float restitution = 0.35f;
        [SerializeField] float tangentialDamping = 0.85f;
        [SerializeField] float skinWidth = 0.05f;
        [SerializeField] float depenetrationSkinWidth = 0.02f;
        [SerializeField] float maxDepenetrationMetersPerStep = 0.25f;
        [SerializeField] int maxDepenetrationIterations = 3;
        [SerializeField] Rigidbody shipRigidbody;

        ShipCollisionShapeSet bakedShapes = ShipCollisionShapeSet.Empty;
        UnityCollisionWorld collisionWorld;

        public float Restitution => restitution;
        public float TangentialDamping => tangentialDamping;
        public float SkinWidth => skinWidth;
        public float DepenetrationSkinWidth => depenetrationSkinWidth;
        public float MaxDepenetrationMetersPerStep => maxDepenetrationMetersPerStep;
        public int MaxDepenetrationIterations => maxDepenetrationIterations;
        public ShipCollisionShapeSet BakedShapes => bakedShapes;
        public ICollisionWorld CollisionWorld => collisionWorld;
        public ShipCollisionMask ObstacleMask => new() { layerMask = obstacleMask.value };
        public bool IsReady => collisionWorld != null && !bakedShapes.IsEmpty;

        void Awake()
        {
            if (shipRoot == null)
            {
                shipRoot = transform.parent != null ? transform.parent : transform;
            }

            if (collisionRoot == null)
            {
                collisionRoot = transform;
            }

            if (shipRigidbody == null)
            {
                shipRigidbody = GetComponent<Rigidbody>();
            }

            if (obstacleMask.value == 0 || obstacleMask.value == ~0)
            {
                obstacleMask = LayerMask.GetMask("Station", "MineableAsteroid");
            }

            BakeCollisionShapes();
        }

        void OnValidate()
        {
            if (shipRoot == null)
            {
                shipRoot = transform.parent != null ? transform.parent : transform;
            }

            if (collisionRoot == null)
            {
                collisionRoot = transform;
            }

            if (shipRigidbody == null)
            {
                shipRigidbody = GetComponent<Rigidbody>();
            }

            if (obstacleMask.value == ~0)
            {
                obstacleMask = LayerMask.GetMask("Station", "MineableAsteroid");
            }

            ValidateRigidbody();
            ValidateColliders();
        }

        public void BakeCollisionShapes()
        {
            ShipCollisionShapeBaker.BakeResult bakeResult =
                ShipCollisionShapeBaker.Bake(shipRoot, collisionRoot);
            bakedShapes = bakeResult.shapeSet;
            collisionWorld = bakedShapes.IsEmpty
                ? null
                : new UnityCollisionWorld(
                    shipRoot,
                    shipRigidbody,
                    bakeResult.meshCastColliders,
                    bakeResult.allShipColliders);

            if (bakedShapes.IsEmpty)
            {
                Debug.LogError(
                    $"{name}: no authored ship collision primitives found under '{collisionRoot.name}'. " +
                    "Add BoxCollider, CapsuleCollider, or convex MeshCollider children under ShipCollisionProxy.",
                    this);
            }
        }

        void ValidateRigidbody()
        {
            if (shipRigidbody == null)
            {
                return;
            }

            if (!shipRigidbody.isKinematic)
            {
                Debug.LogWarning($"{name}: ship Rigidbody should be kinematic.", this);
            }

            if (shipRigidbody.useGravity)
            {
                Debug.LogWarning($"{name}: ship Rigidbody should not use gravity.", this);
            }
        }

        void ValidateColliders()
        {
            if (collisionRoot == null)
            {
                return;
            }

            foreach (Collider collider in collisionRoot.GetComponentsInChildren<Collider>(true))
            {
                if (collider == null || !collider.enabled)
                {
                    continue;
                }

                if (collider.isTrigger)
                {
                    Debug.LogWarning(
                        $"{collider.name}: ship collision collider should not be a trigger. " +
                        "Use DockingTrigger for trigger volumes.", collider);
                }
            }
        }
    }
}
