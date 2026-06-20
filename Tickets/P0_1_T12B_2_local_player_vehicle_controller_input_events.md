# P0.1 T12B-2 - Local Player Vehicle Controller Input Events

## Goal

Add `LocalPlayerVehicleController` and move local input event subscriptions out of `PlayerShipController`, while leaving the fixed-tick authority submission path unchanged.

After this ticket, one-shot actions such as assist, camera toggle, docking mode, port deployment, undock, and bindings panel should be routed by the local player controller possessing a pilot seat.

## Reading Instructions

Read before editing:

- `Docs/project_rules.md`
- `Tickets/P0_1_T12B_vehicle_possession_refactor_overview.md`
- `Tickets/P0_1_T12B_1_vehicle_and_pilot_seat_shell.md`
- `Assets/Scripts/Input/ShipInputReader.cs`
- `Assets/Scripts/Input/KeyboardMouseInputProvider.cs`
- `Assets/Scripts/Flight/PlayerShipController.cs`
- `Assets/Scripts/Vehicles/ShipVehicle.cs`
- `Assets/Scripts/Vehicles/ShipPilotSeat.cs`
- `Assets/Scripts/Camera/ShipCameraController.cs`
- `Assets/Scripts/Docking/DockingModeController.cs`
- `Assets/Scripts/Docking/DockingCaptureController.cs`
- `Assets/Scripts/Docking/ShipDockingNode.cs`
- `Assets/Prefabs/Ships/PF_PlayerShip.prefab`
- `Assets/Scenes/FlightTest.unity`

## Implementation

### 1. Add `LocalPlayerVehicleController`

Create `Assets/Scripts/Player/LocalPlayerVehicleController.cs`.

Responsibilities for this ticket:

- serialized `ShipInputReader input`
- serialized `InputBindingsPanel bindingsPanel`
- serialized `ShipPilotSeat startingSeat`
- `ShipPilotSeat CurrentSeat { get; private set; }`
- `ShipVehicle CurrentVehicle => CurrentSeat != null ? CurrentSeat.Vehicle : null`
- subscribe/unsubscribe to `ShipInputReader` event actions
- on `Start`, enter `startingSeat` if assigned

Do not use a singleton.

### 2. Move Input Event Routing

Move these event subscriptions out of `PlayerShipController.WireInputCallbacks()`:

- `ToggleAssistRequested`
- `ToggleCameraRequested`
- `ToggleBindingsPanelRequested`
- `ToggleDockingModeRequested`
- `ToggleDockingPortDeployRequested`
- `UndockRequested`

Route them through `LocalPlayerVehicleController` to the currently possessed `ShipVehicle`.

Add small delegation methods to `ShipVehicle` if useful:

- `CycleAssistMode()`
- `ToggleCamera()`
- `ToggleDockingMode()`
- `ToggleShipDockingPortDeployed()`
- `RequestUndock()`

Those methods should delegate to existing components. Keep them narrow.

### 3. Keep Authority Submission In Place

Do not move `PlayerShipController.FixedUpdate` authority submission in this ticket.

This keeps the refactor checkpoint small: event routing changes, continuous flight command submission does not.

### 4. Wire Scene

Add or wire `LocalPlayerVehicleController` in `FlightTest`.

It must reference:

- existing `ShipInputReader`
- starting `ShipPilotSeat`
- existing `InputBindingsPanel` if one is currently wired

Do not create fake seats or fake ships at runtime.

### 5. Avoid Double Subscription

After moving the event handlers, make sure `PlayerShipController` no longer subscribes the same events. Double subscriptions will show up as camera toggling twice, docking mode immediately exiting, or assist cycling by two modes.

## Guardrails

- Do not move continuous input command submission yet.
- Do not change input bindings.
- Do not touch docking math.
- Do not remove `PlayerShipController` yet.
- Do not change weapon firing flow yet.
- Do not silently create missing authored setup.

## Verification

Build:

```powershell
dotnet build Assembly-CSharp.csproj --no-restore
dotnet build Assembly-CSharp-Editor.csproj --no-restore
```

Playtest:

- `F` cycles assist exactly once per press.
- `V` toggles camera exactly once per press.
- Docking mode toggles exactly once per `H`.
- Docking port deploy toggles exactly once per `J`.
- `U` undocks when docked.
- Bindings panel still toggles.
- Continuous flight controls still work.
- Docking/capture/undock still works.

Search:

```powershell
rg -n "ToggleAssistRequested|ToggleCameraRequested|ToggleDockingModeRequested|ToggleDockingPortDeployRequested|UndockRequested" Assets/Scripts/Flight/PlayerShipController.cs
```

Expected: no event subscription ownership remains in `PlayerShipController`.

## Acceptance Criteria

- `LocalPlayerVehicleController` owns local input event subscriptions.
- `PlayerShipController` no longer owns one-shot local input events.
- Continuous flight behavior remains unchanged.
