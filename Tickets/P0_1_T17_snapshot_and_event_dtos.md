# P0.1 T17 - Snapshot And Event DTOs

## Goal

Define package-agnostic DTOs for inputs, entity snapshots, projectile events, docking events, and asteroid activation/resource events.

After this ticket, the simulation has a clear data contract for networking without yet adding a networking package.

## Reading Instructions

Read these files before editing:

- `Tickets/P0_1_T01_entity_identity_and_registry.md`
- `Tickets/P0_1_T08_reference_frame_ids.md`
- `Tickets/P0_1_T11_manual_magnetic_capture_and_docked_state.md`
- `Tickets/P0_1_T12_manual_undock_and_recapture_lockout.md`
- `Tickets/P0_1_T16_mineable_resource_stub.md`
- `Assets/Scripts/Flight/ShipInputCommand.cs`
- `Assets/Scripts/Flight/ShipState.cs`
- `Assets/Scripts/Flight/ShipThrusterOutput.cs`
- `Assets/Scripts/MultiplayerFuture/EntitySnapshot.cs`
- `Assets/Scripts/Projectiles/ProjectileWorld.cs`
- `Assets/Scripts/Docking/DockingState.cs`
- `Assets/Scripts/Asteroids/AsteroidDescriptorId.cs`
- `Assets/Scripts/Asteroids/AsteroidResourceState.cs`
- `Assets/Scripts/World/EntityId.cs`
- `Assets/Scripts/World/ReferenceFrameId.cs`

Do not start until T16 is complete. If docking tickets are not complete yet, define docking DTOs using the ticket contracts and compile them after docking lands.

## Required Files

Create:

- `Assets/Scripts/NetworkingFuture/NetworkTick.cs`
- `Assets/Scripts/NetworkingFuture/PlayerInputDto.cs`
- `Assets/Scripts/NetworkingFuture/EntitySnapshotDto.cs`
- `Assets/Scripts/NetworkingFuture/ProjectileEventDto.cs`
- `Assets/Scripts/NetworkingFuture/DockingEventDto.cs`
- `Assets/Scripts/NetworkingFuture/AsteroidActivationEventDto.cs`
- `Assets/Scripts/NetworkingFuture/AsteroidResourceEventDto.cs`
- `Assets/Scripts/NetworkingFuture/DtoConversion.cs`
- `Assets/Tests/EditMode/NetworkingDtoConversionTests.cs`

Use namespace `FlightModel.NetworkingFuture`.

## DTO Rules

DTOs must be:

- plain structs or sealed classes
- serializable by value
- no `MonoBehaviour`
- no `GameObject`
- no `Transform`
- no `Collider`
- no NGO, Mirror, Steam, Epic, or transport package types
- no direct dependency on a networking package

Using current project value types like `EntityId`, `ReferenceFrameId`, `AsteroidDescriptorId`, `Vector3`, and `Quaternion` is acceptable.

## Required DTOs

`NetworkTick`:

- `uint value`
- comparison/equality
- increment helper

`PlayerInputDto`:

- `EntityId controlledEntityId`
- `NetworkTick tick`
- input axes matching `ShipInputCommand`
- boost/fine/brake/fire/docking relevant button states

`EntitySnapshotDto`:

- `EntityId entityId`
- entity kind
- `ReferenceFrameId frameId`
- position
- rotation
- linear velocity
- angular velocity
- optional docked state fields if docking exists

`ProjectileEventDto`:

- spawn event
- hit event
- despawn event
- projectile ID
- owner entity
- position/rotation/velocity
- tick

`DockingEventDto`:

- request dock/capture
- docked
- undock requested
- undocked
- recapture lockout state
- ship entity
- station entity if available
- port ID
- tick

`AsteroidActivationEventDto`:

- descriptor ID
- entity ID if promoted
- activated/deactivated
- position/rotation/radius
- tick

`AsteroidResourceEventDto`:

- descriptor ID
- remaining resource
- depleted flag
- applied delta
- tick

## Conversion

`DtoConversion` should contain explicit conversion methods:

- `ShipInputCommand` to/from `PlayerInputDto` where possible
- `ShipState` to `EntitySnapshotDto`
- asteroid resource state to resource DTO

Do not hide conversion in implicit operators. Use named methods.

## Tests

Add tests for:

- input DTO round-trips core axes/buttons
- ship state snapshot preserves frame ID, position, velocity, and rotation
- asteroid resource DTO preserves descriptor ID and remaining amount
- DTOs do not reference networking package types

## Acceptance Criteria

- Project compiles:

```powershell
dotnet build Assembly-CSharp.csproj --no-restore
```

- DTO tests compile and pass.
- No networking package is added.
- DTOs are package-agnostic.
- DTOs cover input, snapshots, projectile events, docking events, asteroid activation, and asteroid resources.
- Existing single-player behavior is unchanged.

## Guardrails

- Do not add NGO/Mirror/transport yet.
- Do not implement replication.
- Do not open sockets.
- Do not add login/auth.
- Do not mutate gameplay state from DTO constructors.
- Do not create hidden dependencies on scene objects.
