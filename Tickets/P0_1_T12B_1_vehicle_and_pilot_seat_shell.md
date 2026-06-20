# P0.1 T12B-1 - Vehicle And Pilot Seat Shell

## Goal

Add the vehicle/seat model as an inert shell without moving live behavior yet.

After this ticket, gameplay should feel unchanged. The only visible difference should be that `PF_PlayerShip` has explicit vehicle and pilot-seat components that show the current local player starts seated.

## Reading Instructions

Read before editing:

- `Docs/project_rules.md`
- `Tickets/P0_1_T12B_vehicle_possession_refactor_overview.md`
- `Assets/Scripts/Flight/PlayerShipController.cs`
- `Assets/Scripts/Flight/ShipFlightController.cs`
- `Assets/Scripts/Flight/ShipPresentationController.cs`
- `Assets/Scripts/Authority/LocalGameAuthority.cs`
- `Assets/Scripts/Docking/DockableShip.cs`
- `Assets/Scripts/Docking/DockingCaptureController.cs`
- `Assets/Scripts/Weapons/PrimaryWeaponController.cs`
- `Assets/Prefabs/Ships/PF_PlayerShip.prefab`
- `Assets/Scenes/FlightTest.unity`

## Implementation

### 1. Add `VehicleOperationalState`

Create `Assets/Scripts/Vehicles/VehicleOperationalState.cs`:

```csharp
namespace FlightModel
{
    public enum VehicleOperationalState
    {
        IdleUnoccupied = 0,
        PilotedLocal = 1,
        PilotedRemote = 2,
        PilotedAi = 3,
        DockedIdle = 4,
        DockedOccupied = 5
    }
}
```

### 2. Add `ShipVehicle`

Create `Assets/Scripts/Vehicles/ShipVehicle.cs`.

Minimum fields/properties:

- serialized references:
  - `WorldEntity worldEntity`
  - `ShipFlightController flight`
  - `LocalGameAuthority authority`
  - `ShipPresentationController presentation`
  - `DockableShip dockableShip`
  - `DockingCaptureController dockingCapture`
  - `PrimaryWeaponController weapon`
  - `ShipWeaponHardpoints hardpoints`
  - `RcsThrusterVfx rcsVfx`
  - `EngineGlowVfx engineGlowVfx`
  - `ShipPilotSeat pilotSeat`
- `VehicleOperationalState OperationalState { get; private set; }`
- `bool HasLocalPilot { get; private set; }`
- public getters for referenced systems needed by later tickets
- `SetOperationalState(VehicleOperationalState state)`
- `SetLocalPilotOccupancy(bool occupied)`

For this ticket, `SetOperationalState` should mostly record state. Do not start disabling systems yet.

Reference discovery:

- Prefer serialized references from prefab.
- It is acceptable to fill missing component references in `Awake` by searching the same object/children recursively.
- Do not create missing gameplay components.
- If a critical reference is missing, log a clear warning/error.

### 3. Add `ShipPilotSeat`

Create `Assets/Scripts/Vehicles/ShipPilotSeat.cs`.

Minimum fields/properties:

- serialized `ShipVehicle vehicle`
- `bool IsOccupied { get; private set; }`
- `bool IsOccupiedByLocalPlayer { get; private set; }`
- `bool CanAcceptLocalPilot => !IsOccupied || IsOccupiedByLocalPlayer`
- `ShipVehicle Vehicle => vehicle`
- `bool TryEnterLocalPilot()`
- `void ExitLocalPilot()`

Behavior:

- `TryEnterLocalPilot` marks the seat locally occupied and calls `vehicle.SetLocalPilotOccupancy(true)`.
- `ExitLocalPilot` clears local occupancy and calls `vehicle.SetLocalPilotOccupancy(false)`.
- If `vehicle` is missing, search parent hierarchy once. Do not create a vehicle.

### 4. Wire Prefab Deliberately

Update `PF_PlayerShip`:

- Add `ShipVehicle` on the ship root.
- Add `ShipPilotSeat` on the ship root or an authored cockpit/seat transform.
- Wire `ShipPilotSeat.vehicle`.
- Wire `ShipVehicle.pilotSeat`.
- Wire obvious existing ship system references.

Do not create or move authored docking nodes, station ports, hardpoints, or COG/root transforms.

### 5. Temporary Start Occupancy

For this shell ticket, it is acceptable for `ShipPilotSeat` to have a serialized `startsOccupiedByLocalPlayer = true` used only to set initial occupancy in `Start`.

This is temporary. Later sub-tickets will move starting possession into `LocalPlayerVehicleController`.

## Guardrails

- Do not move input handling.
- Do not move authority submission.
- Do not edit docking math.
- Do not remove `PlayerShipController`.
- Do not change controls.
- Do not change tuning.
- Do not create runtime fake setup.

## Verification

Build:

```powershell
dotnet build Assembly-CSharp.csproj --no-restore
dotnet build Assembly-CSharp-Editor.csproj --no-restore
```

Playtest:

- Current fly/fire/camera/dock/undock loop works unchanged.
- In Play mode, `ShipVehicle.OperationalState` is `PilotedLocal`.
- `ShipPilotSeat.IsOccupiedByLocalPlayer` is true.
- No new fake gameplay objects appear.

## Acceptance Criteria

- New vehicle and seat classes exist.
- `PF_PlayerShip` is explicitly wired with `ShipVehicle` and `ShipPilotSeat`.
- Existing gameplay is unchanged.
- No responsibilities have been moved yet.
