using UnityEngine;

namespace FlightModel
{
    /// <summary>
    /// Unity Physics query backend for ship movement sweeps.
    /// This is the only runtime class in the collision feature that calls Unity Physics APIs.
    /// </summary>
    public sealed class UnityCollisionWorld : ICollisionWorld
    {
        const int OverlapBufferSize = 128;

        readonly Transform shipRoot;
        readonly Rigidbody shipRigidbody;
        readonly Collider[] meshCastColliders;
        readonly Collider[] allShipColliders;
        readonly Collider[] overlapBuffer = new Collider[OverlapBufferSize];
        bool loggedOverlapOverflow;

        public UnityCollisionWorld(
            Transform shipRoot,
            Rigidbody shipRigidbody,
            Collider[] meshCastColliders,
            Collider[] allShipColliders)
        {
            this.shipRoot = shipRoot;
            this.shipRigidbody = shipRigidbody;
            this.meshCastColliders = meshCastColliders ?? System.Array.Empty<Collider>();
            this.allShipColliders = allShipColliders ?? System.Array.Empty<Collider>();
        }

        public bool SweepShip(
            in ShipCollisionShapeSet shapes,
            Vector3 fromPosition,
            Quaternion fromRotation,
            Vector3 toPosition,
            Quaternion toRotation,
            in ShipCollisionMask mask,
            out ShipCollisionHit hit)
        {
            hit = default;
            if (shapes.IsEmpty)
            {
                return false;
            }

            Vector3 delta = toPosition - fromPosition;
            float distance = delta.magnitude;
            if (distance <= 1e-6f)
            {
                return false;
            }

            Vector3 direction = delta / distance;
            bool found = false;
            ShipCollisionHit bestHit = default;
            float bestDistance = float.MaxValue;

            for (int i = 0; i < shapes.shapes.Length; i++)
            {
                if (!TrySweepPrimitive(
                        shapes.shapes[i],
                        fromPosition,
                        fromRotation,
                        direction,
                        distance,
                        mask.layerMask,
                        out ShipCollisionHit primitiveHit))
                {
                    continue;
                }

                if (primitiveHit.distance < bestDistance)
                {
                    bestDistance = primitiveHit.distance;
                    bestHit = primitiveHit;
                    found = true;
                }
            }

            hit = bestHit;
            return found;
        }

        public bool ComputeShipPenetration(
            in ShipCollisionShapeSet shapes,
            Vector3 position,
            Quaternion rotation,
            in ShipCollisionMask mask,
            out ShipCollisionHit hit)
        {
            hit = default;
            if (shapes.IsEmpty || shipRoot == null || allShipColliders.Length == 0)
            {
                return false;
            }

            Vector3 savedPosition = shipRoot.position;
            Quaternion savedRotation = shipRoot.rotation;
            shipRoot.SetPositionAndRotation(position, rotation);
            Physics.SyncTransforms();

            try
            {
                bool found = false;
                ShipCollisionHit bestHit = default;
                float bestDistance = 0f;

                for (int i = 0; i < allShipColliders.Length; i++)
                {
                    Collider shipCollider = allShipColliders[i];
                    if (shipCollider == null || !shipCollider.enabled || shipCollider.isTrigger)
                    {
                        continue;
                    }

                    int overlapCount = CollectPotentialOverlaps(shipCollider, mask.layerMask);
                    for (int overlapIndex = 0; overlapIndex < overlapCount; overlapIndex++)
                    {
                        Collider obstacleCollider = overlapBuffer[overlapIndex];
                        if (obstacleCollider == null
                            || !obstacleCollider.enabled
                            || obstacleCollider.isTrigger
                            || obstacleCollider.transform.IsChildOf(shipRoot))
                        {
                            continue;
                        }

                        if (!Physics.ComputePenetration(
                                shipCollider,
                                shipCollider.transform.position,
                                shipCollider.transform.rotation,
                                obstacleCollider,
                                obstacleCollider.transform.position,
                                obstacleCollider.transform.rotation,
                                out Vector3 direction,
                                out float distance))
                        {
                            continue;
                        }

                        if (!found || distance > bestDistance)
                        {
                            bestDistance = distance;
                            bestHit = new ShipCollisionHit
                            {
                                hasHit = true,
                                point = obstacleCollider.ClosestPoint(shipCollider.bounds.center),
                                normal = direction,
                                distance = distance,
                                layer = obstacleCollider.gameObject.layer
                            };
                            found = true;
                        }
                    }
                }

                hit = bestHit;
                return found;
            }
            finally
            {
                shipRoot.SetPositionAndRotation(savedPosition, savedRotation);
                Physics.SyncTransforms();
            }
        }

        bool TrySweepPrimitive(
            in ShipCollisionShape shape,
            Vector3 fromPosition,
            Quaternion fromRotation,
            Vector3 direction,
            float distance,
            int layerMask,
            out ShipCollisionHit hit)
        {
            switch (shape.kind)
            {
                case ShipCollisionPrimitiveKind.Box:
                    return TryBoxCast(shape, fromPosition, fromRotation, direction, distance, layerMask, out hit);
                case ShipCollisionPrimitiveKind.Capsule:
                    return TryCapsuleCast(shape, fromPosition, fromRotation, direction, distance, layerMask, out hit);
                case ShipCollisionPrimitiveKind.ConvexMesh:
                    return TryMeshCast(shape, fromPosition, fromRotation, direction, distance, layerMask, out hit);
                default:
                    hit = default;
                    return false;
            }
        }

