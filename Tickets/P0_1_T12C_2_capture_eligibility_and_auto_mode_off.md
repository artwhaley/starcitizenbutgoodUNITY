# P0.1 T12C-2 - Capture Eligibility Uses Docking Mode, Not Docking Camera

## Goal

Make magnetic capture depend on explicit docking mode, not the docking camera.

After this ticket:

- capture can begin from cockpit, external, or docking camera
- capture can begin only when docking mode is enabled
- docking camera is not required for capture
- docking mode automatically turns off when the ship reaches `Docked`
- `U` can undock while docking mode is off

## Reading Instructions

Read before editing:

- `Docs/project_rules.md`
- `Tickets/P0_1_T12C_docking_mode_camera_and_timed_motion_overview.md`
- `Tickets/P0_1_T12C_1_decouple_docking_mode_camera_and_controls.md`
- `Assets/Scripts/Docking/DockingState.cs`
- `Assets/Scripts/Docking/DockingModeController.cs`
- `Assets/Scripts/Docking/DockingCaptureController.cs`
- `Assets/Scripts/Docking/DockableShip.cs`
- `Assets/Scripts/Docking/DockingTargetProvider.cs`
- `Assets/Scripts/Vehicles/ShipVehicle.cs`
- `Assets/Scripts/Authority/LocalGameAuthority.cs`

## Required Behavior

### Capture Eligibility

`DockingCaptureController` must stop using active docking camera as the condition for capture mode.

Replace camera-based logic with explicit docking-mode logic.

Recommended implementation:

- add serialized/configured reference:

```csharp
[SerializeField] DockingModeController dockingModeController;
```

- add configure parameter or property so `ShipVehicle` wires it:

```csharp
dockingCaptureController.Configure(..., dockingModeController, ...);
```

- derive:

```csharp
bool DockingModeEnabled =>
    dockingModeController != null && dockingModeController.IsDockingModeEnabled;
```

State transitions:

- `FreeFlight -> DockingMode` when `DockingModeEnabled`
- `DockingMode -> FreeFlight` when not enabled
- `DockingMode -> MagneticCapture` when all capture preconditions pass
- `MagneticCapture -> Docked` when capture completes
- `MagneticCapture -> FreeFlight` or `DockingMode` if docking mode is disabled before docked
- `Docked` follow continues regardless of docking mode
- `RecaptureLockout` exits to `DockingMode` only if docking mode is enabled; otherwise exits to `FreeFlight`

### Auto Disable Docking Mode On Docked

When capture completes and state becomes `Docked`:

- call a method on `DockingModeController` like `SetDockingModeEnabled(false)`
- if active view is docking camera, return to cockpit
- hide docking HUD
- do not undock
- do not retract the ship docking node

Do not require docking mode for `U` undock.

### HUD Flags

Docking HUD telemetry may still display magnetic/docked/lockout flags when visible, but visibility remains controlled by T12C-1:

- docking HUD visible only in docking camera
- docking camera available only while docking mode is on

Once docking mode auto-disables on dock, HUD should hide because docking camera should exit.

## Guardrails

- Do not change capture math except replacing camera-mode checks with docking-mode checks.
- Do not change magnetic pull duration here; T12C-3 owns timed motion.
- Do not change undock movement here; T12C-4 owns timed undock.
- Do not make camera view a capture precondition.
- Do not make docking mode required for undock.

## Verification

Build:

```powershell
dotnet build Assembly-CSharp.csproj --no-restore
```

Manual test:

- Docking mode off, inside capture envelope: no capture.
- Docking mode on, cockpit view, inside capture envelope: capture starts.
- Docking mode on, external view, inside capture envelope: capture starts.
- Docking mode on, docking view, inside capture envelope: capture starts.
- On docked, docking mode turns off and docking camera exits to cockpit if needed.
- Pressing `U` while docked works even though docking mode is off.

## Acceptance Criteria

- Capture eligibility uses explicit docking mode state.
- Docking camera is not required for capture.
- Docking mode auto-disables on docked.
- Undock still works while docking mode is off.

