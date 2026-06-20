using UnityEngine;
using FlightModel.World;

namespace FlightModel.Docking
{
    /// <summary>
    /// Manual-docking state machine. Owns transitions FreeFlight -> DockingMode ->
    /// MagneticCapture -> Docked -> Undocking -> RecaptureLockout.
    ///
    /// Phase 0.1: no autopilot, no physics joints. Magnetic capture and undock
    /// separation write directly to ShipState via ShipFlightController.OverwriteState.
    /// When docked, the position+rotation are re-derived each tick from
    /// StationDockingPort.ShipAttachTransform so future animated ports can move
    /// the docked ship without code changes here.
    /// </summary>
    public class DockingCaptureController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] DockableShip dockableShip;
        [SerializeField] ShipFlightController flight;
        [SerializeField] DockingModeController dockingModeController;

        [Header("Settings")]
        [SerializeField] DockingCaptureSettings settings = DockingCaptureSettings.Default;

        [Header("Undock / lockout")]
        [SerializeField] float magneticCaptureDurationSeconds = 0.5f;
        [SerializeField] float undockSeparationDurationSeconds = 0.35f;
        [SerializeField] float undockOffsetMeters = 1.25f;
        [SerializeField] float undockVelocityMetersPerSecond = 0.5f;
        [SerializeField] float recaptureUnlockDistanceMeters = 1.0f;
        [SerializeField] float dockedVelocityDecayPerSecond = 8f;
        [SerializeField] bool logCaptureDiagnostics = true;

        public DockingState State { get; private set; } = DockingState.FreeFlight;
        public StationDockingPort DockedPort { get; private set; }
        public StationDockingPort LastPort { get; private set; }
        public string LastCaptureBlockReason { get; private set; }

        StationDockingPort capturePort;
        float nextCaptureDiagnosticLogTime;
        ShipState captureStartState;
        Quaternion captureReferenceNodeWorldRotation = Quaternion.identity;
        float captureElapsedSeconds;
        ShipState undockStartState;
        StationDockingPort undockPort;
        Quaternion undockReferenceNodeWorldRotation = Quaternion.identity;
        float undockElapsedSeconds;

        public bool IsDocked => State == DockingState.Docked;
        public bool IsMagneticActive => State == DockingState.MagneticCapture;
        public bool IsLockedOut => State == DockingState.RecaptureLockout;
        public bool IsFlightGated =>
            State == DockingState.MagneticCapture
            || State == DockingState.Docked
            || State == DockingState.Undocking;

        public ShipCameraController CameraController { get; set; }

        public void Configure(
            DockableShip dockable,
            ShipFlightController flightController,
            DockingCaptureSettings captureSettings,
            DockingModeController modeController,
            ShipCameraController camera = null)
        {
            dockableShip = dockable;
            flight = flightController;
            dockingModeController = modeController;
            if (captureSettings != null)
            {
                settings = captureSettings;
            }

            CameraController = camera;
        }

        public bool RequestUndock()
        {
            if (State != DockingState.Docked)
            {
                return false;
            }

            return BeginTimedUndock();
        }

        /// <summary>
        /// Called from LocalGameAuthority.Tick AFTER ShipFlightController.Simulate.
        /// Magnet attraction in MagneticCapture, snap into Docked, follow attach
        /// transform while Docked, and tick the RecaptureLockout counter.
        /// </summary>
        public void TickOverride(float deltaSeconds)
        {
            if (flight == null || dockableShip == null)
            {
                return;
            }

            ShipDockingNode shipNode = dockableShip.ShipDockingNode;
            StationDockingPort targetPort = dockableShip.TargetProvider != null
                ? dockableShip.TargetProvider.CurrentTarget
                : null;
            StationDockingPort port = ResolveActivePort(targetPort);

            ShipState state = flight.State;

            // Pose the docking node would render at after the current ShipState
            // is applied to the COG transform. We do NOT trust the docking
            // node's Transform here because ShipPresentationController writes
            // cogTransform from ShipState in LateUpdate (one frame behind).
            Vector3 shipNodeOffsetFromCogLocal = GetNodeOffsetFromCog(shipNode);
            Quaternion shipNodeRotFromCogLocal = GetNodeRotationFromCog(shipNode);
            Vector3 shipNodeWorldPos = state.position + state.rotation * shipNodeOffsetFromCogLocal;
            Quaternion shipNodeWorldRot = state.rotation * shipNodeRotFromCogLocal;
            Vector3 shipNodeWorldForward = shipNodeWorldRot * GetShipActionAxisLocal(shipNode);

            switch (State)
            {
                case DockingState.FreeFlight:
                    if (DockingModeEnabled())
                    {
                        State = DockingState.DockingMode;
                    }
                    break;

                case DockingState.DockingMode:
                    if (TryStartMagneticCapture(shipNode, port, state, shipNodeWorldPos, shipNodeWorldForward))
                    {
                        BeginMagneticCapture(port, state, shipNodeWorldRot);
                    }
                    else if (!DockingModeEnabled())
                    {
                        capturePort = null;
                        State = DockingState.FreeFlight;
                    }
                    else
                    {
                        MaybeLogCaptureDiagnostic(port, shipNodeWorldPos);
                    }
                    break;

                case DockingState.MagneticCapture:
                    if (!DockingModeEnabled())
                    {
                        capturePort = null;
                        State = DockingState.FreeFlight;
                        break;
                    }

                    if (shipNode == null || port == null || port.ShipAttachTransform == null)
                    {
                        capturePort = null;
                        State = DockingState.DockingMode;
                        break;
                    }

                    ShipState magneticState = ApplyTimedMagneticCapture(
                        state,
                        port,
                        shipNodeOffsetFromCogLocal,
                        shipNodeRotFromCogLocal,
                        deltaSeconds,
                        out bool captureComplete);
                    Vector3 magneticNodeWorldPos = magneticState.position
                        + magneticState.rotation * shipNodeOffsetFromCogLocal;

                    if (captureComplete)
                    {
                        DockedPort = port;
                        capturePort = null;
                        State = DockingState.Docked;
                        dockingModeController?.SetDockingModeEnabled(false);
                    }
                    else if (!MagneticCaptureStillValid(port, magneticNodeWorldPos))
                    {
                        capturePort = null;
                        State = DockingModeEnabled()
                            ? DockingState.DockingMode
                            : DockingState.FreeFlight;
                    }
                    break;

                case DockingState.Docked:
                    if (port == null
                        || port.ShipAttachTransform == null
                        || !port.IsAvailable
                        || shipNode == null)
                    {
                        DockedPort = null;
                        capturePort = null;
                        State = DockingModeEnabled()
                            ? DockingState.DockingMode
                            : DockingState.FreeFlight;
                        break;
                    }

                    ApplyDockedFollow(
                        state,
                        shipNodeWorldRot,
                        port,
                        shipNodeOffsetFromCogLocal,
                        shipNodeRotFromCogLocal);
                    break;

                case DockingState.Undocking:
                    TickTimedUndock(shipNode, port, shipNodeOffsetFromCogLocal, shipNodeRotFromCogLocal, deltaSeconds);
                    break;

                case DockingState.RecaptureLockout:
                    TickRecaptureLockout(port, shipNodeWorldPos);
                    break;
            }
        }

        bool DockingModeEnabled()
            => dockingModeController != null && dockingModeController.IsDockingModeEnabled;

        StationDockingPort ResolveActivePort(StationDockingPort targetPort)
        {
            return State switch
            {
                DockingState.MagneticCapture => capturePort != null ? capturePort : targetPort,
                DockingState.Docked => DockedPort,
                DockingState.Undocking => undockPort != null ? undockPort : DockedPort,
                DockingState.RecaptureLockout => LastPort != null ? LastPort : targetPort,
                _ => targetPort
            };
        }

        void BeginMagneticCapture(
            StationDockingPort port,
            in ShipState state,
            Quaternion shipNodeWorldRot)
        {
            capturePort = port;
            captureStartState = state;
            captureReferenceNodeWorldRotation = shipNodeWorldRot;
            captureElapsedSeconds = 0f;
            State = DockingState.MagneticCapture;
        }

        bool TryStartMagneticCapture(
            ShipDockingNode shipNode,
            StationDockingPort port,
            in ShipState state,
            Vector3 shipNodeWorldPos,
            Vector3 shipNodeWorldForward)
        {
            if (shipNode == null || port == null || port.ShipAttachTransform == null)
            {
                LastCaptureBlockReason = "missing ship node, target port, or target attach transform";
                return false;
            }

            return CapturePreconditionsHold(shipNode, port, state.linearVelocity, shipNodeWorldPos, shipNodeWorldForward);
        }

        bool CapturePreconditionsHold(
            ShipDockingNode shipNode,
            StationDockingPort port,
            Vector3 shipLinearVelocity,
            Vector3 shipNodeWorldPos,
            Vector3 shipNodeWorldForward)
        {
            if (shipNode == null || port == null || port.ShipAttachTransform == null)
            {
                LastCaptureBlockReason = "missing ship node, target port, or target attach transform";
                return false;
            }

            if (settings.requireShipPortDeployed && !shipNode.IsDockingActive)
            {
                LastCaptureBlockReason = "docking mode is disabled";
                return false;
            }

            if (settings.requireStationPortAvailable && !port.IsAvailable)
            {
                LastCaptureBlockReason = $"station port '{port.PortId}' is not available";
                return false;
            }

            Vector3 delta = port.ShipAttachTransform.position - shipNodeWorldPos;
            float distance = delta.magnitude;
            if (distance > settings.captureDistanceMeters)
            {
                LastCaptureBlockReason =
                    $"distance {distance:0.000}m > capture {settings.captureDistanceMeters:0.000}m";
                return false;
            }

            // Closing angle: ship node forward must point roughly opposite the
            // port's outward direction (within maxCaptureAngleDegrees).
            float closingAngle = 180f - Vector3.Angle(port.WorldForward, shipNodeWorldForward);
            if (closingAngle > settings.maxCaptureAngleDegrees)
            {
                LastCaptureBlockReason =
                    $"axis angle {closingAngle:0.0}deg > capture {settings.maxCaptureAngleDegrees:0.0}deg";
                return false;
            }

            // Closure-speed check: signed projection along -port.WorldForward.
            // Reject if closure (positive) exceeds the cap, or if it's negative
            // (ship is moving away from the port).
            float closureSpeed = Vector3.Dot(shipLinearVelocity, -port.WorldForward);
            if (closureSpeed > settings.maxClosureSpeedMetersPerSecond)
            {
                LastCaptureBlockReason =
                    $"closure speed {closureSpeed:0.00}m/s > max {settings.maxClosureSpeedMetersPerSecond:0.00}m/s";
                return false;
            }
            if (closureSpeed < -0.1f)
            {
                LastCaptureBlockReason =
                    $"ship is moving away from port, closure speed {closureSpeed:0.00}m/s";
                return false;
            }

            LastCaptureBlockReason =
                $"ready: distance {distance:0.000}m, angle {closingAngle:0.0}deg, closure {closureSpeed:0.00}m/s";
            return true;
        }

        void MaybeLogCaptureDiagnostic(StationDockingPort port, Vector3 shipNodeWorldPos)
        {
            if (!logCaptureDiagnostics || Time.unscaledTime < nextCaptureDiagnosticLogTime)
            {
                return;
            }

            if (port == null || port.ShipAttachTransform == null)
            {
                return;
            }

            float diagnosticRange = Mathf.Max(settings.captureDistanceMeters * 2f, 1f);
            float distance = (port.ShipAttachTransform.position - shipNodeWorldPos).magnitude;
            if (distance > diagnosticRange)
            {
                return;
            }

            Debug.Log(
                $"DockingCaptureController: capture blocked near '{port.PortId}': {LastCaptureBlockReason}",
                this);
            nextCaptureDiagnosticLogTime = Time.unscaledTime + 1f;
        }

        bool MagneticCaptureStillValid(StationDockingPort port, Vector3 shipNodeWorldPos)
        {
            if (port == null || port.ShipAttachTransform == null)
            {
                LastCaptureBlockReason = "capture target was lost";
                return false;
            }

            float breakDistance = Mathf.Max(settings.captureDistanceMeters * 3f, settings.captureDistanceMeters + 0.5f);
            float distance = (port.ShipAttachTransform.position - shipNodeWorldPos).magnitude;
            if (distance > breakDistance)
            {
                LastCaptureBlockReason =
                    $"magnetic capture broke: distance {distance:0.000}m > break {breakDistance:0.000}m";
                return false;
            }

            return true;
        }

        bool TrySnapToPort(
            in ShipState state,
            Quaternion shipNodeWorldRot,
            StationDockingPort port,
            Vector3 shipNodeOffsetFromCogLocal,
            Quaternion shipNodeRotFromCogLocal)
        {
            if (port == null || port.ShipAttachTransform == null)
            {
                return false;
            }

            Vector3 delta = port.ShipAttachTransform.position
                - (state.position + state.rotation * shipNodeOffsetFromCogLocal);
            if (delta.sqrMagnitude > settings.snapDistanceMeters * settings.snapDistanceMeters)
            {
                return false;
            }

            flight.OverwriteState(
                ComputeDesiredCogPose(
                    -port.WorldForward,
                    port.ShipAttachTransform.position,
                    shipNodeWorldRot,
                    shipNodeOffsetFromCogLocal,
                    shipNodeRotFromCogLocal));
            return true;
        }

        void ApplyDockedFollow(
            in ShipState state,
            Quaternion shipNodeWorldRot,
            StationDockingPort port,
            Vector3 shipNodeOffsetFromCogLocal,
            Quaternion shipNodeRotFromCogLocal)
        {
            // Re-derive COG pose every tick from the current attach transform
            // so a future animated port can move the ship along.
            ShipState next = ComputeDesiredCogPose(
                -port.WorldForward,
                port.ShipAttachTransform.position,
                shipNodeWorldRot,
                shipNodeOffsetFromCogLocal,
                shipNodeRotFromCogLocal);

            // Bleed residual velocities so a teleport of the attach transform
            // doesn't leave the ship drifting.
            float dt = Time.fixedDeltaTime;
            float decay = Mathf.Exp(-dt * dockedVelocityDecayPerSecond);
            next.linearVelocity = state.linearVelocity * decay;
            next.angularVelocityRadians = state.angularVelocityRadians * decay;
            flight.OverwriteState(next);
        }

        ShipState ApplyTimedMagneticCapture(
            in ShipState state,
            StationDockingPort port,
            Vector3 shipNodeOffsetFromCogLocal,
            Quaternion shipNodeRotFromCogLocal,
            float deltaSeconds,
            out bool captureComplete)
        {
            ShipState desired = ComputeDesiredCogPose(
                -port.WorldForward,
                port.ShipAttachTransform.position,
                captureReferenceNodeWorldRotation,
                shipNodeOffsetFromCogLocal,
                shipNodeRotFromCogLocal);

            float duration = Mathf.Max(0.01f, magneticCaptureDurationSeconds);
            captureElapsedSeconds = Mathf.Min(captureElapsedSeconds + deltaSeconds, duration);
            float t = Mathf.Clamp01(captureElapsedSeconds / duration);
            float eased = SmoothStep(t);

            ShipState next = desired;
            next.position = Vector3.Lerp(captureStartState.position, desired.position, eased);
            next.rotation = Quaternion.Slerp(captureStartState.rotation, desired.rotation, eased);
            next.linearVelocity = Vector3.zero;
            next.angularVelocityRadians = Vector3.zero;

            captureComplete = captureElapsedSeconds >= duration;
            if (captureComplete)
            {
                next.position = desired.position;
                next.rotation = desired.rotation;
            }

            flight.OverwriteState(next);
            return next;
        }

        bool BeginTimedUndock()
        {
            if (flight == null || dockableShip == null || DockedPort == null)
            {
                State = DockingState.RecaptureLockout;
                LastPort = DockedPort;
                DockedPort = null;
                return true;
            }

            ShipDockingNode shipNode = dockableShip.ShipDockingNode;
            StationDockingPort port = DockedPort;
            ShipState state = flight.State;

            Quaternion nodeRot = GetNodeRotationFromCog(shipNode);
            undockStartState = state;
            undockPort = port;
            undockReferenceNodeWorldRotation = state.rotation * nodeRot;
            undockElapsedSeconds = 0f;
            State = DockingState.Undocking;
            return true;
        }

        void TickTimedUndock(
            ShipDockingNode shipNode,
            StationDockingPort port,
            Vector3 shipNodeOffsetFromCogLocal,
            Quaternion shipNodeRotFromCogLocal,
            float deltaSeconds)
        {
            StationDockingPort activePort = undockPort != null ? undockPort : port;
            if (flight == null || activePort == null || activePort.ShipAttachTransform == null || shipNode == null)
            {
                LastPort = activePort;
                DockedPort = null;
                undockPort = null;
                State = DockingState.RecaptureLockout;
                return;
            }

            Vector3 outward = activePort.WorldForward;
            Vector3 desiredShipNodeWorldPos =
                activePort.ShipAttachTransform.position + outward * undockOffsetMeters;
            ShipState desired = ComputeDesiredCogPose(
                -outward,
                desiredShipNodeWorldPos,
                undockReferenceNodeWorldRotation,
                shipNodeOffsetFromCogLocal,
                shipNodeRotFromCogLocal);

            float duration = Mathf.Max(0.01f, undockSeparationDurationSeconds);
            undockElapsedSeconds = Mathf.Min(undockElapsedSeconds + deltaSeconds, duration);
            float t = Mathf.Clamp01(undockElapsedSeconds / duration);
            float eased = SmoothStep(t);

            ShipState next = desired;
            next.position = Vector3.Lerp(undockStartState.position, desired.position, eased);
            next.rotation = Quaternion.Slerp(undockStartState.rotation, desired.rotation, eased);
            next.linearVelocity = Vector3.zero;
            next.angularVelocityRadians = Vector3.zero;

            bool complete = undockElapsedSeconds >= duration;
            if (complete)
            {
                next.position = desired.position;
                next.rotation = desired.rotation;
                next.linearVelocity = outward * undockVelocityMetersPerSecond;
            }

            flight.OverwriteState(next);

            if (!complete)
            {
                return;
            }

            LastPort = activePort;
            DockedPort = null;
            undockPort = null;
            State = DockingState.RecaptureLockout;
        }

        void TickRecaptureLockout(StationDockingPort currentPort, Vector3 shipNodeWorldPos)
        {
            if (LastPort == null || LastPort.ShipAttachTransform == null)
            {
                LastCaptureBlockReason = "recapture lockout cleared because previous port was lost";
                LastPort = null;
                State = DockingModeEnabled()
                    ? DockingState.DockingMode
                    : DockingState.FreeFlight;
                return;
            }

            float dist = (LastPort.ShipAttachTransform.position - shipNodeWorldPos).magnitude;
            if (dist >= recaptureUnlockDistanceMeters)
            {
                LastCaptureBlockReason =
                    $"recapture lockout cleared: distance {dist:0.000}m >= {recaptureUnlockDistanceMeters:0.000}m";
                LastPort = null;
                State = DockingModeEnabled()
                    ? DockingState.DockingMode
                    : DockingState.FreeFlight;
                return;
            }

            LastCaptureBlockReason =
                $"recapture lockout: distance {dist:0.000}m < {recaptureUnlockDistanceMeters:0.000}m";
        }

        // ----- pose helpers --------------------------------------------------

        Vector3 GetNodeOffsetFromCog(ShipDockingNode shipNode)
        {
            if (shipNode == null || shipNode.NodeTransform == null)
            {
                return Vector3.zero;
            }

            Transform root = dockableShip != null ? dockableShip.ShipRoot : null;
            if (root == null)
            {
                return shipNode.NodeTransform.localPosition;
            }

            return root.InverseTransformPoint(shipNode.NodeTransform.position);
        }

        Quaternion GetNodeRotationFromCog(ShipDockingNode shipNode)
        {
            if (shipNode == null || shipNode.NodeTransform == null)
            {
                return Quaternion.identity;
            }

            Transform root = dockableShip != null ? dockableShip.ShipRoot : null;
            if (root == null)
            {
                return shipNode.NodeTransform.localRotation;
            }

            return Quaternion.Inverse(root.rotation) * shipNode.NodeTransform.rotation;
        }

        static Vector3 GetShipActionAxisLocal(ShipDockingNode shipNode)
        {
            if (shipNode == null || shipNode.ActionAxisLocal.sqrMagnitude < 1e-6f)
            {
                return BlenderImportedAxes.DefaultActionAxisLocal;
            }

            return shipNode.ActionAxisLocal.normalized;
        }

        static Quaternion ComputeDesiredShipNodeRotation(
            Vector3 desiredForward,
            Quaternion currentShipNodeWorldRotation)
        {
            if (desiredForward.sqrMagnitude < 1e-6f)
            {
                return currentShipNodeWorldRotation;
            }

            desiredForward.Normalize();
            Vector3 currentForward = currentShipNodeWorldRotation * BlenderImportedAxes.DefaultActionAxisLocal;
            if (currentForward.sqrMagnitude < 1e-6f)
            {
                return currentShipNodeWorldRotation;
            }

            currentForward.Normalize();
            return Quaternion.FromToRotation(currentForward, desiredForward) * currentShipNodeWorldRotation;
        }

        static float SmoothStep(float t)
            => t * t * (3f - 2f * t);

        static Quaternion ComputeDesiredCogRotation(
            Vector3 desiredShipForward,
            Quaternion currentShipNodeWorldRotation,
            Quaternion shipNodeRotFromCogLocal)
        {
            Quaternion desiredShipNodeRot = ComputeDesiredShipNodeRotation(
                desiredShipForward,
                currentShipNodeWorldRotation);
            return desiredShipNodeRot * Quaternion.Inverse(shipNodeRotFromCogLocal);
        }

        /// <summary>
        /// Compute the COG pose that places the docking node exactly at the
        /// given world position with the given action direction. Velocity and
        /// angular velocity are zeroed.
        /// </summary>
        ShipState ComputeDesiredCogPose(
            Vector3 desiredShipNodeForward,
            Vector3 desiredShipNodeWorldPos,
            Quaternion currentShipNodeWorldRotation,
            Vector3 shipNodeOffsetFromCogLocal,
            Quaternion shipNodeRotFromCogLocal)
        {
            Quaternion desiredCogRot = ComputeDesiredCogRotation(
                desiredShipNodeForward,
                currentShipNodeWorldRotation,
                shipNodeRotFromCogLocal);
            Vector3 desiredCogPos = desiredShipNodeWorldPos
                - desiredCogRot * shipNodeOffsetFromCogLocal;

            return new ShipState
            {
                position = desiredCogPos,
                rotation = desiredCogRot,
                linearVelocity = Vector3.zero,
                angularVelocityRadians = Vector3.zero,
                assistMode = flight != null ? flight.State.assistMode : FlightAssistMode.AssistOff,
                frameId = flight != null ? flight.State.frameId : ReferenceFrameId.LocalZone,
                boostActive = false,
                fineControlActive = true,
                currentMassKg = flight != null ? flight.State.currentMassKg : 0f,
                remainingFuelKg = flight != null ? flight.State.remainingFuelKg : 0f,
                remainingHypergolicKg = flight != null ? flight.State.remainingHypergolicKg : 0f,
                appliedOutput = default
            };
        }
    }
}
