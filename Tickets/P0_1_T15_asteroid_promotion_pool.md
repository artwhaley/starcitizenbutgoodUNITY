# P0.1 T15 - Asteroid Promotion Pool

## Goal

Promote nearby asteroid descriptors into pooled interactive GameObjects with colliders and `WorldEntity`, then demote them when they are far away.

After this ticket, close asteroids can participate in hit/collision/mining systems while far asteroids remain cheap instanced scenery.

## Reading Instructions

Read these files before editing:

- `Tickets/P0_1_T13_asteroid_sector_descriptors.md`
- `Tickets/P0_1_T14_instanced_asteroid_scenery.md`
- `Tickets/P0_1_T12A_custom_collision_query_and_bounce.md`
- `Assets/Scripts/Asteroids/AsteroidDescriptor.cs`
- `Assets/Scripts/Asteroids/AsteroidSceneryRenderer.cs`
- `Assets/Scripts/World/WorldEntity.cs`
- `Assets/Scripts/World/EntityKind.cs`
- `Assets/Scripts/World/LocalEntityRegistry.cs`
- `Assets/Scripts/Physics/ICollisionWorld.cs`
- `Assets/Scripts/Weapons/SimpleTarget.cs`
- `ProjectSettings/TagManager.asset`

Do not start until T14 is complete.

## Required Files

Create:

- `Assets/Scripts/Asteroids/AsteroidInstance.cs`
- `Assets/Scripts/Asteroids/AsteroidPromotionPool.cs`
- `Assets/Scripts/Asteroids/AsteroidPromotionSettings.cs`

Modify:

- `Assets/Scripts/Asteroids/AsteroidSceneryRenderer.cs`
- `Assets/Scripts/World/EntityKind.cs` if asteroid kind is missing
- `Assets/Scenes/FlightTest.unity`

## Promotion Rules

Use descriptor identity as the source of truth.

Defaults:

- promote radius: `250m`
- demote radius: `325m`
- max promoted asteroids: `128`
- collider type: sphere collider scaled to descriptor radius

Promotion:

- choose descriptors within promote radius of player ship
- pull an object from pool
- set transform from descriptor
- assign descriptor ID
- add/configure `WorldEntity`
- set layer to `MineableAsteroid`
- enable collider
- hide or suppress its instanced scenery copy if it would visually overlap

Demotion:

- when descriptor is beyond demote radius
- disable collider
- return object to pool
- keep descriptor state available for future promotion
- restore instanced scenery visibility if applicable

Use hysteresis: do not promote/demote at the same exact distance.

## Pool

`AsteroidPromotionPool` should prewarm pooled objects or grow up to max.

Each pooled asteroid should have:

- `AsteroidInstance`
- `WorldEntity`
- `SphereCollider`
- visual mesh renderer

Do not instantiate/destroy asteroids every frame.

## Collision Integration

Promoted asteroids must be included in the same collision query path created by T12A:

- layer `MineableAsteroid`
- simple collider proxy
- no render mesh collider

The ship may bounce off promoted asteroids if T12A is complete.

## Acceptance Criteria

- Project compiles:

```powershell
dotnet build Assembly-CSharp.csproj --no-restore
```

- Nearby descriptors promote into pooled asteroid GameObjects.
- Far descriptors demote back to scenery-only state.
- Promoted asteroids have colliders on `MineableAsteroid`.
- Promoted asteroids have stable descriptor IDs.
- Pool does not instantiate/destroy continuously while hovering near threshold.
- Scenery renderer does not double-draw promoted asteroids in an obvious way.
- No resource mining is implemented yet.
- No networking package is added.

## Guardrails

- Do not create GameObjects for all visible asteroids.
- Do not use MeshCollider for asteroids.
- Do not allocate large lists every frame if avoidable.
- Do not add resource depletion.
- Do not add persistence.
- Do not change docking behavior.
