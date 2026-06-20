# P0.1 T12C-1 - Decouple Docking Mode, Docking Camera, And Docking Controls

## Goal

Make docking mode an explicit state independent from the active camera view.

After this ticket:

- `J` toggles docking mode on/off.
- `V` cycles cockpit/external when docking mode is off.
- `V` cycles cockpit/external/docking when docking mode is on.
- Docking HUD is visible only in docking camera.
- Docking-relative controls apply only in docking camera.
- This ticket does not change capture permission yet; T12C-2 will switch capture to the new explicit docking-mode state.

## Reading Instructions

Read before editing:

- `Docs/project_rules.md`
- `Tickets/P0_1_T12C_docking_mode_camera_and_timed_motion_overview.md`
- `Assets/Scripts/Input/ShipInputReader.cs`
- `Assets/Scripts/Input/KeyboardMouseInputProvider.cs`
- `Assets/Settings/InputProfiles/FlightInputActions.inputactions`
- `Assets/Scripts/Camera/ShipCameraController.cs`
- `Assets/Scripts/Docking/DockingModeController.cs`
- `Assets/Scripts/Docking/DockingInputTransformer.cs`
- `Assets/Scripts/Docking/ShipDockingNode.cs`
- `Assets/Scripts/Vehicles/ShipVehicle.cs`
- `Assets/Scripts/Player/LocalPlayerVehicleController.cs`

## Required Behavior

### Docking Mode

Add explicit state to `DockingModeController`:

```csharp
public bool IsDockingModeEnabled { get; private set; }
public bool IsDockingCameraActive => cameraController != null && cameraController.IsDockingActive;
public bool ShouldUseDockingControls => IsDockingModeEnabled && IsDockingCameraActive;
```

`ToggleDockingMode()` should:

- toggle `IsDockingModeEnabled`
- when enabling:
  - keep current camera view unchanged
  - make docking camera available in the `V` cycle
  - do not force docking camera
- when disabling:
  - if current view is docking camera, return to cockpit
  - hide docking HUD
  - keep ship docking port deployment unchanged

### Camera Cycle

Update camera cycling so `V` behaves as:

When docking mode is off:

```text
Cockpit <-> External
```

When docking mode is on:

```text
Cockpit -> External -> Docking -> Cockpit
```

Entering docking camera requires an authored `ShipDockingNode`. If the node is missing:

- skip docking camera
- log a clear warning/error
- do not create a node

Implementation can live in either `ShipCameraController` or `DockingModeController`, but the result must keep the concepts clean:

- `ShipCameraController` owns active view.
- `DockingModeController` owns whether docking mode is enabled and whether docking camera is allowed.

Recommended shape:

```csharp
public void ToggleView(bool dockingModeEnabled, ShipDockingNode dockingNode)
```

or:

```csharp
DockingModeController.ToggleCameraView()
```

No deferred decision: choose the smallest change that keeps `ShipVehicle.ToggleCameraView()` as the public vehicle-level call.

### Docking HUD

HUD visible only when both are true:

- docking mode is enabled
- active camera is docking camera

HUD hidden in cockpit/external even while docking mode is enabled.

### Docking Controls

Change `DockingModeController.TransformInput(...)`:

```csharp
if (!ShouldUseDockingControls)
{
    return pilotInput;
}

return DockingInputTransformer.Transform(pilotInput, shipState, dockingNode);
```

Do not apply docking-relative input in cockpit or external view.

## Input Binding

Confirm `J` is the binding for docking mode.

If current binding still uses `H` for docking camera/mode, update it so:

- `J` invokes `ToggleDockingModeRequested`
- `V` remains `ToggleCameraRequested`
- `H` does not toggle docking camera

Do not add a separate docking-camera key.

## Guardrails

- Do not change capture preconditions in this ticket.
- Do not edit magnetic capture timing in this ticket.
- Do not edit undock behavior in this ticket.
- Do not create missing docking nodes.
- Do not make docking mode deploy/retract the docking port.

## Verification

Build:

```powershell
dotnet build Assembly-CSharp.csproj --no-restore
```

Manual test:

- Docking mode off: `V` cycles cockpit/external only.
- Docking mode on: `V` cycles cockpit/external/docking.
- Docking mode on in cockpit/external: controls are normal.
- Docking mode on in docking camera: controls are docking-relative.
- Docking HUD appears only in docking camera.
- Turning docking mode off while in docking camera returns to cockpit and hides HUD.

## Acceptance Criteria

- Docking mode has explicit state.
- Camera view is not the source of truth for docking mode.
- Docking camera is available only while docking mode is enabled.
- Docking-relative controls apply only while viewing through docking camera.
- Existing flight, weapons, and docking HUD math still compile and function.

