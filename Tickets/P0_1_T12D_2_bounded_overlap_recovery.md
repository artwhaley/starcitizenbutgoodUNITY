# P0.1 T12D-2 - Bounded Overlap Recovery

## Goal

Handle the case where the ship starts a simulation frame already overlapping an obstacle, without causing another large visible teleport.

Sweeps alone are not enough. Unity casts can miss or behave poorly when the cast begins inside geometry. Tier 0 needs a bounded depenetration pass after sweep response.

## Reading Instructions

Read before editing:

- `Docs/project_rules.md`
- `Tickets/P0_1_T12D_collision_response_repair_overview.md`
- `Tickets/P0_1_T12D_1_fix_sweep_response_cog_snap.md`
- `Assets/Scripts/Physics/ICollisionWorld.cs`
- `Assets/Scripts/Physics/UnityCollisionWorld.cs`
- `Assets/Scripts/Physics/ShipCollisionResolver.cs`
- `Assets/Scripts/Physics/ShipCollisionProxy.cs`
- `Assets/Scripts/Physics/ShipCollisionShape.cs`
- `Assets/Scripts/Physics/ShipCollisionShapeBaker.cs`
- `Assets/Scripts/Flight/ShipFlightController.cs`
- `Assets/Prefabs/Ships/PF_PlayerShip.prefab`
- `Assets/Prefabs/Arena/PF_TestArena.prefab`

## Required Architecture

Extend `ICollisionWorld` with a query for current-pose overlap recovery. Choose a concrete method signature and update all implementers and tests. Use this shape unless there is a compile-time reason it cannot work:

```csharp
bool ComputeShipPenetration(
    in ShipCollisionShapeSet shapes,
    Vector3 position,
    Quaternion rotation,
    in ShipCollisionMask mask,
    out ShipCollisionHit hit);
```

`ShipCollisionHit.normal` for penetration must point in the direction the ship COG should move to resolve the overlap. `ShipCollisionHit.distance` must be penetration depth in meters.

`UnityCollisionWorld` may use Unity physics APIs to implement this query. Acceptable implementation:

- for each ship primitive, collect nearby overlapping world colliders using `Physics.OverlapBox` / `Physics.OverlapCapsule` / equivalent conservative query
- ignore triggers
- ignore ship colliders
- only consider obstacle mask layers
- call `Physics.ComputePenetration` between the posed ship primitive collider/shape and the obstacle collider, or a practical Unity-backed equivalent
- return the deepest penetration or a stable accumulated correction

If convex mesh depenetration is too risky in this ticket, support box and capsule first and return clear warnings for unsupported convex mesh depenetration. Do not fake success.

## Resolver Behavior

After sweep response in `ShipCollisionResolver.ResolveMovement`, run bounded penetration recovery against the final state.

Add serialized tuning to `ShipCollisionProxy` and pass it into the resolver:

- `maxDepenetrationMetersPerStep`, default `0.25`
- `depenetrationSkinWidth`, default `0.02`
- `maxDepenetrationIterations`, default `3`

Implementation rules:

- If no penetration, leave state unchanged.
- If penetration exists, move COG by:

```csharp
Vector3 correction = hit.normal * Mathf.Min(hit.distance + depenetrationSkinWidth, maxDepenetrationMetersPerStep);
```

- Repeat for at most `maxDepenetrationIterations`.
- Never apply an unbounded correction in one frame.
- If velocity is still moving into the penetration normal, remove the inward component:

```csharp
float inward = Vector3.Dot(state.linearVelocity, hit.normal);
if (inward < 0f)
{
    state.linearVelocity -= hit.normal * inward;
}
```

- Do not add bounce energy during pure depenetration. Bounce belongs to sweep impact response.

## Debug Telemetry

Extend existing collision telemetry enough to inspect:

- whether sweep collision happened
- whether depenetration happened
- penetration correction magnitude
- hit normal

Keep this minimal. Do not build a new UI unless an existing debug HUD field can be updated with little risk.

## Guardrails

- Do not let Unity Rigidbody contacts drive movement.
- Do not use `OnCollisionEnter` or `OnTriggerStay` for ship movement response.
- Do not apply unlimited depenetration correction.
- Do not silently invent collision geometry.
- Do not include `DockingTrigger`, `ProjectileHit`, `SceneryAsteroid`, `Default`, or `ShipHull` in obstacle queries.
- Do not edit docking behavior.

## Tests

Add or update EditMode tests with a fake `ICollisionWorld`.

Minimum tests:

- no overlap after sweep produces no depenetration correction
- overlap correction moves along penetration normal
- correction is capped by `maxDepenetrationMetersPerStep`
- repeated correction stops after `maxDepenetrationIterations`
- inward velocity along penetration normal is removed
- depenetration does not add bounce velocity

## Verification

Run:

```powershell
dotnet build Assembly-CSharp.csproj --no-restore
dotnet build Assembly-CSharp-Editor.csproj --no-restore
```

Run available EditMode tests for collision resolver if possible.

## Acceptance Criteria

- Starting inside or slightly penetrating an arena obstacle does not launch the ship or teleport it a large distance.
- Recovery moves are bounded and visible as small correction, not a snap across the scene.
- Sweep collision response from T12D-1 still works.
- Runtime and editor builds pass.

