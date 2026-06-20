# P0.1 T12A - Custom Collision Query And Bounce Response

## Goal

Add real ship environmental collision response without giving ship movement ownership to Unity Rigidbody physics.

After this ticket, the player ship should bounce off configured station/arena/static obstacle colliders using the custom flight simulation state. Unity physics may be used only as the local collision query backend. The authoritative motion result must remain in `ShipState`.

Ship and world collision geometry must be **authored in the editor** as explicit `BoxCollider`, `CapsuleCollider`, and convex `MeshCollider` primitives placed in the object hierarchy. Multiple primitives on one body compose a compound collision volume (for example three convex hulls approximating a concave asteroid). Runtime code must not auto-generate gameplay collision from render meshes.

This ticket exists before asteroid/networking work so the 0.1 friend test has a known path for ship-vs-world collision that can later move to a pure C# server implementation.

## Reading Instructions

Read these files before editing:

- `Tickets/P0_1_T06_ship_collision_proxy_and_physics_layers.md`
- `Tickets/P0_1_T07_simulation_extraction_and_tests.md`
- `Docs/project_rules.md`
- `Assets/Scripts/Physics/ShipCollisionProxy.cs`
- `Assets/Scripts/Flight/ShipSimulator.cs`
- `Assets/Scripts/Flight/ShipFlightController.cs`
- `Assets/Scripts/Flight/ShipState.cs`
- `Assets/Scripts/Flight/PlayerShipController.cs`
- `Assets/Scripts/World/ReferenceFrameId.cs`
- `Assets/Prefabs/Ships/PF_PlayerShip.prefab`
- `Assets/Prefabs/Arena/PF_TestArena.prefab`
- `Assets/Scenes/FlightTest.unity`
- `ProjectSettings/TagManager.asset`
- `ProjectSettings/DynamicsManager.asset`

Do not start until T08 is complete. This ticket may be executed while docking tickets are in progress only if it does not edit docking files.

## Required Files

Create:

- `Assets/Scripts/Physics/ICollisionWorld.cs`
- `Assets/Scripts/Physics/ShipCollisionPrimitiveKind.cs`
- `Assets/Scripts/Physics/ShipCollisionShape.cs`
- `Assets/Scripts/Physics/ShipCollisionShapeSet.cs`
- `Assets/Scripts/Physics/ShipCollisionHit.cs`
- `Assets/Scripts/Physics/ShipCollisionMask.cs`
- `Assets/Scripts/Physics/ShipCollisionResolver.cs`
- `Assets/Scripts/Physics/ShipCollisionShapeBaker.cs`
- `Assets/Scripts/Physics/UnityCollisionWorld.cs`
- `Assets/Tests/EditMode/ShipCollisionResolverTests.cs`

Modify:

- `Assets/Scripts/Physics/ShipCollisionProxy.cs`
- `Assets/Scripts/Flight/ShipFlightController.cs`
- `Assets/Scripts/Flight/ShipSimulationTelemetry.cs`
- `Assets/Prefabs/Ships/PF_PlayerShip.prefab`
- `Assets/Prefabs/Arena/PF_TestArena.prefab`
- `Assets/Scenes/FlightTest.unity` only if needed to put arena/station obstacles on correct layers

Do not edit docking scripts in this ticket.

## Required Architecture

Do not call `UnityEngine.Physics` from `ShipSimulator`.

Create a collision abstraction:

```csharp
public interface ICollisionWorld
{
    bool SweepShip(
        in ShipCollisionShapeSet shapes,
        Vector3 fromPosition,
        Quaternion fromRotation,
        Vector3 toPosition,
        Quaternion toRotation,
        in ShipCollisionMask mask,
        out ShipCollisionHit hit);
}
```

The responsibilities must remain:

- `ICollisionWorld` performs queries only.
- `UnityCollisionWorld` is the only class in this feature that calls Unity `Physics` sweep APIs.
- `ShipCollisionResolver` applies the returned hit to `ShipState`.
- `ShipSimulator` remains deterministic-ish simulation math; collision resolution runs after `ShipSimulator.Step`.
- Unity Rigidbody never owns ship movement.

Using `UnityEngine.Vector3` and `Quaternion` is acceptable in this phase because `ShipSimulator` already uses them. Do not add dependencies on `GameObject`, `Transform`, `Collider`, `Rigidbody`, or `MonoBehaviour` to `ShipCollisionResolver` or `ShipCollisionShape`.

## Authored Collision Shape Model

### Ship

- `ShipCollisionProxy` lives under ship COG (or references a `collisionRoot` transform under COG).
- Authors place any number of child colliders under the proxy root:
  - `BoxCollider`
  - `CapsuleCollider`
  - convex `MeshCollider` only
- All ship collision colliders must be on layer `ShipHull`, non-trigger, enabled.
- `ShipCollisionShapeBaker` converts authored colliders into a `ShipCollisionShapeSet` in COG-local space at runtime.
- `PlayerShipController` continues to disable colliders under the **visual** mesh subtree only. Colliders under the collision proxy subtree must remain enabled.

