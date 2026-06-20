# P0.1 T03 - Authority-Owned Fixed Tick

## Goal

Introduce an explicit, authority-owned simulation tick with stable tick numbers and fixed simulation delta.

After this ticket, local authoritative simulation should advance through numbered ticks, and input/weapon requests should carry the current tick where practical.

Gameplay must feel unchanged.

## Reading Instructions

Read these files before editing:

- `Tickets/P0_1_T01_entity_identity_and_registry.md`
- `Tickets/P0_1_T02_route_flight_through_local_authority.md`
- `Assets/Scripts/Authority/IGameAuthority.cs`
- `Assets/Scripts/Authority/LocalGameAuthority.cs`
- `Assets/Scripts/Authority/ClientInputCommand.cs`
- `Assets/Scripts/Authority/WeaponFireRequest.cs`
- `Assets/Scripts/Flight/PlayerShipController.cs`
- `Assets/Scripts/Weapons/PrimaryWeaponController.cs`
- `Assets/Scripts/Projectiles/ProjectileWorld.cs`

If flight is not routed through `LocalGameAuthority.SubmitInput()` yet, stop and complete T02 first.

## Required Design

Use a single local simulation rate:

```csharp
public const float SimulationDeltaSeconds = 1f / 60f;
```

For this ticket, use 60 Hz. Do not make it configurable yet.

`LocalGameAuthority` owns:

- `uint ServerTick`
- an accumulator
- advancing simulation in exact `SimulationDeltaSeconds` steps

Unity `FixedUpdate` or `Update` may pass elapsed time to the authority, but authority decides how many simulation ticks to run.

## Implementation Instructions

### 1. Extend `IGameAuthority` Carefully

Keep existing methods:

```csharp
void SubmitInput(in ClientInputCommand command);
void SubmitWeaponFire(in WeaponFireRequest request);
void Tick(float deltaTime);
```

Do not rename `Tick` yet.

Add to `LocalGameAuthority`:

- `public const float SimulationDeltaSeconds = 1f / 60f;`
- `public uint ServerTick { get; private set; }`
- private accumulator, e.g. `float tickAccumulator`

### 2. Update `LocalGameAuthority.Tick(float deltaTime)`

`Tick(deltaTime)` should:

- Accumulate elapsed time.
- While accumulator has at least `SimulationDeltaSeconds`, run exactly one simulation step.
- Increment `ServerTick` once per simulation step.
- Simulate controlled ship using `SimulationDeltaSeconds`, not raw `deltaTime`.
- Tick projectiles using `SimulationDeltaSeconds`, not raw `deltaTime`.
- Sync projectile views after simulation ticks have run.

If multiple ticks run in one Unity frame, avoid syncing projectile views inside every inner tick. Sync once after the loop.

### 3. Update Input Tick Assignment

Where `PlayerShipController` builds `ClientInputCommand`:

- Set `inputTick` to `authority.ServerTick`.

This is still local-only. No input buffering yet.

### 4. Update Weapon Fire Tick Assignment

Where `PrimaryWeaponController` submits `WeaponFireRequest`:

- Set `inputTick` to the current authority tick if authority exists.
- Keep fallback `0` only if authority is missing.

### 5. Remove Duplicate Tick Fields

If `LocalGameAuthority` has an old private `serverTick`, replace it with `ServerTick`.

Do not change projectile IDs in this ticket.

## Acceptance Criteria

- Project compiles:

```powershell
dotnet build Assembly-CSharp.csproj --no-restore
```

- Current single-player flight feel remains the same or very close.
- `LocalGameAuthority.ServerTick` increments once per authoritative simulation step.
- Ship simulation uses `SimulationDeltaSeconds`.
- Projectile simulation uses `SimulationDeltaSeconds`.
- `ClientInputCommand.inputTick` is populated from authority tick.
- `WeaponFireRequest.inputTick` is populated from authority tick where available.
- No networking package is added.
- No prediction/reconciliation is added.

## Guardrails

- Do not make a complex scheduler.
- Do not add rollback.
- Do not add interpolation.
- Do not change gameplay tuning to compensate for the tick unless absolutely required.
- Do not move simulation out of `ShipFlightController` yet; that is T07.
- Do not add docking, asteroid, or persistence code.

