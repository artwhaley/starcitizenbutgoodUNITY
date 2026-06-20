# P0.1 T12B-4 - Shrink Or Remove PlayerShipController

## Goal

Remove `PlayerShipController` as a mega-script now that vehicle/seat/player-controller responsibilities have been split.

After this ticket, `PlayerShipController` should either be deleted or reduced to a thin temporary validation facade under 150 lines.

## Reading Instructions

Read before editing:

- `Docs/project_rules.md`
- `Tickets/P0_1_T12B_vehicle_possession_refactor_overview.md`
- `Tickets/P0_1_T12B_1_vehicle_and_pilot_seat_shell.md`
- `Tickets/P0_1_T12B_2_local_player_vehicle_controller_input_events.md`
- `Tickets/P0_1_T12B_3_authority_submission_and_presentation_ownership.md`
- `Assets/Scripts/Flight/PlayerShipController.cs`
- `Assets/Scripts/Vehicles/ShipVehicle.cs`
- `Assets/Scripts/Player/LocalPlayerVehicleController.cs`
- `Assets/Prefabs/Ships/PF_PlayerShip.prefab`
- `Assets/Scenes/FlightTest.unity`

## Implementation

### 1. Remove Remaining Responsibilities

`PlayerShipController` must not own:

- input event subscriptions
- fixed tick authority submission
- local presentation updates
- docking HUD telemetry updates
- docking mode hint updates
- weapon fire routing
- camera routing
- normal-path runtime component creation

Move any remaining needed logic into the new owner components, keeping methods small and explicit.

### 2. Delete Or Thin The Component

Preferred:

- remove `PlayerShipController` from `PF_PlayerShip`
- delete `Assets/Scripts/Flight/PlayerShipController.cs`
- remove stale compile references if needed

Acceptable transitional state:

- keep `PlayerShipController` as a validation facade under 150 lines
- it may verify required references exist and log clear errors
- it may call one explicit bootstrap method on `ShipVehicle`
- it must not silently repair missing required setup

### 3. Remove Normal-Path Runtime Repair

Search for normal-path runtime repair related to the player ship and remove or make it loud:

- `AddComponent<LocalGameAuthority>()`
- `AddComponent<ShipWeaponHardpoints>()`
- `AddComponent<RcsThrusterVfx>()`
- `AddComponent<EngineGlowVfx>()`
- `AddComponent<DockingCaptureController>()`
- `AddComponent<DockingModeController>()`
- `AddComponent<DockableShip>()`
- runtime-created docking nodes

Important distinction:

- Creating transient HUD/VFX/pool objects may remain where intentionally presentation-only.
- Creating required gameplay identity/control/pose components as hidden repair should not remain.

### 4. Update Prefab Explicit Wiring

Any component previously added by `PlayerShipController` at runtime must now be explicitly on the prefab or produce a clear missing-reference error.

Do not add fake transforms or fake gameplay nodes.

## Guardrails

- Do not change simulation math.
- Do not change docking math.
- Do not change controls.
- Do not change tuning.
- Do not make asteroid/network changes.
- Do not delete unrelated serialized prefab data.

## Verification

Build:

```powershell
dotnet build Assembly-CSharp.csproj --no-restore
dotnet build Assembly-CSharp-Editor.csproj --no-restore
```

Search:

```powershell
rg -n "class PlayerShipController|PlayerShipController" Assets/Scripts Assets/Prefabs Assets/Scenes
rg -n "AddComponent<.*(LocalGameAuthority|ShipWeaponHardpoints|RcsThrusterVfx|EngineGlowVfx|DockingCaptureController|DockingModeController|DockableShip)" Assets/Scripts
rg -n "new GameObject\\(\"node_docking|created runtime ShipDockingNode|default station|autopilot" Assets/Scripts
```

Expected:

- no mega-script behavior remains
- no fake docking/setup shortcuts exist
- any retained `PlayerShipController` is small and transitional

Playtest:

- Full fly/fire/camera/dock/capture/undock loop still works.

## Acceptance Criteria

- `PlayerShipController` is removed or reduced to a thin validation facade.
- Current behavior still works.
- Required ship setup is explicit, not silently repaired.