        bool TryBoxCast(
            in ShipCollisionShape shape,
            Vector3 fromPosition,
            Quaternion fromRotation,
            Vector3 direction,
            float distance,
            int layerMask,
            out ShipCollisionHit hit)
        {
            Vector3 worldCenter = fromPosition + fromRotation * shape.localCenter;
            Quaternion worldRotation = fromRotation * shape.localRotation;
            if (Physics.BoxCast(
                    worldCenter,
                    shape.halfExtents,
                    direction,
                    out RaycastHit rayHit,
                    worldRotation,
                    distance,
                    layerMask,
                    QueryTriggerInteraction.Ignore))
            {
                hit = FromRaycastHit(rayHit);
                return true;
            }

            hit = default;
            return false;
        }

        bool TryCapsuleCast(
            in ShipCollisionShape shape,
            Vector3 fromPosition,
            Quaternion fromRotation,
            Vector3 direction,
            float distance,
            int layerMask,
            out ShipCollisionHit hit)
        {
            GetCapsuleEndpoints(
                shape.localCenter,
                shape.radius,
                shape.height,
                shape.capsuleAxis,
                fromPosition,
                fromRotation,
                out Vector3 point0,
                out Vector3 point1);

            if (Physics.CapsuleCast(
                    point0,
                    point1,
                    shape.radius,
                    direction,
                    out RaycastHit rayHit,
                    distance,
                    layerMask,
                    QueryTriggerInteraction.Ignore))
            {
                hit = FromRaycastHit(rayHit);
                return true;
            }

            hit = default;
            return false;
        }

        bool TryMeshCast(
            in ShipCollisionShape shape,
            Vector3 fromPosition,
            Quaternion fromRotation,
            Vector3 direction,
            float distance,
            int layerMask,
            out ShipCollisionHit hit)
        {
            hit = default;
            if (shipRoot == null
                || shipRigidbody == null
                || shape.meshCastColliderIndex < 0
                || shape.meshCastColliderIndex >= meshCastColliders.Length)
            {
                return false;
            }

            Collider collider = meshCastColliders[shape.meshCastColliderIndex];
            if (collider == null)
            {
                return false;
            }

            Vector3 savedPosition = shipRoot.position;
            Quaternion savedRotation = shipRoot.rotation;
            bool[] colliderEnabledStates = DisableOtherShipColliders(collider);
            shipRoot.SetPositionAndRotation(fromPosition, fromRotation);
            Physics.SyncTransforms();

            try
            {
                if (shipRigidbody.SweepTest(
                        direction,
                        out RaycastHit rayHit,
                        distance,
                        QueryTriggerInteraction.Ignore))
                {
                    hit = FromRaycastHit(rayHit);
                    return true;
                }
            }
            finally
            {
                RestoreShipColliderStates(colliderEnabledStates);
                shipRoot.SetPositionAndRotation(savedPosition, savedRotation);
                Physics.SyncTransforms();
            }

            return false;
        }

        bool[] DisableOtherShipColliders(Collider activeCollider)
        {
            var states = new bool[allShipColliders.Length];
            for (int i = 0; i < allShipColliders.Length; i++)
            {
                Collider shipCollider = allShipColliders[i];
                if (shipCollider == null)
                {
                    continue;
                }

                states[i] = shipCollider.enabled;
                shipCollider.enabled = shipCollider == activeCollider;
            }

            return states;
        }

        int CollectPotentialOverlaps(Collider shipCollider, int layerMask)
        {
            Bounds bounds = shipCollider.bounds;
            Vector3 halfExtents = Max(bounds.extents, 0.01f);
            int count = Physics.OverlapBoxNonAlloc(
                bounds.center,
                halfExtents,
                overlapBuffer,
                Quaternion.identity,
                layerMask,
                QueryTriggerInteraction.Ignore);

            if (count >= overlapBuffer.Length && !loggedOverlapOverflow)
            {
                Debug.LogWarning(
                    "UnityCollisionWorld: overlap buffer filled during penetration query. " +
                    "Some collision candidates may be skipped.");
                loggedOverlapOverflow = true;
            }

            return Mathf.Min(count, overlapBuffer.Length);
        }

        static Vector3 Max(Vector3 value, float minimum)
        {
            return new Vector3(
                Mathf.Max(value.x, minimum),
                Mathf.Max(value.y, minimum),
                Mathf.Max(value.z, minimum));
        }

        void RestoreShipColliderStates(bool[] states)
        {
            for (int i = 0; i < allShipColliders.Length && i < states.Length; i++)
            {
                Collider shipCollider = allShipColliders[i];
                if (shipCollider != null)
                {
                    shipCollider.enabled = states[i];
                }
            }
        }

        static void GetCapsuleEndpoints(
            Vector3 localCenter,
            float radius,
            float height,
            int direction,
            Vector3 position,
            Quaternion rotation,
            out Vector3 point0,
            out Vector3 point1)
        {
            Vector3 axis = direction switch
            {
                0 => Vector3.right,
                1 => Vector3.up,
                _ => Vector3.forward
            };

            float halfLine = Mathf.Max(0f, height * 0.5f - radius);
            Vector3 worldAxis = rotation * axis;
            Vector3 worldCenter = position + rotation * localCenter;
            point0 = worldCenter - worldAxis * halfLine;
            point1 = worldCenter + worldAxis * halfLine;
        }

        static ShipCollisionHit FromRaycastHit(RaycastHit rayHit)
        {
            return new ShipCollisionHit
            {
                hasHit = true,
                point = rayHit.point,
                normal = rayHit.normal,
                distance = rayHit.distance,
                layer = rayHit.collider != null ? rayHit.collider.gameObject.layer : 0
            };
        }
    }
}
