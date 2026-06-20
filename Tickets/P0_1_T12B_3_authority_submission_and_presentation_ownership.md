# P0.1 T12B-3 - Authority Submission And Presentation Ownership

## Goal

Move continuous local driving of the possessed ship out of `PlayerShipController` and into `LocalPlayerVehicleController`.

After this ticket, the local player controller should submit input to the possessed vehicle's authority and apply local presentation. The ship remains a vehicle that can exist without local input.

## Reading Instructions

Read before editing:

- `Docs/project_rules.md`
- `Tickets/P0_1_T12B_vehicle_possession_refactor_overview.md`
- `Tickets/P0_1_T12B_1_vehicle_and_pilot_seat_shell.md`
- `Tickets/P0_1_T12B_2_local_player_vehicle_controller_input_events.md`
- `Assets/Scripts/Player/LocalPlayerVehicleController.cs`
- `Assets/Scripts/Vehicles/ShipVehicle.cs`
- `Assets/Scripts/Vehicles/ShipPilotSeat.cs`
- `Assets/Scripts/Flight/PlayerShipController.cs`
- `Assets/Scripts/Flight/ShipPresentationController.cs`
- `Assets/Scripts/Authority/LocalGameAuthority.cs`
- `Assets/Scripts/Docking/DockingModeController.cs`
- `Assets/Scripts/Docking/DockingCaptureController.cs`
- `Assets/Scripts/Input/ShipInputReader.cs`
- `Assets/Scripts/Weapons/PrimaryWeaponController.cs`

## Implementation

### 1. Move Fixed-Tick Local Input Submission

Move the logic equivalent to `PlayerShipController.FixedUpdate` into `LocalPlayerVehicleController`.

The new flow should:

- read/build current local command from `ShipInputReader`
- if no current possessed vehicle, submit nothing
- transform input through possessed vehicle's `DockingModeController` when docking mode is active
- suppress flight input while `DockingCaptureController.IsFlightGated`
- create `Authority.ClientInputCommand`
- submit it to possessed vehicle's `LocalGameAuthority`
- tick the possessed vehicle's `LocalGameAuthority`
- apply possessed vehicle presentation after the authority tick

Do not change `LocalGameAuthority` simulation math.

### 2. Move Local Presentation Update

Move the logic equivalent to `PlayerShipController.Update` local presentation handling into `LocalPlayerVehicleController`:

- weapon fire tick
- camera pan/tilt/zoom
- audio/presentation tick
- docking HUD telemetry update if still owned by local presentation
- docking mode hint update

If docking HUD update currently depends on private `PlayerShipController` methods, move that logic to either:

- `ShipVehicle` as a small delegation method, or
- `LocalPlayerVehicleController` as local presentation logic.

Do not change HUD math.

### 3. Make Unpossessed Path Explicit

If `LocalPlayerVehicleController` has no current seat/vehicle:

- do not submit input
- do not tick local camera/HUD for a ship
- do not fire weapons
- leave the ship entity existing

No on-foot controller is required yet.

### 4. Keep PlayerShipController Temporarily If Needed

`PlayerShipController` may remain for bootstrap/validation after this ticket, but it must not still submit input to authority or own local presentation updates.

## Guardrails

- Do not change input bindings.
- Do not change simulation math.
- Do not rewrite docking.
- Do not change weapon projectile behavior.
- Do not remove `PlayerShipController` completely in this ticket unless it is trivial and verified.
- Do not create fake setup at runtime.

## Verification

Build:

```powershell
dotnet build Assembly-CSharp.csproj --no-restore
dotnet build Assembly-CSharp-Editor.csproj --no-restore
```

Playtest:

- Ship flies normally from local input.
- Assist works.
- Camera works.
- Weapons fire.
- Docking camera/HUD works.
- Docking capture gates controls.
- Docked state holds.
- Undock works.
- If `LocalPlayerVehicleController` is temporarily unseated in inspector during Play, ship no longer responds to local flight/fire commands.

Search:

```powershell
rg -n "SubmitInput|authority.Tick|ApplySimulationState|TickPresentation|UpdateDockingHud|UpdateDockingModeHint" Assets/Scripts/Flight/PlayerShipController.cs
```

Expected: `PlayerShipController` no longer owns these local driving/presentation responsibilities.

## Acceptance Criteria

- Continuous local driving is owned by `LocalPlayerVehicleController`.
- A vehicle without local possession does not receive local input.
- Current play loop still works.
