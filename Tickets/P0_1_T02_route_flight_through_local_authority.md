# P0.1 T02 - Route Flight Through Local Authority

## Goal

Make local single-player flight use the same command submission boundary that multiplayer will use later.

After this ticket, `PlayerShipController` should no longer be the component that directly decides authoritative ship simulation. It may still gather local input and update presentation, but the ship movement step must be triggered through `LocalGameAuthority`.

Gameplay must feel unchanged.

## Reading Instructions

Read these files before editing:

- `Tickets/P0_1_T01_entity_identity_and_registry.md`
- `Assets/Scripts/Flight/PlayerShipController.cs`
- `Assets/Scripts/Flight/ShipFlightController.cs`
- `Assets/Scripts/Flight/ShipInputCommand.cs`
- `Assets/Scripts/Authority/IGameAuthority.cs`
- `Assets/Scripts/Authority/LocalGameAuthority.cs`
- `Assets/Scripts/Authority/ClientInputCommand.cs`
- `Assets/Scripts/Weapons/PrimaryWeaponController.cs`
- `Assets/Scripts/Projectiles/ProjectileWorld.cs`
- `Assets/Scripts/World/WorldEntity.cs`
- `Assets/Scripts/World/LocalEntityRegistry.cs`

If the `Assets/Scripts/World/` files do not exist yet, stop and complete T01 first.

## Current Problem

`PlayerShipController.FixedUpdate()` currently does this:

```csharp
ShipInputCommand command = ResolveFlightCommand();
flight.Simulate(Time.fixedDeltaTime, command);
ShipState state = flight.State;
cogTransform.SetPositionAndRotation(state.position, state.rotation);
authority?.Tick(Time.fixedDeltaTime);
```

That means flight bypasses `IGameAuthority.SubmitInput()`. Weapons are partially authority-routed, but movement is not.

## Required Design

Use this local flow:

```text
ShipInputReader / PlayerShipController
  -> ClientInputCommand
  -> LocalGameAuthority.SubmitInput(command)
  -> LocalGameAuthority.Tick(delta)
  -> ShipFlightController.Simulate(delta, input)
  -> PlayerShipController / presentation applies resulting state to transform
```

This ticket should support one local controlled ship only. Do not build multi-player input queues yet.

## Implementation Instructions

### 1. Extend `LocalGameAuthority`

Add serialized or runtime-wired references:

- controlled ship `WorldEntity`
- controlled `ShipFlightController`
- controlled ship root/COG transform if needed

Add fields:

- latest submitted `ClientInputCommand`
- bool indicating whether an input command has been submitted
- public property exposing the most recently simulated `ShipState` if useful

Implement `SubmitInput(in ClientInputCommand command)`:

- Store the latest command.
- Ignore commands for the wrong entity if a controlled ship entity is available.
- Do not simulate immediately inside `SubmitInput`.

Update `Tick(float deltaTime)`:

- Simulate the controlled ship before projectiles.
- Use the latest submitted `ShipInputCommand`.
- If no input has been submitted yet, use `default`.
- Preserve existing projectile behavior.

Do not add prediction, rollback, buffering, packet handling, or networking.

### 2. Update `PlayerShipController`

`PlayerShipController.FixedUpdate()` should:

- Resolve the local command.
- Build a `ClientInputCommand`.
- Set `clientId = 1` for now.
- Set `controlledEntityId` from the local ship `WorldEntity` when available.
- Set `inputTick = 0` for now. T03 will introduce real ticks.
- Call `authority.SubmitInput(...)`.
- Call `authority.Tick(Time.fixedDeltaTime)` for now.
- Apply the resulting `flight.State` to `cogTransform`.

Important: T03 will move tick ownership further. For this ticket, it is acceptable for `PlayerShipController` to call `authority.Tick(...)` so long as the flight sim itself goes through `SubmitInput`.

### 3. Preserve Presentation Behavior

Do not change:

- camera behavior
- HUD behavior
- audio behavior
- weapon firing behavior
- tuning behavior
- input binding behavior

`Update()` may continue to call `weapon.Tick(command.firePrimary)` for now.

### 4. Avoid Double Simulation

There must be exactly one ship simulation step per fixed tick.

After the change, search for direct calls to:

```csharp
flight.Simulate(
```

Only authority-owned code should call it at runtime.

Editor tools or future tests may call simulation directly, but `PlayerShipController` should not.

## Acceptance Criteria

- Project compiles:

```powershell
dotnet build Assembly-CSharp.csproj --no-restore
```

- Current single-player flight feel is unchanged.
- Current firing behavior is unchanged.
- `LocalGameAuthority.SubmitInput()` is no longer empty.
- Runtime flight simulation is triggered from `LocalGameAuthority.Tick()`.
- `PlayerShipController.FixedUpdate()` submits `ClientInputCommand` instead of directly calling `flight.Simulate(...)`.
- No networking package is added.
- No new multiplayer UI is added.
- No docking or asteroid code is added.

## Guardrails

- Keep this as a routing change only.
- Do not introduce queues or reconciliation.
- Do not create a new network transport abstraction.
- Do not rewrite `ShipFlightController` yet.
- Do not change physics/collision behavior.
- Do not remove local play mode.
- Do not break `PrimaryWeaponController`.

