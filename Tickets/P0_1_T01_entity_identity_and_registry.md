# P0.1 T01 - Entity Identity And Local Registry

## Goal

Introduce stable local entity identity for ships, stations, asteroids, and projectiles without changing current gameplay.

This is the first Phase 0.1 foundation task. It should not add networking, docking, asteroid generation, prediction, persistence, or a new authority architecture yet. It only gives the project a small, explicit entity model so later authority/docking/asteroid work can stop using hard-coded IDs.

## Why This Comes First

Current code assumes one player and one ship:

- `LocalGameAuthority` hard-codes `ClientId = 1` and `PlayerEntityId = 1`.
- `WeaponFireRequest` and `ClientInputCommand` already carry entity IDs, but those IDs are not allocated or registered anywhere.
- `EntitySnapshot` exists in `MultiplayerFuture/`, but nothing maps a GameObject or simulation state to an entity ID.
- Future docking needs to say "ship entity X is docked at station entity Y / port Z."
- Future asteroid activation needs stable IDs for generated asteroids so mined/depleted state can be tracked.

This ticket creates the smallest useful identity layer.

## Primary Files

Create:

- `Assets/Scripts/World/EntityId.cs`
- `Assets/Scripts/World/EntityKind.cs`
- `Assets/Scripts/World/WorldEntity.cs`
- `Assets/Scripts/World/LocalEntityRegistry.cs`

Modify:

- `Assets/Scripts/Authority/LocalGameAuthority.cs`
- `Assets/Scripts/Weapons/PrimaryWeaponController.cs`
- `Assets/Scripts/Projectiles/ProjectileState.cs` only if needed for naming consistency
- `Assets/Prefabs/Ships/PF_PlayerShip.prefab` only if you are comfortable safely adding `WorldEntity`; otherwise add it at runtime as a temporary bridge and note the follow-up

## Required Types

### `EntityId`

Create a small serializable value type:

```csharp
namespace FlightModel.World
{
    [System.Serializable]
    public readonly struct EntityId : System.IEquatable<EntityId>
    {
        public readonly int Value;
        public bool IsValid => Value > 0;

        public EntityId(int value) => Value = value;

        public bool Equals(EntityId other) => Value == other.Value;
        public override bool Equals(object obj) => obj is EntityId other && Equals(other);
        public override int GetHashCode() => Value;
        public override string ToString() => IsValid ? Value.ToString() : "Invalid";

        public static bool operator ==(EntityId left, EntityId right) => left.Equals(right);
        public static bool operator !=(EntityId left, EntityId right) => !left.Equals(right);

        public static readonly EntityId Invalid = new(0);
    }
}
```

Keep the backing value as `int` for now because existing command structs use `int`. Do not convert every existing struct to `EntityId` in this ticket unless it stays small and safe.

### `EntityKind`

```csharp
namespace FlightModel.World
{
    public enum EntityKind
    {
        Unknown = 0,
        Ship = 1,
        Station = 2,
        Asteroid = 3,
        Projectile = 4,
        Character = 5
    }
}
```

### `WorldEntity`

Create a MonoBehaviour placed on entity root GameObjects:

- Serialized `EntityKind kind`
- Serialized `int serializedEntityId`
- Public `EntityId Id`
- Public `EntityKind Kind`
- Public `Transform Root`
- Method `Assign(EntityId id, EntityKind kind)` for registry/runtime assignment

Rules:

- If `serializedEntityId > 0`, preserve it.
- If not assigned, the registry can allocate one at runtime.
- Do not use GUID strings for this ticket.
- Do not add persistence yet.

### `LocalEntityRegistry`

Create a simple MonoBehaviour registry:

- Singleton-like convenience is acceptable for this local prototype, but avoid global static gameplay state where possible.
- It should allocate monotonically increasing positive IDs.
- It should register/unregister `WorldEntity` instances.
- It should support:
  - `WorldEntity Register(WorldEntity entity, EntityKind fallbackKind)`
  - `bool TryGet(EntityId id, out WorldEntity entity)`
  - `EntityId AllocateId()`
  - `IReadOnlyCollection<WorldEntity> Entities`

Runtime behavior:

- On `Awake`, find existing `WorldEntity` components in the active scene and register them.
- If multiple entities have the same non-zero ID, log an error and allocate a new ID for the later duplicate.
- If no registry exists, `LocalGameAuthority` may create one as a temporary bridge, but prefer a scene/root object long-term.

## Integration Requirements

### Player ship

Ensure the player ship root has a `WorldEntity` of kind `Ship`.

Preferred:

- Add `WorldEntity` to `PF_PlayerShip` root in the prefab.
- Give it `kind = Ship`.
- It may leave `serializedEntityId = 0` so the registry allocates a local runtime ID.

Acceptable temporary bridge:

- In `LocalGameAuthority.Awake`, if `playerShipRoot` or its parent lacks `WorldEntity`, add one and register it.
- Log a clear TODO-style warning that the prefab should be explicitly wired in a follow-up.

### `LocalGameAuthority`

Remove hard-coded entity identity as the source of truth.

Current hard-coded constants:

```csharp
const int ClientId = 1;
const int PlayerEntityId = 1;
```

Expected new shape:

- Keep `ClientId = 1` temporarily if needed.
- Replace `PlayerEntityId` usage with the registered ship entity ID.
- Add serialized/reference field for `LocalEntityRegistry` or resolve it in `Awake`.
- Add field/property for the controlled ship `WorldEntity`.
- When spawning projectiles, pass `controlledShipEntity.Id.Value` as `ownerEntityId`.

Do not rewrite the whole authority system in this ticket.

### `PrimaryWeaponController`

When submitting `WeaponFireRequest`, populate `shooterEntityId` from the owning `WorldEntity` if available.

Acceptable fallback:

- If no `WorldEntity` is found, continue using `1` and log a warning once.

Do not add networking.
Do not add client prediction.
Do not change firing behavior.

## Acceptance Criteria

- Project compiles with:

```powershell
dotnet build Assembly-CSharp.csproj --no-restore
```

- Current single-player flight and firing behavior is preserved.
- Player ship has a registered entity ID at runtime.
- Projectiles spawned by `LocalGameAuthority` use the player ship's registered entity ID as `ownerEntityId`.
- `WeaponFireRequest.shooterEntityId` is no longer blindly hard-coded to `1` when a `WorldEntity` is available.
- Existing public command structs can still use `int` IDs for now.
- No networking package is added.
- No docking, asteroid, station interior, persistence, or prediction code is added.

## Guardrails

- Keep this small. This is identity plumbing, not the full world model.
- Do not create a complex ECS.
- Do not create a database/persistence layer.
- Do not move all gameplay code into `World/`.
- Do not introduce reflection, source generators, or serialization frameworks.
- Avoid changing prefab hierarchy except to add `WorldEntity` if safe.
- Preserve existing namespaces where practical, but put new identity types under `FlightModel.World`.

## Follow-Up Tickets This Enables

After this lands, the next foundation tickets should be:

1. Route local flight through `LocalGameAuthority.SubmitInput`.
2. Add an authority-owned fixed simulation tick.
3. Add ship collision proxy and physics layers.
4. Add `ReferenceFrameId` and dock socket state.
5. Add deterministic asteroid descriptor IDs.

