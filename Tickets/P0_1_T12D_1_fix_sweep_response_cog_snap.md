# P0.1 T12D-1 - Fix Sweep Response COG Snap

## Goal

Fix the jarring collision teleport where the ship jumps to a strange position near an obstacle.

The current bug is in `ShipCollisionResolver.ResolveMovement`: it places `state.position` at `hit.point + hit.normal * skinWidth`. `state.position` is the ship COG, while `hit.point` is a surface/contact point from the cast. Do not assign the COG to a contact point.

After this ticket, a sweep hit must move the ship COG to the safe travel position just before impact, then adjust velocity into a bounce/slide.

## Reading Instructions

Read before editing:

- `Docs/project_rules.md`
- `Tickets/P0_1_T12A_custom_collision_query_and_bounce.md`
- `Tickets/P0_1_T12D_collision_response_repair_overview.md`
- `Assets/Scripts/Physics/ShipCollisionResolver.cs`
- `Assets/Scripts/Physics/ShipCollisionHit.cs`
- `Assets/Scripts/Physics/ICollisionWorld.cs`
- `Assets/Scripts/Physics/UnityCollisionWorld.cs`
- `Assets/Scripts/Flight/ShipFlightController.cs`
- `Assets/Scripts/Flight/ShipState.cs`
- `Assets/Scripts/Flight/ShipSimulator.cs`
- `Assets/Prefabs/Ships/PF_PlayerShip.prefab`
- `Assets/Prefabs/Arena/PF_TestArena.prefab`

## Required Behavior

When `ResolveMovement` receives a sweep hit:

1. Compute travel from COG start to proposed COG position:

```csharp
Vector3 travel = proposedPosition - previousPosition;
float travelDistance = travel.magnitude;
Vector3 travelDirection = travel / travelDistance;
```

2. Place the COG on the original travel path, not at `hit.point`:

```csharp
float safeDistance = Mathf.Max(0f, hit.distance - skinWidth);
state.position = previousPosition + travelDirection * safeDistance;
```

3. Preserve the proposed rotation from `ShipSimulator.Step` for this ticket:

```csharp
state.rotation = proposedRotation;
```

This does not solve rotational collision yet, but it prevents translation response from snapping the COG to the contact point.

4. Adjust velocity only if velocity is moving into the surface:

```csharp
float normalSpeed = Vector3.Dot(velocity, hit.normal);
if (normalSpeed < 0f)
{
    Vector3 normalVelocity = hit.normal * normalSpeed;
    Vector3 tangentialVelocity = velocity - normalVelocity;
    velocity = -normalVelocity * restitution + tangentialVelocity * tangentialDamping;
}
```

5. If the final velocity magnitude is below `0.05m/s`, zero it.

## Guardrails

- Do not use `hit.point` as a ship COG position.
- Do not let Rigidbody physics move the ship.
- Do not change `ShipSimulator.Step`.
- Do not call `UnityEngine.Physics` from `ShipCollisionResolver`.
- Do not implement overlap recovery in this ticket; that is T12D-2.
- Do not change docking, weapons, asteroid, or networking code.

## Tests

Add or update EditMode tests for `ShipCollisionResolver` using a fake `ICollisionWorld`.

Minimum tests:

- no hit leaves position and velocity unchanged
- head-on hit places COG at `previousPosition + direction * (hit.distance - skinWidth)`, not at `hit.point`
- head-on hit reflects only the inward normal component
- tangential velocity is damped but not reversed
- tiny post-collision velocity is zeroed

The fake hit should deliberately use a `hit.point` that is not equal to the expected COG position so the old bug would fail the test.

## Verification

Run:

```powershell
dotnet build Assembly-CSharp.csproj --no-restore
dotnet build Assembly-CSharp-Editor.csproj --no-restore
```

Run available EditMode tests for collision resolver if the local Unity test runner path is available. If not available, report that tests compile but were not executed.

## Acceptance Criteria

- Ship collision no longer snaps COG to `RaycastHit.point`.
- Sweep response clamps along original COG travel path.
- Velocity response still bounces/slides.
- Runtime and editor builds pass.
- Existing docking behavior remains untouched.

