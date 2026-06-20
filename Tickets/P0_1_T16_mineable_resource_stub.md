# P0.1 T16 - Mineable Resource Stub

## Goal

Add a minimal authority-owned mineable state model keyed by asteroid descriptor/entity ID and integrate it with the existing weapon hit path.

After this ticket, shooting a promoted asteroid can reduce a simple resource amount. This is a gameplay stub for the friend test, not a full mining loop.

## Reading Instructions

Read these files before editing:

- `Tickets/P0_1_T15_asteroid_promotion_pool.md`
- `Assets/Scripts/Asteroids/AsteroidInstance.cs`
- `Assets/Scripts/Asteroids/AsteroidDescriptorId.cs`
- `Assets/Scripts/Weapons/PrimaryWeaponController.cs`
- `Assets/Scripts/Weapons/SimpleTarget.cs`
- `Assets/Scripts/Gameplay/IHitReceiver.cs` if it exists
- `Assets/Scripts/Projectiles/ProjectileWorld.cs`
- `Assets/Scripts/Authority/LocalGameAuthority.cs`
- `Assets/Scripts/World/EntityId.cs`
- `Assets/Scripts/World/WorldEntity.cs`

Do not start until T15 is complete.

## Required Files

Create:

- `Assets/Scripts/Asteroids/AsteroidResourceState.cs`
- `Assets/Scripts/Asteroids/AsteroidResourceRegistry.cs`
- `Assets/Scripts/Asteroids/MineableAsteroid.cs`
- `Assets/Tests/EditMode/AsteroidResourceRegistryTests.cs`

Modify:

- `Assets/Scripts/Asteroids/AsteroidInstance.cs`
- existing hit receiver path, using the smallest compatible change

## Resource State

`AsteroidResourceState`:

- descriptor ID
- resource seed
- total resource units
- remaining resource units
- depleted flag

Defaults:

- total resource units: deterministic range `25` to `150` from descriptor resource seed
- hit depletion per basic weapon impact: `1` unit unless weapon damage data already exists

`AsteroidResourceRegistry`:

- local-authority-owned registry
- create/get state by descriptor ID
- deterministic initial state from descriptor
- mutate remaining units only through explicit methods

Do not store resource amounts only on the pooled GameObject; pooled objects are transient.

## Hit Integration

`MineableAsteroid` should receive hits through the existing hit receiver pattern.

On hit:

- find descriptor ID from `AsteroidInstance`
- request depletion from `AsteroidResourceRegistry`
- optionally update a simple visual/debug state when depleted

Do not add cargo, inventory, sell prices, station trade UI, or mining lasers.

## Authority Direction

For local single-player, `LocalGameAuthority` or a local registry may own mutation.

Prepare for server authority:

- expose explicit methods like `ApplyMiningHit(descriptorId, amount)`
- do not let UI or VFX directly mutate resource state
- keep state serializable for T17/T20

## Tests

Add tests for:

- same descriptor creates same initial resource amount
- depletion cannot go below zero
- depleted flag becomes true at zero
- state survives pooled object demotion/promotion by descriptor ID

## Acceptance Criteria

- Project compiles:

```powershell
dotnet build Assembly-CSharp.csproj --no-restore
```

- Shooting a promoted asteroid reduces its resource state.
- Resource state is keyed by descriptor ID, not pooled object instance.
- Demoting and re-promoting an asteroid preserves remaining resource state.
- Depleted asteroid reports depleted.
- No inventory/cargo/economy is added.
- No networking package is added.

## Guardrails

- Do not create a full mining profession loop.
- Do not add station trade.
- Do not add persistence to disk.
- Do not store authoritative resource state only on GameObjects.
- Do not change weapon feel except hit target handling.
