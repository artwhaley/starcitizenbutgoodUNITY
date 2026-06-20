using UnityEngine;

namespace FlightModel
{
    public static class ShipCollisionResolver
    {
        const float MovementEpsilon = 1e-6f;
        const float VelocityZeroThresholdMps = 0.05f;

        public static bool ResolveMovement(
            ref ShipState state,
            in ShipCollisionShapeSet shapes,
            ICollisionWorld collisionWorld,
            in ShipCollisionMask mask,
            Vector3 previousPosition,
            Quaternion previousRotation,
            float restitution,
            float tangentialDamping,
            float skinWidth,
            float depenetrationSkinWidth,
            float maxDepenetrationMetersPerStep,
            int maxDepenetrationIterations,
            out ShipCollisionHit hit,
            out bool depenetrated,
            out float depenetrationDistance)
        {
            hit = default;
            depenetrated = false;
            depenetrationDistance = 0f;

            if (collisionWorld == null || shapes.IsEmpty)
            {
                return false;
            }

            Vector3 proposedPosition = state.position;
            Quaternion proposedRotation = state.rotation;
            bool resolved = false;

            if (collisionWorld.SweepShip(
                    shapes,
                    previousPosition,
                    previousRotation,
                    proposedPosition,
                    proposedRotation,
                    mask,
                    out ShipCollisionHit sweepHit))
            {
                resolved = true;
                hit = sweepHit;
                ApplySweepResponse(
                    ref state,
                    previousPosition,
                    proposedPosition,
                    proposedRotation,
                    restitution,
                    tangentialDamping,
                    skinWidth,
                    sweepHit);
            }

            int iterations = Mathf.Max(0, maxDepenetrationIterations);
            for (int i = 0; i < iterations; i++)
            {
                if (!collisionWorld.ComputeShipPenetration(
                        shapes,
                        state.position,
                        state.rotation,
                        mask,
                        out ShipCollisionHit penetrationHit))
                {
                    break;
                }

                float correctionDistance = Mathf.Min(
                    penetrationHit.distance + depenetrationSkinWidth,
                    maxDepenetrationMetersPerStep);
                if (correctionDistance <= MovementEpsilon)
                {
                    break;
                }

                state.position += penetrationHit.normal * correctionDistance;
                RemoveInwardNormalVelocity(ref state.linearVelocity, penetrationHit.normal);

                depenetrated = true;
                depenetrationDistance += correctionDistance;
                hit = penetrationHit;
                resolved = true;
            }

            if (state.linearVelocity.sqrMagnitude < VelocityZeroThresholdMps * VelocityZeroThresholdMps)
            {
                state.linearVelocity = Vector3.zero;
            }

            return resolved;
        }

        static void ApplySweepResponse(
            ref ShipState state,
            Vector3 previousPosition,
            Vector3 proposedPosition,
            Quaternion proposedRotation,
            float restitution,
            float tangentialDamping,
            float skinWidth,
            in ShipCollisionHit hit)
        {
            Vector3 travel = proposedPosition - previousPosition;
            float travelDistance = travel.magnitude;
            if (travelDistance > MovementEpsilon)
            {
                Vector3 travelDirection = travel / travelDistance;
                float safeDistance = Mathf.Max(0f, hit.distance - skinWidth);
                state.position = previousPosition + travelDirection * safeDistance;
            }
            else
            {
                state.position = previousPosition;
            }

            state.rotation = proposedRotation;

            Vector3 velocity = state.linearVelocity;
            float normalSpeed = Vector3.Dot(velocity, hit.normal);
            if (normalSpeed < 0f)
            {
                Vector3 normalVelocity = hit.normal * normalSpeed;
                Vector3 tangentialVelocity = velocity - normalVelocity;
                velocity = -normalVelocity * restitution + tangentialVelocity * tangentialDamping;

                if (velocity.sqrMagnitude < VelocityZeroThresholdMps * VelocityZeroThresholdMps)
                {
                    velocity = Vector3.zero;
                }

                state.linearVelocity = velocity;
            }
        }

        static void RemoveInwardNormalVelocity(ref Vector3 velocity, Vector3 normal)
        {
            float inwardSpeed = Vector3.Dot(velocity, normal);
            if (inwardSpeed < 0f)
            {
                velocity -= normal * inwardSpeed;
            }
        }
    }
}
