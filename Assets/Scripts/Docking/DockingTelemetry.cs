using UnityEngine;

namespace FlightModel.Docking
{
    /// <summary>
    /// Snapshot of relationship between ship docking node and the currently-targeted
    /// station docking port. All offsets and velocities are expressed in the docking
    /// camera frame: lateral X is camera right, lateral Y is camera up, closure velocity
    /// is along camera forward toward the target.
    /// </summary>
    public struct DockingTelemetry
    {
        public bool hasTarget;
        public bool shipPortDeployed;
        public bool targetPortAvailable;

        public Vector2 lateralOffsetMeters;
        public Vector2 lateralAxisOffsetMeters;
        public Vector2 lateralGuidanceDegrees;
        public Vector2 lateralNeedleNormalized;
        public Vector2 lateralVelocityMetersPerSecond;
        public float closureVelocityMetersPerSecond;
        public float closureDistanceMeters;
        public float rollOffsetDegrees;
        public float verticalOffsetMeters;
        public Vector2 angularAxisError;
        public float distanceMeters;

        // Set by the docking capture state machine (T11+). Docking HUD reads these.
        public bool magneticCaptureActive;
        public bool docked;
        public bool recaptureLockout;

        public static DockingTelemetry Empty => new DockingTelemetry
        {
            hasTarget = false,
        };
    }

    public static class DockingTelemetryUtility
    {
        /// <summary>
        /// Compute telemetry from the actual ship docking node pose and current
        /// target station port. All offsets are in the ship docking camera frame.
        /// Zero means the ship node position is on the target node position and
        /// the ship action axis is opposite the station port action axis.
        /// </summary>
        public static DockingTelemetry Compute(
            ShipDockingNode shipNode,
            Vector3 shipLinearVelocity,
            bool shipPortDeployed,
            StationDockingPort target)
        {
            if (shipNode == null)
            {
                return DockingTelemetry.Empty;
            }

            return ComputeFromPoses(
                shipNode.WorldPosition,
                shipNode.WorldRotation,
                shipNode.ActionAxisLocal,
                shipLinearVelocity,
                shipPortDeployed,
                target);
        }

        public static DockingTelemetry ComputeFromPoses(
            Vector3 shipNodePosition,
            Quaternion shipNodeRotation,
            Vector3 shipActionAxisLocal,
            Vector3 shipLinearVelocity,
            bool shipPortDeployed,
            StationDockingPort target)
        {
            DockingTelemetry t = new DockingTelemetry
            {
                hasTarget = target != null,
                targetPortAvailable = target != null && target.IsAvailable,
                shipPortDeployed = shipPortDeployed,
            };

            if (target == null)
            {
                return t;
            }

            DockingFrame shipFrame = DockingFrameUtility.FromPose(
                shipNodePosition,
                shipNodeRotation,
                shipActionAxisLocal);
            DockingFrame targetFrame = DockingFrameUtility.FromTransform(
                target.NodeTransform,
                target.ActionAxisLocal);

            Vector3 delta = targetFrame.position - shipFrame.position;
            Vector3 targetToShip = shipFrame.position - targetFrame.position;
            t.distanceMeters = delta.magnitude;
            t.closureDistanceMeters = Vector3.Dot(targetToShip, targetFrame.forward);

            Vector3 offsetInShipFrame = shipFrame.WorldToFrameVector(delta);
            t.lateralOffsetMeters.x = offsetInShipFrame.x;
            t.lateralOffsetMeters.y = offsetInShipFrame.y;
            t.verticalOffsetMeters = t.lateralOffsetMeters.y;

            Vector3 targetAxisToShip = targetToShip - targetFrame.forward * t.closureDistanceMeters;
            Vector3 targetAxisOffsetInShipFrame = shipFrame.WorldToFrameVector(-targetAxisToShip);
            t.lateralAxisOffsetMeters = new Vector2(
                targetAxisOffsetInShipFrame.x,
                targetAxisOffsetInShipFrame.y);

            float approachDistance = Mathf.Max(0.01f, Mathf.Abs(t.closureDistanceMeters));
            float guidanceX = Mathf.Atan2(t.lateralAxisOffsetMeters.x, approachDistance) * Mathf.Rad2Deg;
            float guidanceY = Mathf.Atan2(t.lateralAxisOffsetMeters.y, approachDistance) * Mathf.Rad2Deg;
            t.lateralGuidanceDegrees = new Vector2(guidanceX, guidanceY);

            const float needleFullScaleDegrees = 45f;
            t.lateralNeedleNormalized = new Vector2(
                Mathf.Clamp(guidanceX / needleFullScaleDegrees, -1f, 1f),
                Mathf.Clamp(guidanceY / needleFullScaleDegrees, -1f, 1f));

            float normalVelocity = Vector3.Dot(shipLinearVelocity, targetFrame.forward);
            float backHemisphereSign = t.closureDistanceMeters >= 0f ? 1f : -1f;
            t.closureVelocityMetersPerSecond = -normalVelocity * backHemisphereSign;
            Vector3 transverseVelocity = shipLinearVelocity - normalVelocity * targetFrame.forward;
            t.lateralVelocityMetersPerSecond.x = Vector3.Dot(transverseVelocity, shipFrame.right);
            t.lateralVelocityMetersPerSecond.y = Vector3.Dot(transverseVelocity, shipFrame.up);

            // A perfect face-to-face alignment has the ship action axis opposite
            // the station port action axis, so the desired ship forward is
            // -targetFrame.forward.
            Vector3 desiredShipForward = -targetFrame.forward;
            float forwardComponent = Vector3.Dot(desiredShipForward, shipFrame.forward);
            if (forwardComponent > 1e-3f)
            {
                float yaw = Mathf.Atan2(
                    Vector3.Dot(desiredShipForward, shipFrame.right),
                    forwardComponent);
                float pitch = Mathf.Atan2(
                    Vector3.Dot(desiredShipForward, shipFrame.up),
                    forwardComponent);
                t.angularAxisError = new Vector2(yaw, pitch);
            }

            Vector3 desiredUp = DockingFrame.ProjectOntoPlane(targetFrame.up, shipFrame.forward);
            Vector3 shipUp = DockingFrame.ProjectOntoPlane(shipFrame.up, shipFrame.forward);
            if (desiredUp.sqrMagnitude > 1e-6f && shipUp.sqrMagnitude > 1e-6f)
            {
                t.rollOffsetDegrees = Vector3.SignedAngle(
                    shipUp.normalized,
                    desiredUp.normalized,
                    shipFrame.forward);
            }

            return t;
        }
    }
}
