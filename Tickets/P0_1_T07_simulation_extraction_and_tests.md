# P0.1 T07 - Simulation Extraction And Tests

## Goal

Extract the ship simulation step toward a plain C# shape and add focused tests for core simulation behavior.

After this ticket, `ShipFlightController` may remain a MonoBehaviour, but the math that advances `ShipState` should live in a non-MonoBehaviour class that can be tested without a scene.

Gameplay must feel unchanged.

## Reading Instructions

Read these files before editing:

- `Tickets/P0_1_T01_entity_identity_and_registry.md`
- `Tickets/P0_1_T02_route_flight_through_local_authority.md`
- `Tickets/P0_1_T03_authority_owned_fixed_tick.md`
- `Assets/Scripts/Flight/ShipFlightController.cs`
- `Assets/Scripts/Flight/ShipState.cs`
- `Assets/Scripts/Flight/ShipInputCommand.cs`
- `Assets/Scripts/Flight/ShipControlRequest.cs`
- `Assets/Scripts/Flight/ShipControlRequestPipeline.cs`
- `Assets/Scripts/Flight/ShipPropellantAccounting.cs`
- `Assets/Scripts/Flight/ShipTuning.cs`
- `Assets/Scripts/Projectiles/ProjectileWorld.cs`
- `Packages/manifest.json`

Do not start until T02 and T03 are complete.

## Required Files

Create:

- `Assets/Scripts/Flight/ShipSimulator.cs`
- `Assets/Tests/EditMode/ShipSimulatorTests.cs`
- `Assets/Tests/EditMode/ProjectileWorldTests.cs`

If the project has no test folders, create them.

Do not add asmdefs in this ticket unless Unity test discovery requires it. If asmdefs are needed, keep them minimal and document why.

## Required Design

`ShipSimulator` should be a plain C# class or static class.

It should expose a method similar to:

```csharp
public static void Step(
    ref ShipState state,
    ShipTuning tuning,
    IReadOnlyList<IShipControlRequestSource> externalRequestSources,
    float deltaSeconds,
    in ShipInputCommand input,
    out ShipSimulationTelemetry telemetry)
```

The exact signature may differ, but:

- state must be passed in/out explicitly
- no Transform references
- no GameObject references
- no `Time.deltaTime`
- no camera/UI/VFX/audio references
- no authority/network references

If creating `ShipSimulationTelemetry` is too large, keep existing last-output fields in `ShipFlightController`, but do not leave important applied-output math trapped inside the MonoBehaviour.

## Implementation Instructions

### 1. Move Step Math Out Of `ShipFlightController`

Move the math currently inside `ShipFlightController.Simulate(...)` into `ShipSimulator`.

`ShipFlightController.Simulate(...)` should become a thin wrapper:

- validate tuning/delta
- call `ShipSimulator.Step(...)`
- store last telemetry fields needed by HUD/VFX

Preserve existing public properties:

- `State`
- `LastAppliedThrusterCommand`
- `LastThrusterOutput`
- `LastPilotRequest`
- `LastAssistRequest`
- `LastBrakeRequest`
- `LastMergedControlRequest`
- `Tuning`

### 2. Fix Current Mass Reset

Remove this incorrect behavior:

```csharp
state.currentMassKg = tuning.dryMassKg;
```

Runtime mass should at least be:

```text
dry mass + remaining fuel + remaining hypergolic
```

Keep it simple:

- after burn, set `currentMassKg = tuning.dryMassKg + state.remainingFuelKg + state.remainingHypergolicKg`
- do not add cargo mass yet
- do not add damage mass yet

Update tests to cover this.

### 3. Add EditMode Tests

Add tests for:

- forward thrust increases forward speed from rest
- no input preserves velocity in assist off
- brake requests counter existing velocity
- fuel decreases under main forward thrust
- hypergolic decreases under strafe or angular thrust
- current mass decreases as propellant burns
- projectile world advances projectile position
- projectile despawns after lifetime or range

Tests should be small and deterministic.

Use programmatically created `ShipTuning` instances in tests.

### 4. Do Not Change Feel

This is an extraction and correctness pass, not a retuning pass.

If flight feel changes due to mass including propellant, that is acceptable only if small and physically expected. Do not compensate with tuning changes in this ticket.

## Acceptance Criteria

- Project compiles:

```powershell
dotnet build Assembly-CSharp.csproj --no-restore
```

- Unity EditMode tests compile.
- New tests pass when run through Unity Test Runner or available CLI test path.
- Existing single-player scene still flies and fires.
- `ShipFlightController.Simulate(...)` is a wrapper around `ShipSimulator`.
- Simulation step math is testable without a scene.
- `currentMassKg` is not reset to dry mass every tick.
- No networking package is added.
- No docking or asteroid code is added.

## Guardrails

- Do not rewrite the flight model.
- Do not change tuning asset values.
- Do not add cargo/inventory.
- Do not add persistence.
- Do not introduce reflection/source generation.
- Do not convert the whole project to asmdefs unless required for tests.
- Keep test scope focused on simulation and projectiles.

