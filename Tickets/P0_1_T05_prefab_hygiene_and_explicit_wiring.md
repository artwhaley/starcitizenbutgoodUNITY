# P0.1 T05 - Prefab Hygiene And Explicit Wiring

## Goal

Make the player ship prefab reflect the components it actually needs at runtime.

After this ticket, normal gameplay should not depend on `PlayerShipController` adding core ship components at runtime. Missing wiring should be visible in the prefab, not silently repaired every play session.

Gameplay must feel unchanged.

## Reading Instructions

Read these files before editing:

- `Tickets/P0_1_T01_entity_identity_and_registry.md`
- `Tickets/P0_1_T02_route_flight_through_local_authority.md`
- `Tickets/P0_1_T03_authority_owned_fixed_tick.md`
- `Tickets/P0_1_T04_ship_runtime_role_split.md`
- `Assets/Prefabs/Ships/PF_PlayerShip.prefab`
- `Assets/Scripts/Flight/PlayerShipController.cs`
- `Assets/Scripts/Flight/ShipPresentationController.cs`
- `Assets/Scripts/Authority/LocalGameAuthority.cs`
- `Assets/Scripts/Weapons/PrimaryWeaponController.cs`
- `Assets/Scripts/Weapons/ShipWeaponHardpoints.cs`
- `Assets/Scripts/Projectiles/ProjectileViewPool.cs`
- `Assets/Scripts/Flight/RcsThrusterVfx.cs`
- `Assets/Scripts/Flight/EngineGlowVfx.cs`
- `Assets/Scripts/World/WorldEntity.cs`

Open the prefab YAML carefully before editing. Preserve existing references unless you intentionally replace them.

## Required Design

`PF_PlayerShip` root should explicitly contain or reference:

- `WorldEntity` with kind `Ship`
- `PlayerShipController`
- `ShipInputReader`
- `JoystickInputProvider`
- `LocalGameAuthority`
- `PrimaryWeaponController`
- `ShipPresentationController`
- `ShipCameraController`
- `ShipAudioHooks`
- `ProjectileViewPool`

The visual ship root or visual reference object should explicitly contain:

- `ShipVisualReferences`
- `ShipWeaponHardpoints`

The player ship root should explicitly contain:

- `RcsThrusterVfx`
- `EngineGlowVfx`

Do not create duplicate components.

## Implementation Instructions

### 1. Add Missing Components To Prefab

Use Unity-safe prefab editing if available. If editing YAML manually, be extremely conservative.

Add only components needed by current runtime behavior.

Wire serialized references so `PlayerShipController` and `LocalGameAuthority` do not need to discover/create them at runtime during normal play.

### 2. Reduce Runtime `AddComponent` Repair

In `PlayerShipController.WireVisualSubsystems()`:

- Remove or bypass normal-path `AddComponent<LocalGameAuthority>()`.
- Remove or bypass normal-path `AddComponent<ShipWeaponHardpoints>()`.
- Remove or bypass normal-path creation of `ProjectileViewPool`.
- Remove or bypass normal-path `AddComponent<RcsThrusterVfx>()`.
- Remove or bypass normal-path `AddComponent<EngineGlowVfx>()`.

Acceptable fallback:

- You may leave guarded error logs that explain which component is missing.
- You may leave emergency runtime repair behind a clearly named private method, but it must not be the expected path and should log a warning.

### 3. Remove Fallback Authority Search From Weapons

In `PrimaryWeaponController`, remove repeated per-frame `FindFirstObjectByType<LocalGameAuthority>()`.

Allowed:

- One-time `GetComponentInParent<LocalGameAuthority>()` in `Awake` as a fallback.
- If authority is still missing, log once and do not fire.

Do not search the whole scene every frame.

### 4. Clean Stale Serialized Fields If Safe

The prefab may contain stale serialized field data from older component versions.

If Unity serialization naturally removes stale fields, fine.

If editing YAML manually, do not aggressively delete unknown sections unless you are certain they correspond to removed fields. Functional correctness is more important than perfect YAML cleanup.

## Acceptance Criteria

- Project compiles:

```powershell
dotnet build Assembly-CSharp.csproj --no-restore
```

- Current scene plays with same flight, camera, HUD, VFX, and firing behavior.
- `PF_PlayerShip` has explicit required components.
- `PlayerShipController` no longer depends on normal-path runtime `AddComponent` for authority, hardpoints, projectile pool, RCS VFX, or engine VFX.
- `PrimaryWeaponController` does not call `FindFirstObjectByType` every frame.
- Missing required components produce clear warnings/errors instead of silent repair.
- No networking package is added.
- No docking or asteroid code is added.

## Guardrails

- Do not redesign the prefab hierarchy.
- Do not replace the ship art.
- Do not change tuning values.
- Do not change controls.
- Do not rewrite camera or VFX behavior.
- Do not make broad scene changes.
- Keep emergency fallback code small and noisy if retained.

