# P0.1 T12C-5 - Docking Playtest And Regression Checks

## Goal

Run a focused full docking regression after T12C-1 through T12C-4.

This ticket is intentionally verification-heavy. Do not add new features unless a test fails and the fix is directly required by this stack.

## Reading Instructions

Read before testing/fixing:

- `Docs/project_rules.md`
- `Tickets/P0_1_T12C_docking_mode_camera_and_timed_motion_overview.md`
- `Tickets/P0_1_T12C_1_decouple_docking_mode_camera_and_controls.md`
- `Tickets/P0_1_T12C_2_capture_eligibility_and_auto_mode_off.md`
- `Tickets/P0_1_T12C_3_timed_magnetic_capture.md`
- `Tickets/P0_1_T12C_4_timed_undock_separation.md`
- `Assets/Scripts/Docking/DockingCaptureController.cs`
- `Assets/Scripts/Docking/DockingModeController.cs`
- `Assets/Scripts/Camera/ShipCameraController.cs`
- `Assets/Scripts/Vehicles/ShipVehicle.cs`
- `Assets/Scenes/FlightTest.unity`

## Required Checks

### Build

Run:

```powershell
dotnet build Assembly-CSharp.csproj --no-restore
dotnet build Assembly-CSharp-Editor.csproj --no-restore
```

If the editor build fails from a transient file lock, retry once after a short delay. Do not ignore real compiler errors.

### Camera / Mode Matrix

Verify:

- docking mode off: `V` cycles cockpit/external only
- docking mode on: `V` cycles cockpit/external/docking
- docking HUD visible only in docking camera
- docking-relative controls only in docking camera
- cockpit/external controls stay normal even while docking mode is on

### Capture Matrix

Verify capture can start from:

- cockpit view with docking mode on
- external view with docking mode on
- docking camera with docking mode on

Verify capture cannot start from:

- cockpit view with docking mode off
- external view with docking mode off

### Capture Motion

Verify:

- magnetic capture visibly pulls over about `0.5s`
- ship does not get hurled away
- roll around docking axis does not prevent capture
- final docked pose is stable
- docking node, not COG, ends at station attach point
- docking mode auto-disables on `Docked`
- docking HUD hides after auto-disable

### Undock Motion

Verify:

- pressing `U` while docked starts timed undock
- docking mode does not need to be on
- ship separates visibly over about `0.35s`
- ship drifts outward after release
- controls return after timed undock
- recapture does not happen until lockout distance clears

### Regression Checks

Also check:

- normal flight still works
- assist modes still toggle
- brakes still work
- primary weapons still spawn from gun nodes
- RCS/main engine VFX still respond
- docking HUD math still matches approach geometry
- no fake docking nodes/ports/hardpoints are created

## Fix Scope

If a check fails:

- fix only the behavior covered by T12C
- do not refactor unrelated systems
- do not touch asteroid/networking tickets
- do not rewrite docking HUD math unless HUD math is the failing check

## Acceptance Criteria

- Runtime and editor builds pass.
- All matrix checks pass.
- No new hidden runtime authoring fixes are introduced.
- Docking feels manual and controllable:
  - `J` arms capture
  - `V` chooses view
  - docking-relative controls are camera-specific
  - capture and undock move visibly instead of teleporting

