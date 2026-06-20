# P0.1 T06 - Ship Collision Proxy And Physics Layers

## Goal

Add a simple explicit ship collision/trigger proxy and define physics layers needed for docking, asteroid activation, and projectile filtering.

After this ticket, the player ship should be detectable by trigger volumes and future collision queries without relying on visual mesh colliders.

Gameplay should feel unchanged except that trigger-based systems can now detect the ship.

## Reading Instructions

Read these files before editing:

- `Tickets/P0_1_T01_entity_identity_and_registry.md`
- `Tickets/P0_1_T05_prefab_hygiene_and_explicit_wiring.md`
- `Assets/Prefabs/Ships/PF_PlayerShip.prefab`
- `Assets/Prefabs/Arena/PF_TestArena.prefab`
- `Assets/Scripts/Gameplay/DockingZone.cs`
- `Assets/Scripts/Flight/PlayerShipController.cs`
- `Assets/Scripts/Flight/ShipHierarchyUtility.cs`
- `Assets/Scripts/Projectiles/ProjectileWorld.cs`
- `Assets/Scripts/Weapons/WeaponDefinition.cs`
- `ProjectSettings/TagManager.asset`
- `ProjectSettings/DynamicsManager.asset`

Do not start until T05 prefab wiring is complete.

## Required Layers

Add these Unity layers in `ProjectSettings/TagManager.asset`:

- `ShipHull`
- `Station`
- `DockingTrigger`
- `ProjectileHit`
- `MineableAsteroid`
- `SceneryAsteroid`

Use the first available user layer slots. Do not rename existing layers.

## Required Component

Create:

- `Assets/Scripts/Physics/ShipCollisionProxy.cs`

Behavior:

- MonoBehaviour placed on the ship COG or a child under COG.
- Requires/creates no visual mesh.
- Owns a simple collider reference.
- Exposes `WorldEntity` or `ShipFlightController` reference if useful.
- Provides a clear place for future collision/sweep logic.

For this ticket, the proxy may be a marker component with serialized references. The actual collider and Rigidbody are the important part.

## Ship Proxy Setup

On `PF_PlayerShip`:

- Add a child under `COG` named `ShipCollisionProxy`.
- Set its layer to `ShipHull`.
- Add a simple `CapsuleCollider` or `BoxCollider`.
- Add a kinematic `Rigidbody`.
- Set `Rigidbody.useGravity = false`.
- Set `Rigidbody.isKinematic = true`.
- Add `ShipCollisionProxy`.

Collider sizing:

- Use a conservative collider that roughly covers the ship body.
- Do not attempt perfect mesh collision.
- Do not use MeshCollider for the player ship.

## Docking Trigger Setup

On the current `DockingZone` object in `PF_TestArena`:

- Set layer to `DockingTrigger`.
- Keep its collider as trigger.

Update `DockingZone` if needed so it can find `ShipFlightController` from the ship proxy's parent hierarchy.

Do not implement actual docking in this ticket.

## Projectile Layer Rules

For now:

- Keep existing weapon behavior working.
- Do not fully redesign projectile masks.
- Ensure projectiles can still hit the existing test target.

If you update default `WeaponDefinition.hitMask`, do so carefully and document it in the ticket completion notes.

## Acceptance Criteria

- Project compiles:

```powershell
dotnet build Assembly-CSharp.csproj --no-restore
```

- Current single-player flight and firing behavior is preserved.
- Player ship has an explicit simple collider proxy on/under COG.
- Player ship proxy has a kinematic Rigidbody with gravity disabled.
- `DockingZone.OnTriggerStay` can detect the player ship when entering the trigger.
- Visual mesh colliders may still be disabled; the proxy remains active.
- Required layers exist in `TagManager.asset`.
- No docking state machine is added.
- No asteroid generation is added.
- No networking package is added.

## Guardrails

- Do not switch flight to Rigidbody physics.
- Do not add environmental collision response yet.
- Do not make the ship bounce/crash.
- Do not use complex mesh colliders for the ship.
- Do not change flight tuning.
- Do not implement mining.
- Do not implement docking beyond trigger detection.