Do not use render mesh collision for the player ship. Do not auto-generate ship colliders at runtime.

### World (arena, station, asteroids)

- Authors place colliders directly on obstacle objects in the prefab/scene hierarchy.
- Multiple colliders on one object or subtree compose one logical obstacle (no special grouping component required).
- Solid obstacles use layer `Station` or `MineableAsteroid`.
- Trigger volumes (`DockingTrigger`) stay triggers and are ignored by ship movement sweeps.

Do not auto-generate world collision from imported render meshes in this ticket.

## Unity Query Backend

`UnityCollisionWorld` should implement `ICollisionWorld`.

For each primitive in the baked `ShipCollisionShapeSet`:

- **Box** — `Physics.BoxCast` using baked center, half extents, and local rotation
- **Capsule** — `Physics.CapsuleCast` using baked endpoints, radius, and axis
- **Convex mesh** — `Rigidbody.SweepTest` on the proxy rigidbody with only the target mesh collider enabled, after posing the ship root at the sweep start pose (with `Physics.SyncTransforms` because auto-sync is off)

Sweep rules:

- use the configured obstacle mask
- ignore trigger colliders
- ignore all ship-authored colliders on `ShipHull`
- return the closest hit across all ship primitives

Do not make the ship Rigidbody non-kinematic.

## Collision Resolver

`ShipCollisionResolver` should expose a pure method similar to:

```csharp
public static bool ResolveMovement(
    ref ShipState state,
    in ShipCollisionShapeSet shapes,
    ICollisionWorld collisionWorld,
    in ShipCollisionMask mask,
    Vector3 previousPosition,
    Quaternion previousRotation,
    float restitution,
    float tangentialDamping,
    float skinWidth,
    out ShipCollisionHit hit)
```

Implementation rules:

- sweep from previous pose to proposed pose
- if no hit, leave `state` unchanged
- if hit, clamp position to impact point plus normal * skin width
- reflect only the velocity component moving into the surface normal
- multiply reflected normal velocity by restitution
- multiply tangential velocity by tangential damping
- if final velocity magnitude is below `0.05m/s`, zero it
- do not alter fuel, inputs, docking state, or weapon state

Default tuning:

- `restitution = 0.35`
- `tangentialDamping = 0.85`
- `skinWidth = 0.05m`

Store these values on `ShipCollisionProxy` as serialized fields for now. Do not add them to `ShipTuning` in this ticket.

## Integration

`ShipFlightController.Simulate(...)` should:

1. Capture previous `ShipState.position` and `ShipState.rotation`.
2. Call `ShipSimulator.Step(...)` as it does now.
3. If a collision world/proxy is configured, resolve movement against obstacle layers using the baked `ShipCollisionShapeSet`.
4. Write collision telemetry for HUD/debug.
5. Apply final state to the controlled transform through the existing presentation path.

Collision resolution must not run inside `ShipSimulator` and must not call Unity physics from `ShipSimulator`.

## Layers And Masks

Collision obstacles for this ticket:

- `Station`
- `MineableAsteroid`

Do not collide against:

- `Default` (unless explicitly added later; prefer authoring obstacles on `Station`)
- `DockingTrigger`
- `SceneryAsteroid`
- `ProjectileHit`
- `ShipHull`

Move intentional arena/station solid obstacles in `PF_TestArena` to `Station`.

## Tests

Add EditMode tests for `ShipCollisionResolver` using a fake `ICollisionWorld`.

Minimum tests:

- no hit leaves position and velocity unchanged
- head-on hit clamps position outside the plane and reflects velocity
- tangential velocity is preserved/damped instead of fully reflected
- tiny post-collision velocity is zeroed
- compound shape set delegates to collision world (fake returns hit for multi-shape input)
- resolver does not require Unity `Physics`

Do not require a scene for these tests.

## Acceptance Criteria

- Project compiles:

```powershell
dotnet build Assembly-CSharp.csproj --no-restore
```

- EditMode collision resolver tests compile and pass through the available test path.
- Player ship can no longer fly through configured arena/station obstacle colliders in `FlightTest`.
- Ship bounces off obstacles using custom `ShipState` velocity changes.
- Ship Rigidbody remains kinematic.
- `ShipSimulator` does not directly call `UnityEngine.Physics`.
- Collision query backend is isolated behind `ICollisionWorld`.
- Authors can place multiple box/capsule/convex mesh colliders under `ShipCollisionProxy` and under world obstacle hierarchies.
- No docking behavior is changed.
- No asteroid generation is added.
- No networking package is added.

## Guardrails

- Do not switch to Rigidbody-driven flight.
- Do not use physics joints.
- Do not use non-convex `MeshCollider` for ship or solid obstacles.
- Do not implement damage, death, shields, repair, or insurance.
- Do not add arbitrary render-mesh collision as the authority model.
- Do not auto-generate colliders from render meshes at runtime.
- Do not make collision depend on camera state.
- Do not edit docking tickets or docking scripts.
- Do not add a pure C# server implementation in this ticket; only preserve the seam for it.
