# P0.1 T14 - Instanced Asteroid Scenery

## Goal

Render far asteroid descriptors with GPU instancing and no per-asteroid GameObject spam.

After this ticket, the player should see an expansive asteroid environment generated from deterministic sector descriptors. Far asteroids are visual scenery only: no colliders, no WorldEntity, no mining state.

## Reading Instructions

Read these files before editing:

- `Tickets/P0_1_T13_asteroid_sector_descriptors.md`
- `Assets/Scripts/Asteroids/AsteroidDescriptor.cs`
- `Assets/Scripts/Asteroids/AsteroidDescriptorGenerator.cs`
- `Assets/Scripts/Asteroids/AsteroidGenerationSettings.cs`
- `Assets/Scripts/Flight/ShipFlightController.cs`
- `Assets/Scripts/Flight/ShipState.cs`
- `Assets/Scenes/FlightTest.unity`
- `Packages/manifest.json`

Do not start until T13 is complete.

## Required Files

Create:

- `Assets/Scripts/Asteroids/AsteroidSceneryRenderer.cs`
- `Assets/Scripts/Asteroids/AsteroidSectorTracker.cs`
- `Assets/Scripts/Asteroids/AsteroidVisualLibrary.cs`

Modify:

- `Assets/Scenes/FlightTest.unity`
- project assets only as needed to assign meshes/materials

Do not modify docking scripts.

## Visual Library

Create `AsteroidVisualLibrary` as a ScriptableObject with:

- array of asteroid meshes
- array of asteroid materials
- fallback mesh/material fields if arrays are empty

If no asteroid mesh asset exists, create a simple low-poly placeholder asteroid mesh procedurally at runtime inside the renderer. Do not import a new package.

## Sector Tracking

`AsteroidSectorTracker` should:

- track the player's current `AsteroidSectorCoord`
- expose active scenery sectors within a configurable radius
- default radius: `2` sectors in each direction
- use `AsteroidGenerationSettings.sectorSizeMeters`

For phase 0.1, tracking the local player ship is enough.

## Renderer

`AsteroidSceneryRenderer` should:

- get descriptors from `AsteroidDescriptorGenerator`
- batch by mesh/material/visual variant
- draw using `Graphics.DrawMeshInstanced` or `Graphics.DrawMeshInstancedIndirect`
- use matrices from descriptor position/rotation/scale
- update batches when the active sector set changes
- avoid creating one GameObject per descriptor

Use `DrawMeshInstanced` first unless existing project constraints clearly favor indirect drawing.

Hard limits:

- no more than one manager GameObject for the renderer/tracker
- no per-asteroid GameObjects
- no per-asteroid Colliders
- no per-frame descriptor regeneration for unchanged sectors

## Scene Setup

Add one asteroid scenery manager to `FlightTest.unity`.

Serialized defaults:

- world seed: `12345`
- sector size: `1000m`
- asteroids per sector: `96`
- visible sector radius: `2`
- min radius: `8m`
- max radius: `80m`

Place the initial field so the player sees asteroids after entering play mode.

## Acceptance Criteria

- Project compiles:

```powershell
dotnet build Assembly-CSharp.csproj --no-restore
```

- `FlightTest` shows a deterministic asteroid field.
- Moving across sector boundaries updates visible sectors.
- Far asteroids are rendered with GPU instancing.
- No per-asteroid GameObjects are created for scenery asteroids.
- No colliders are created for scenery asteroids.
- Docking still works if T09-T12 have been completed.
- No mining state is added.
- No networking package is added.

## Guardrails

- Do not use GameObject-per-asteroid for scenery.
- Do not add colliders to scenery asteroids.
- Do not use render mesh colliders.
- Do not make asteroid generation depend on frame rate.
- Do not change ship tuning or controls.
- Do not add resource harvesting.
