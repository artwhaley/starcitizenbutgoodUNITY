# P0.1 T11 - Manual Magnetic Capture And Docked State

## Goal

Implement manual docking capture.

When the pilot manually aligns and moves the ship docking node close enough to a station docking port, a configurable magnetic capture activates. The magnet first applies physical/simulated attraction, then transitions to a nonphysical snap when close enough, attaches the ship docking node exactly to the station port, and enters docked state.

No autopilot. The player flies into the capture envelope manually.

## Reading Instructions

Read these files before editing:

- `Tickets/P0_1_T09_manual_docking_nodes_camera_and_hud.md`
- `Tickets/P0_1_T10_docking_mode_controls_and_camera_cycle.md`
- `Assets/Scripts/Docking/ShipDockingNode.cs`
- `Assets/Scripts/Docking/StationDockingPort.cs`
- `Assets/Scripts/Docking/DockingModeController.cs`
- `Assets/Scripts/Docking/DockingTelemetry.cs`
- `Assets/Scripts/Docking/DockingPortDeploymentState.cs`
- `Assets/Scripts/Flight/ShipFlightController.cs`
- `Assets/Scripts/Flight/ShipSimulator.cs` if it exists
- `Assets/Scripts/Flight/ShipState.cs`
- `Assets/Scripts/Authority/LocalGameAuthority.cs`
- `Assets/Scripts/World/ReferenceFrameId.cs` if T08 exists

Do not start until T09 and T10 are complete.

## Required Files

Create:

- `Assets/Scripts/Docking/DockingState.cs`
- `Assets/Scripts/Docking/DockableShip.cs`
- `Assets/Scripts/Docking/DockingCaptureSettings.cs`
- `Assets/Scripts/Docking/DockingCaptureController.cs`

Modify:

- `Assets/Scripts/Authority/LocalGameAuthority.cs`
- `Assets/Scripts/Flight/ShipFlightController.cs` or extracted simulator only as needed for state/snap support
- `Assets/Scripts/Docking/DockingHud.cs`
- `Assets/Prefabs/Ships/PF_PlayerShip.prefab`
- `Assets/Prefabs/Arena/PF_TestArena.prefab`

## Docking State

Use this state model:

```csharp
public enum DockingState
{
    FreeFlight = 0,
    DockingMode = 1,
    MagneticCapture = 2,
    Docked = 3,
    Undocking = 4,
    RecaptureLockout = 5
}
```

Definitions:

- `FreeFlight`: normal flight.
- `DockingMode`: pilot has docking camera/control mode active.
- `MagneticCapture`: ship is inside capture range and magnet is pulling/aliging it.
- `Docked`: ship node is snapped to station port.
- `Undocking`: later T12 transition.
- `RecaptureLockout`: later T12 lockout.

This ticket must implement through `Docked`. T12 finishes undocking/lockout.

## Capture Settings

`DockingCaptureSettings` should be serializable and include:

- `float captureDistanceMeters = 0.5f`
- `float snapDistanceMeters = 0.08f`
- `float maxCaptureAngleDegrees = 8f`
- `float maxRollAngleDegrees = 8f`
- `float maxClosureSpeedMetersPerSecond = 1.5f`
- `float magneticPositionStrength = 4f`
- `float magneticRotationStrength = 4f`
- `float maxMagneticAcceleration = 2f`
- `bool requireShipPortDeployed = true`
- `bool requireStationPortAvailable = true`

These are starting values. Keep them configurable in inspector.

## Capture Conditions

Magnetic capture may start only when:

- docking mode is active
- ship docking node is deployed/active
- there is a valid target `StationDockingPort`
- target station port is available/active
- ship node is within `captureDistanceMeters`
- ship node forward is nearly opposite station port forward
- roll offset is within tolerance
- closure speed is below max
- ship is not in recapture lockout

Do not capture when merely passing near a port in normal flight mode.

Do not capture when the ship docking node is retracted, even if the camera is in docking mode.

Do not capture when the station docking port is unavailable, disabled, retracting, or occupied.

## Magnetic Attraction

During `MagneticCapture`:

- Apply attraction/alignment through the ship simulation state, not Rigidbody physics.
- Pull the ship docking node toward the station port's `ShipAttachTransform`.
- Rotate the ship so ship docking node forward faces opposite the current attach transform's docking forward.
- Reduce relative velocity as it approaches.

Implementation can be simple:

- compute desired correction in world space
- convert to acceleration/velocity adjustment with clamped strength
- update `ShipState.linearVelocity`, `ShipState.angularVelocityRadians`, `ShipState.position`, and/or `ShipState.rotation`

This is allowed to be a special docking capture force. It is not an autopilot, because the player already manually entered the capture envelope.

Do not use Unity `FixedJoint`.

## Nonphysical Snap

When within `snapDistanceMeters` and angular tolerance:

- Compute the ship root/COG transform required to place `ShipDockingNode` exactly on the station port's `ShipAttachTransform`.
- Set ship state position/rotation to that exact pose.
- Set linear and angular velocity to zero.
- Set state to `Docked`.
- Disable normal flight input while docked.
- Keep camera in docking or external view for now.

The ship and station remain separate entities. Parenting the visual/root under station is allowed only as a presentation/local-frame convenience, not as the only stored truth.

Stored dock truth should include at least:

- docked station/port reference
- docked port ID
- current attach transform reference or equivalent port attachment ID
- local docking-node-to-ship-root offset needed to follow a moving attach transform
- previous frame/state needed to undock later

## Future Animated Port Compatibility

Do not assume a docked port is static.

This ticket does not implement retract/extend animations, but the code must allow this later:

```text
manual approach
-> magnetic capture
-> snap ship docking node to port attach transform
-> future animation retracts port attach transform
-> docked ship follows attach transform
-> future undock animation extends attach transform
-> release ship
```

Therefore:

- docked pose should be derived from the port's current `ShipAttachTransform`, not a one-time station-root pose
- if the attach transform moves while docked, the ship should be able to follow it
- do not parent the ship directly to the station root as the only way to stay docked
- if parenting is used as a temporary presentation shortcut, parent/follow the moving attach transform, not the station root
- keep enough offset data to set the ship state from the moving attach transform each tick

## HUD Feedback

Docking HUD should clearly show:

- magnetic capture active
- docked state active

Simple text is fine.

## Acceptance Criteria

- Project compiles:

```powershell
dotnet build Assembly-CSharp.csproj --no-restore
```

- Player can manually enter docking mode, align with target port, and fly into capture range.
- Magnetic capture does not activate while the ship docking node is retracted.
- Magnetic capture does not activate while the target station port is unavailable/inactive.
- Magnetic capture activates only inside configured range/tolerances.
- Capture pulls/alines the ship without using an autopilot path.
- Close-range snap places the ship docking node exactly at the station docking port.
- Docked pose is based on `StationDockingPort.ShipAttachTransform`.
- Ship enters `Docked` state.
- Ship velocities are zero while docked.
- Normal flight input does not move the ship while docked.
- Design does not prevent a future animated port attach transform from moving the docked ship.
- No Unity physics joint is used.
- No automatic approach from outside capture range is added.
- No networking is added.

## Guardrails

- Manual docking only.
- Do not implement long-range autodock.
- Do not use `ShipAutopilotRequestSource` for this.
- Do not let normal flight accidentally capture.
- Do not let retracted ship ports capture.
- Do not let unavailable station ports capture.
- Do not merge ship and station entities.
- Do not build station UI/trading.
- Do not implement undock lockout here; that is T12.
