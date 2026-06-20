# P0.1 T12C-4 - Timed Undock Separation

## Goal

Replace instant undock hop/teleport with visible separation over several frames.

Pressing `U` while docked should make the ship break contact and drift away from the docking port. The ship should not instantly appear `0.5m` or `1.25m` away.

## Reading Instructions

Read before editing:

- `Docs/project_rules.md`
- `Tickets/P0_1_T12C_docking_mode_camera_and_timed_motion_overview.md`
- `Tickets/P0_1_T12_manual_undock_and_recapture_lockout.md`
- `Tickets/P0_1_T12C_3_timed_magnetic_capture.md`
- `Assets/Scripts/Docking/DockingState.cs`
- `Assets/Scripts/Docking/DockingCaptureController.cs`
- `Assets/Scripts/Docking/DockableShip.cs`
- `Assets/Scripts/Docking/StationDockingPort.cs`
- `Assets/Scripts/Docking/ShipDockingNode.cs`
- `Assets/Scripts/Flight/ShipFlightController.cs`
- `Assets/Scripts/Flight/RcsThrusterVfx.cs`
- `Assets/Scripts/Authority/LocalGameAuthority.cs`

## Required Behavior

### State

Add a timed undocking state.

Preferred:

- add `Undocking` to `DockingState`

If adding enum state is too invasive, use an internal timed substate, but logs/HUD/debugging should still be clear that undocking is active.

No deferred decision: prefer adding `DockingState.Undocking` unless it causes a large unrelated cascade.

`IsFlightGated` should be true while:

- `MagneticCapture`
- `Docked`
- `Undocking`

### Config

Use these defaults:

```csharp
float undockSeparationDurationSeconds = 0.35f;
float undockOffsetMeters = 1.25f;
float undockVelocityMetersPerSecond = 0.5f;
float recaptureUnlockDistanceMeters = 1.0f;
```

Keep existing offset/velocity/lockout fields if present; add duration.

### Request Undock

When `RequestUndock()` is called while docked:

- do not instantly move to the final offset
- store docked port as the undock source
- store starting ship state
- compute target COG pose so the ship docking node ends at:

```text
port.ShipAttachTransform.position + port.WorldForward * undockOffsetMeters
```

- preserve docking-axis roll
- enter `Undocking`
- reset undock elapsed time

### During Undocking

Each fixed authority tick:

- keep flight input gated
- interpolate from start COG pose to target COG pose over duration
- write interpolated state with `ShipFlightController.OverwriteState`
- keep velocities zero or damped during the timed separation

When duration completes:

- write final pose exactly
- set linear velocity to `port.WorldForward * undockVelocityMetersPerSecond`
- set angular velocity to zero
- set `LastPort`
- clear `DockedPort`
- enter `RecaptureLockout`

### RCS Burst

Preferred future presentation:

- show a short RCS burst pushing off from the docking port

For this ticket:

- do not block completion on custom RCS burst VFX
- add a clear TODO or tiny hook only if the existing VFX API already supports one-shot directional bursts
- do not fake it with unrelated thrusters if it risks breaking RCS mapping

### Recapture Lockout

Keep current lockout behavior:

- capture disabled until ship docking node is at least `1.0m` from the last docked port

The timer motion should move the docking node away from the port, not the COG by a guessed offset.

## Guardrails

- Do not teleport on undock.
- Do not require docking mode for undock.
- Do not retract the docking port automatically.
- Do not use Unity physics joints.
- Do not add autopilot.
- Do not create missing docking nodes or ports.
- Do not break current docked-follow compatibility with animated ports.

## Verification

Build:

```powershell
dotnet build Assembly-CSharp.csproj --no-restore
```

Manual test:

- Dock successfully.
- Confirm docking mode has auto-disabled from T12C-2.
- Press `U`.
- Ship visibly separates over about `0.35s`.
- Ship docking node moves away from station attach transform along port outward axis.
- After separation completes, ship drifts outward slowly.
- Manual controls return after timed undock.
- Magnetic capture does not immediately re-grab the ship.
- Capture can work again after moving beyond recapture lockout and returning deliberately.

## Acceptance Criteria

- Undock is visibly animated/timed, not an instant hop.
- Flight input is gated only during the short undocking state.
- Final release velocity is outward from the station port.
- Recapture lockout still works.
- `U` works while docking mode is off.

