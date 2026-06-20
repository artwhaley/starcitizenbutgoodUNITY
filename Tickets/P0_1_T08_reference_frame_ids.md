# P0.1 T08 - Reference Frame IDs

## Goal

Replace ad hoc string frame IDs with a typed reference-frame identity model.

After this ticket, ships and other future entities should be able to state which frame they are in using a small structured ID, even though only the local zone frame is active for gameplay.

Gameplay must feel unchanged.

## Reading Instructions

Read these files before editing:

- `Tickets/P0_1_T01_entity_identity_and_registry.md`
- `Tickets/P0_1_T07_simulation_extraction_and_tests.md`
- `Assets/Scripts/Flight/ShipState.cs`
- `Assets/Scripts/Flight/ShipFlightController.cs`
- `Assets/Scripts/Flight/ShipSimulator.cs`
- `Assets/Scripts/MultiplayerFuture/EntitySnapshot.cs`
- `Assets/Scripts/World/EntityId.cs`
- `Assets/Scripts/World/EntityKind.cs`
- `Assets/Scripts/World/WorldEntity.cs`

Do not start until T01 exists. Prefer completing T07 first so frame changes touch the extracted simulator once.

## Required Files

Create:

- `Assets/Scripts/World/ReferenceFrameKind.cs`
- `Assets/Scripts/World/ReferenceFrameId.cs`
- `Assets/Scripts/World/ReferenceFrame.cs`
- `Assets/Scripts/World/ReferenceFrameRegistry.cs`

Modify:

- `Assets/Scripts/Flight/ShipState.cs`
- `Assets/Scripts/Flight/ShipFlightController.cs`
- `Assets/Scripts/Flight/ShipSimulator.cs` if created
- `Assets/Scripts/MultiplayerFuture/EntitySnapshot.cs`

## Required Types

### `ReferenceFrameKind`

```csharp
namespace FlightModel.World
{
    public enum ReferenceFrameKind
    {
        Unknown = 0,
        Zone = 1,
        Station = 2,
        ShipInterior = 3,
        Docking = 4,
        Eva = 5
    }
}
```

### `ReferenceFrameId`

Create a serializable value type:

- `int Value`
- `bool IsValid`
- equality operators
- `ToString()`
- static `Invalid`
- static `LocalZone`

Use:

```csharp
public static readonly ReferenceFrameId LocalZone = new(1);
```

Keep the backing value as `int`.

### `ReferenceFrame`

MonoBehaviour for frame roots:

- serialized `ReferenceFrameKind kind`
- serialized `int serializedFrameId`
- public `ReferenceFrameId Id`
- public `ReferenceFrameKind Kind`
- public `Transform Root`
- method `Assign(ReferenceFrameId id, ReferenceFrameKind kind)`

### `ReferenceFrameRegistry`

Local registry similar to `LocalEntityRegistry`:

- allocate positive frame IDs, starting after `LocalZone`
- register/unregister `ReferenceFrame`
- find by ID
- ensure a local zone frame exists

Keep it simple. Do not add coordinate origin shifting yet.

## Implementation Instructions

### 1. Replace `ShipState.frameId`

Current:

```csharp
public string frameId;
```

Replace with:

```csharp
public ReferenceFrameId frameId;
```

or use `referenceFrameId` if you prefer clearer naming. Update all call sites.

Default ship initialization should set:

```csharp
frameId = ReferenceFrameId.LocalZone;
```

### 2. Update `EntitySnapshot`

Add frame ID to snapshots:

```csharp
public ReferenceFrameId frameId;
```

If this requires a `using FlightModel.World;`, add it.

Do not implement networking serialization yet.

### 3. Add Local Zone Frame

Ensure there is a local zone frame available at runtime.

Acceptable:

- `ReferenceFrameRegistry` creates a `LocalZoneFrame` GameObject if none exists.

Preferred if simple:

- Add a scene/root object later, but do not require scene editing in this ticket.

### 4. Keep Positions Semantics Unchanged

For now, `ShipState.position` remains local to `ReferenceFrameId.LocalZone`.

Do not implement transform conversion between frames yet.

Do not implement docking frame switching yet.

This ticket only replaces the ID model and prepares the registry.

## Acceptance Criteria

- Project compiles:

```powershell
dotnet build Assembly-CSharp.csproj --no-restore
```

- Existing flight scene still plays.
- `ShipState` no longer uses a string for frame ID.
- New ships initialize into `ReferenceFrameId.LocalZone`.
- `EntitySnapshot` includes typed frame ID.
- `ReferenceFrameRegistry` can register and look up frames.
- No docking behavior is added.
- No coordinate conversion system is added.
- No networking package is added.
- No asteroid code is added.

## Guardrails

- Keep this as identity infrastructure only.
- Do not add floating-origin systems.
- Do not parent ships under frames yet.
- Do not implement station interiors.
- Do not implement docking state.
- Do not rename unrelated fields.
- Do not change flight tuning or movement behavior.

