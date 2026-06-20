# P0.1 T12D-3 - Collision Playtest And Regression

## Goal

Verify Tier 0 collision behavior in `FlightTest` after T12D-1 and T12D-2.

This ticket is verification-heavy. Fix only direct regressions in the collision stack.

## Reading Instructions

Read before testing/fixing:

- `Docs/project_rules.md`
- `Tickets/P0_1_T12D_collision_response_repair_overview.md`
- `Tickets/P0_1_T12D_1_fix_sweep_response_cog_snap.md`
- `Tickets/P0_1_T12D_2_bounded_overlap_recovery.md`
- `Assets/Scripts/Physics/ShipCollisionResolver.cs`
- `Assets/Scripts/Physics/UnityCollisionWorld.cs`
- `Assets/Scripts/Physics/ShipCollisionProxy.cs`
- `Assets/Scripts/Flight/ShipFlightController.cs`
- `Assets/Scripts/Flight/ShipPresentationController.cs`
- `Assets/Prefabs/Ships/PF_PlayerShip.prefab`
- `Assets/Prefabs/Arena/PF_TestArena.prefab`
- `Assets/Scenes/FlightTest.unity`
- `ProjectSettings/TagManager.asset`

## Required Checks

### Build

Run:

```powershell
dotnet build Assembly-CSharp.csproj --no-restore
dotnet build Assembly-CSharp-Editor.csproj --no-restore
```

### Static Setup Checks

Confirm:

- player ship collision proxy exists under/near COG
- ship collision proxy has authored primitive collider(s)
- ship Rigidbody remains kinematic and gravity disabled
- ship collision proxy layer is `ShipHull`
- arena solid obstacles are on `Station` or another explicitly included solid obstacle layer
- docking trigger volumes remain triggers and are not used for movement collision
- obstacle mask excludes `ShipHull`, `DockingTrigger`, `ProjectileHit`, `SceneryAsteroid`, and `Default`

### Manual Playtest Matrix

In `FlightTest`, test:

- slow head-on collision with a station/arena box
- fast head-on collision with a station/arena box
- shallow glancing collision along a box face
- corner/edge impact
- braking into an obstacle
- RCS-only movement into an obstacle
- rotation near obstacle
- backing away after collision

Expected results:

- no pass-through on configured solid obstacle colliders
- no COG snap to surface contact point
- no sudden jump across or around the obstacle
- head-on impact bounces back according to restitution
- glancing impact slides with tangential damping
- small overlaps recover with bounded correction
- ship remains controllable after impact

### Regression Checks

Also verify:

- normal flight still works
- boost still works
- braking/assist modes still work
- weapons still spawn from hardpoints
- RCS/main engine VFX still respond
- docking mode/camera/capture/undock still works at a basic smoke-test level
- no fake nodes, hardpoints, ports, or collision geometry are created

## Fix Scope

If a check fails:

- fix the smallest collision-stack issue that explains the failure
- do not refactor docking, weapons, networking, or asteroid systems
- do not change ship tuning values unless collision tuning is the explicit failing behavior
- do not introduce Rigidbody-driven motion

## Acceptance Criteria

- Runtime and editor builds pass.
- Collision resolver tests pass or are reported as compile-only if no local Unity test runner is available.
- The player ship bounces/slides off configured arena obstacles without jarring teleport.
- Collision remains custom-state driven with Unity used only as query backend.
- No unrelated systems regress in the smoke checks.

