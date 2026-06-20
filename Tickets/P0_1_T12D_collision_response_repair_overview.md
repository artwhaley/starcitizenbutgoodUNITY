# P0.1 T12D - Collision Response Repair Overview

## Goal

Repair the Tier 0 ship-vs-environment collision implementation so it feels like a physical bounce/slide instead of a positional snap.

The current collision architecture is broadly acceptable for phase 0.1:

- custom ship simulation owns authoritative ship state
- Unity physics is used as a collision query backend
- `ShipCollisionResolver` applies collision response to `ShipState`
- Unity `Rigidbody` must not own ship movement

The broken behavior is in response math, not in the decision to use Unity queries. The known failure is that the resolver currently writes the ship COG to a collision contact point. That causes the whole ship to jump when a sweep touches an obstacle.

This stack must make collision good enough for the first friend test:

- solid arena/station obstacles stop pass-through
- collision does not teleport the ship
- impact response is a predictable bounce or slide
- already-overlapping starts recover gently, within bounds
- the design remains compatible with a future pure C# collision backend

## Stack

Execute in order:

1. `P0_1_T12D_1_fix_sweep_response_cog_snap.md`
2. `P0_1_T12D_2_bounded_overlap_recovery.md`
3. `P0_1_T12D_3_collision_playtest_and_regression.md`

## Architectural Decision

Use Unity physics on Unity client/headless server for phase 0.1 collision queries.

Do not use Unity Rigidbody simulation for ship motion. The ship is still moved by `ShipState`:

```text
ShipSimulator.Step
  -> proposed ShipState

ShipCollisionResolver
  -> sweep / depenetration query through ICollisionWorld
  -> final ShipState position and velocity

ShipPresentationController
  -> applies final ShipState to COG transform
```

`UnityCollisionWorld` may call Unity `Physics` APIs. `ShipCollisionResolver`, `ShipCollisionShape`, and `ShipSimulator` must not depend on `Collider`, `Rigidbody`, `GameObject`, or `Transform`.

## Non-Goals

- Do not implement damage.
- Do not implement fatal collisions.
- Do not implement shield/armor.
- Do not add networking.
- Do not add asteroid generation.
- Do not switch ships to Rigidbody-driven physics.
- Do not write a pure C# collision backend yet.
- Do not auto-generate colliders from render meshes.
- Do not edit docking behavior unless a docking compile error is caused by this stack.

