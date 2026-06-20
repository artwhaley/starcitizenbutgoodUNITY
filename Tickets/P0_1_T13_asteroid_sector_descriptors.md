# P0.1 T13 - Asteroid Sector Descriptors

## Goal

Add deterministic asteroid sector descriptor generation that is pure, seed-based, and testable.

After this ticket, the game should be able to ask for asteroid descriptors for a region of space without instantiating GameObjects, colliders, renderers, or resources.

## Reading Instructions

Read these files before editing:

- `Tickets/P0_1_T08_reference_frame_ids.md`
- `Tickets/P0_1_T12A_custom_collision_query_and_bounce.md`
- `Assets/Scripts/World/EntityId.cs`
- `Assets/Scripts/World/ReferenceFrameId.cs`
- `Assets/Scripts/Flight/ShipState.cs`
- `Assets/Scripts/Projectiles/ProjectileWorld.cs`
- `Assets/Scripts/MultiplayerFuture/EntitySnapshot.cs`
- `ProjectSettings/TagManager.asset`

Do not start until T08 is complete. T12A is not a hard compile prerequisite, but read it so asteroid collision proxies follow the same future-authoritative collision model.

## Required Files

Create:

- `Assets/Scripts/Asteroids/AsteroidWorldSeed.cs`
- `Assets/Scripts/Asteroids/AsteroidSectorCoord.cs`
- `Assets/Scripts/Asteroids/AsteroidDescriptorId.cs`
- `Assets/Scripts/Asteroids/AsteroidDescriptor.cs`
- `Assets/Scripts/Asteroids/AsteroidDescriptorGenerator.cs`
- `Assets/Scripts/Asteroids/AsteroidGenerationSettings.cs`
- `Assets/Tests/EditMode/AsteroidDescriptorGeneratorTests.cs`

Do not create asteroid prefabs or renderers in this ticket.

## Data Contracts

`AsteroidWorldSeed`:

- serializable value type
- stores `int Value`
- equality
- `ToString()`

`AsteroidSectorCoord`:

- serializable value type
- integer `x`, `y`, `z`
- equality
- `ToString()`
- static method to convert world position to sector coordinate using sector size

`AsteroidDescriptorId`:

- serializable value type
- derived deterministically from world seed, sector coordinate, and local index
- stable across runs
- equality
- `ToString()`

`AsteroidDescriptor`:

- `AsteroidDescriptorId id`
- `AsteroidSectorCoord sector`
- `int localIndex`
- `Vector3 position`
- `Quaternion rotation`
- `float radius`
- `Vector3 nonUniformScale`
- `int visualVariant`
- `int resourceSeed`
- `ReferenceFrameId frameId`

`AsteroidGenerationSettings`:

- `float sectorSizeMeters`, default `1000`
- `int asteroidsPerSector`, default `96`
- `float minRadiusMeters`, default `8`
- `float maxRadiusMeters`, default `80`
- `int visualVariantCount`, default `8`
- deterministic density/noise knobs may be added, but keep defaults simple

## Generator

`AsteroidDescriptorGenerator` must be pure:

- no GameObjects
- no Transforms
- no MonoBehaviour state
- no `UnityEngine.Random`
- no `Time`
- no scene queries

Use a deterministic hash or local PRNG seeded by:

```text
worldSeed + sector x/y/z + local index
```

Given the same seed/settings/sector, it must return the same descriptors in the same order every time.

Descriptors should be distributed within the sector bounds. Positions are in `ReferenceFrameId.LocalZone` for phase 0.1.

## Tests

Add EditMode tests for:

- same seed/sector/settings produces identical descriptors
- different sector produces different IDs
- IDs are unique within a sector
- descriptor positions are inside the sector bounds
- radius and scale are inside configured ranges
- no Unity scene objects are required

## Acceptance Criteria

- Project compiles:

```powershell
dotnet build Assembly-CSharp.csproj --no-restore
```

- Tests compile and pass through the available test path.
- Asteroid descriptor generation is deterministic.
- No GameObjects are instantiated.
- No colliders are created.
- No rendering is added.
- No mining state is added.
- No networking package is added.

## Guardrails

- Do not use `UnityEngine.Random`.
- Do not store descriptor state in scene objects.
- Do not create asteroid GameObjects.
- Do not implement resource depletion.
- Do not add floating-origin systems.
- Do not change ship flight behavior.
