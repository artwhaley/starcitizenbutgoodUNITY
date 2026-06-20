# P0.1 T12C-3 - Timed Magnetic Capture Motion

## Goal

Replace instant-looking magnetic capture with visible motion over several frames.

When capture starts, the ship should visibly move from its current pose into the docked pose over about `0.5s` by default. The docking node, not the ship COG, is the reference point that must end at the station attach transform.

## Reading Instructions

Read before editing:

- `Docs/project_rules.md`
- `Tickets/P0_1_T12C_docking_mode_camera_and_timed_motion_overview.md`
- `Tickets/P0_1_T12C_2_capture_eligibility_and_auto_mode_off.md`
- `Assets/Scripts/Docking/DockingCaptureController.cs`
- `Assets/Scripts/Docking/DockingCaptureSettings.cs`
- `Assets/Scripts/Docking/DockableShip.cs`
- `Assets/Scripts/Docking/StationDockingPort.cs`
- `Assets/Scripts/Docking/ShipDockingNode.cs`
- `Assets/Scripts/Flight/ShipFlightController.cs`
- `Assets/Scripts/Flight/ShipState.cs`
- `Assets/Scripts/Authority/LocalGameAuthority.cs`

## Required Behavior

### Config

Add configurable fields to `DockingCaptureController` or `DockingCaptureSettings`.

Use these defaults:

```csharp
float magneticCaptureDurationSeconds = 0.5f;
AnimationCurve magneticCaptureCurve = smoothstep-equivalent if practical;
```

If avoiding `AnimationCurve` for simplicity, use:

```csharp
float t = Mathf.Clamp01(elapsed / duration);
float eased = t * t * (3f - 2f * t);
```

No deferred decision: use smoothstep math unless the existing codebase already has a curve helper.

### Start Capture

When state changes `DockingMode -> MagneticCapture`:

- store starting `ShipState`
- store capture target port
- store starting COG pose
- compute desired final COG pose from:
  - desired ship docking node world position = `port.ShipAttachTransform.position`
  - desired ship docking node forward = `-port.WorldForward`
  - preserve ship roll around the docking axis
  - use COG-relative docking node offset/rotation, not node local position unless guaranteed
- reset capture elapsed time to zero

### During Magnetic Capture

Each fixed authority tick:

- ignore player flight input through existing flight gate
- recompute desired target pose from the current port attach transform so future animated ports remain possible
- advance elapsed time
- interpolate COG position and rotation from start to desired target
- write the interpolated `ShipState` through `ShipFlightController.OverwriteState`
- zero or strongly damp linear/angular velocity during the capture
- do not let normal ship thrust fight the capture

Final snap:

- when elapsed reaches duration, snap exactly to final pose
- transition to `Docked`
- snap should be tiny because interpolation already reached the target

### Axis And Roll

Do not require or force roll alignment around the docking axis.

The desired rotation should align the ship docking node action axis to `-port.WorldForward` while preserving current ship roll around that axis as much as possible.

Do not reintroduce port-up matching as a capture requirement.

## Guardrails

- Do not move COG directly to the station attach point.
- Do not use `node.localPosition` as COG-relative offset unless node is guaranteed direct child of COG.
- Do not use Unity physics forces or joints.
- Do not add autopilot.
- Do not create missing nodes or ports.
- Do not change undock in this ticket.

## Verification

Build:

```powershell
dotnet build Assembly-CSharp.csproj --no-restore
```

Manual test:

- Enter capture envelope with docking mode on.
- Ship visibly pulls in over about half a second, not instantly.
- Ship docking node ends colocated with station attach transform.
- COG remains offset naturally if the docking node is not at ship center.
- Capture works at different roll angles around docking axis.
- Controls cannot fight magnetic capture.
- Final docked state remains stable.

## Acceptance Criteria

- Magnetic capture takes visible time.
- Default duration is about `0.5s`.
- Final docked pose is exact.
- Roll around docking axis is not a capture blocker.
- Capture remains compatible with future animated attach transforms.

