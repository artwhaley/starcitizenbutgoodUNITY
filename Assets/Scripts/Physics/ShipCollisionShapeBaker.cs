using System.Collections.Generic;
using UnityEngine;

namespace FlightModel
{
    public static class ShipCollisionShapeBaker
    {
        public sealed class BakeResult
        {
            public ShipCollisionShapeSet shapeSet;
            public Collider[] meshCastColliders = System.Array.Empty<Collider>();
            public Collider[] allShipColliders = System.Array.Empty<Collider>();
        }

        public static BakeResult Bake(Transform shipRoot, Transform collisionRoot)
        {
            var result = new BakeResult();
            if (shipRoot == null || collisionRoot == null)
            {
                result.shapeSet = ShipCollisionShapeSet.Empty;
                return result;
            }

            Collider[] colliders = collisionRoot.GetComponentsInChildren<Collider>(true);
            var shapes = new List<ShipCollisionShape>(colliders.Length);
            var meshCast = new List<Collider>();
            var allShip = new List<Collider>(colliders.Length);

            for (int i = 0; i < colliders.Length; i++)
            {
                Collider collider = colliders[i];
                if (collider == null || !collider.enabled || collider.isTrigger)
                {
                    continue;
                }

                allShip.Add(collider);

                switch (collider)
                {
                    case BoxCollider box:
                        shapes.Add(BakeBox(shipRoot, collider.transform, box));
                        break;
                    case CapsuleCollider capsule:
                        shapes.Add(BakeCapsule(shipRoot, collider.transform, capsule));
                        break;
                    case MeshCollider mesh when mesh.convex:
                        int meshIndex = meshCast.Count;
                        meshCast.Add(mesh);
                        shapes.Add(BakeConvexMesh(shipRoot, collider.transform, meshIndex));
                        break;
                    case MeshCollider mesh:
                        Debug.LogWarning(
                            $"{mesh.name}: non-convex MeshCollider cannot be used for ship collision. " +
                            "Use convex MeshCollider, BoxCollider, or CapsuleCollider.", mesh);
                        break;
                    default:
                        Debug.LogWarning(
                            $"{collider.name}: unsupported collider type {collider.GetType().Name} " +
                            "on ship collision proxy. Use BoxCollider, CapsuleCollider, or convex MeshCollider.",
                            collider);
                        break;
                }
            }

            result.meshCastColliders = meshCast.ToArray();
            result.allShipColliders = allShip.ToArray();
            result.shapeSet = new ShipCollisionShapeSet(shapes.ToArray(), BuildMeshIndexMap(meshCast.Count));
            return result;
        }

        static int[] BuildMeshIndexMap(int meshCount)
        {
            if (meshCount <= 0)
            {
                return System.Array.Empty<int>();
            }

            var indices = new int[meshCount];
            for (int i = 0; i < meshCount; i++)
            {
                indices[i] = i;
            }

            return indices;
        }

        static ShipCollisionShape BakeBox(Transform shipRoot, Transform colliderTransform, BoxCollider box)
        {
            GetLocalPose(shipRoot, colliderTransform, box.center, out Vector3 localCenter, out Quaternion localRotation);
            return new ShipCollisionShape
            {
                kind = ShipCollisionPrimitiveKind.Box,
                localCenter = localCenter,
                localRotation = localRotation,
                halfExtents = box.size * 0.5f,
                meshCastColliderIndex = -1
            };
        }

        static ShipCollisionShape BakeCapsule(Transform shipRoot, Transform colliderTransform, CapsuleCollider capsule)
        {
            GetLocalPose(shipRoot, colliderTransform, capsule.center, out Vector3 localCenter, out Quaternion localRotation);
            Vector3 lossyScale = colliderTransform.lossyScale;
            float radiusScale = capsule.direction switch
            {
                0 => Mathf.Max(lossyScale.y, lossyScale.z),
                1 => Mathf.Max(lossyScale.x, lossyScale.z),
                _ => Mathf.Max(lossyScale.x, lossyScale.y)
            };
            float heightScale = capsule.direction switch
            {
                0 => Mathf.Abs(lossyScale.x),
                1 => Mathf.Abs(lossyScale.y),
                _ => Mathf.Abs(lossyScale.z)
            };

            return new ShipCollisionShape
            {
                kind = ShipCollisionPrimitiveKind.Capsule,
                localCenter = localCenter,
                localRotation = localRotation,
                radius = capsule.radius * radiusScale,
                height = capsule.height * heightScale,
                capsuleAxis = capsule.direction,
                meshCastColliderIndex = -1
            };
        }

        static ShipCollisionShape BakeConvexMesh(Transform shipRoot, Transform colliderTransform, int meshIndex)
        {
            GetLocalPose(shipRoot, colliderTransform, Vector3.zero, out Vector3 localCenter, out Quaternion localRotation);
            return new ShipCollisionShape
            {
                kind = ShipCollisionPrimitiveKind.ConvexMesh,
                localCenter = localCenter,
                localRotation = localRotation,
                meshCastColliderIndex = meshIndex
            };
        }

        static void GetLocalPose(
            Transform shipRoot,
            Transform colliderTransform,
            Vector3 colliderLocalCenter,
            out Vector3 localCenter,
            out Quaternion localRotation)
        {
            Vector3 worldCenter = colliderTransform.TransformPoint(colliderLocalCenter);
            localCenter = shipRoot.InverseTransformPoint(worldCenter);
            localRotation = Quaternion.Inverse(shipRoot.rotation) * colliderTransform.rotation;
        }
    }
}
