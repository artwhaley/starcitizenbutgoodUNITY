# P0.1 T12B-5 - Idle And Docked Activation Modes

## Goal

Use `VehicleOperationalState` to make ship subsystem activation explicit for local-piloted, unoccupied, docked-occupied, and docked-idle cases.

This is a conservative activation pass. Do not add full power management, distance LOD, or network relevance yet.

## Reading Instructions

Read before editing:

- `Docs/project_rules.md`
- `Tickets/P0_1_T12B_vehicle_possession_refactor_overview.md`
- `Tickets/P0_1_T12B_1_vehicle_and_pilot_seat_shell.md`
- `Tickets/P0_1_T12B_2_local_player_vehicle_controller_input_events.md`
- `Tickets/P0_1_T12B_3_authority_submission_and_presentation_ownership.md`
- `Tickets/P0_1_T12B_4_shrink_or_remove_player_ship_controller.md`
- `Assets/Scripts/Vehicles/ShipVehicle.cs`
- `Assets/Scripts/Vehicles/ShipPilotSeat.cs`
- `Assets/Scripts/Player/LocalPlayerVehicleController.cs`
- `Assets/Scripts/Docking/DockingCaptureController.cs`
- `Assets/Scripts/Flight/ShipPresentationController.cs`
- `Assets/Scripts/Authority/LocalGameAuthority.cs`

## Implementation

### 1. Define State Transitions

`ShipVehicle` should set operational state from simple facts:

- local pilot seated and not docked -> `PilotedLocal`
- local pilot seated and docked -> `DockedOccupied`
- no local pilot and docked -> `DockedIdle`
- no local pilot and not docked -> `IdleUnoccupied`

Do not implement remote/AI transitions yet. Leave explicit TODOs if useful.

### 2. Gate Local-Only Systems

When not locally piloted/occupied:

- local player controller must not submit flight input
- local camera ownership should detach or remain inactive
- local HUD should not show as if the player is piloting
- local weapon fire input should not fire the ship

When docked:

- docking follow/capture systems may remain active
- flight controls remain gated by docking state
- thrust VFX/audio should not respond to stale local input

Do not disable durable identity components such as `WorldEntity`.

Do not disable docking components in a way that breaks docked follow or undock.

### 3. Add Public Methods For Future Enter/Exit

Add simple public methods if not already present:

- `LocalPlayerVehicleController.EnterSeat(ShipPilotSeat seat)`
- `LocalPlayerVehicleController.ExitCurrentSeat()`

No keybind is required yet.

### 4. Keep Current Scene Start Behavior

`FlightTest` must still start with the player seated in the ship and able to fly.

## Guardrails

- Do not add on-foot movement.
- Do not add enter/exit animation.
- Do not add AI piloting.
- Do not add networking.
- Do not change docking math.
- Do not change controls.
- Do not disable systems needed for docking follow/undock.

## Verification

Build:

```powershell
dotnet build Assembly-CSharp.csproj --no-restore
dotnet build Assembly-CSharp-Editor.csproj --no-restore
```

Playtest normal path:

- Start seated.
- Fly/fire/camera/dock/undock works.
- While flying, `ShipVehicle.OperationalState == PilotedLocal`.
- While docked and occupied, state is `DockedOccupied`.

Manual unoccupied check:

- During Play mode, call `ExitCurrentSeat()` from inspector/debug path or temporarily expose a test button if one already exists.
- Confirm ship no longer responds to local flight/fire input.
- Durable ship object remains in scene.
- Re-enter seat through `EnterSeat(startingSeat)` and confirm control resumes.

Docked idle check:

- Dock ship.
- Exit current seat.
- Confirm state becomes `DockedIdle`.
- Confirm ship remains held/followed by docking system.
- Confirm local controls do not move/fire the ship.

## Acceptance Criteria

- Operational state reflects local possession and docking state.
- Unoccupied ships do not receive local input.
- Docked idle ships remain docked and stable.
- Current start-seated play loop remains intact.
