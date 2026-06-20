# P0.1 T20 - Docking And Asteroid Replication

## Goal

Replicate the minimum docking, asteroid activation, and asteroid resource state needed for the 0.1 multiplayer loop.

After this ticket, two connected players should share the same basic world facts: docked/undocked state, promoted asteroid activation, and simple mineable resource depletion.

## Reading Instructions

Read these files before editing:

- `Tickets/P0_1_T09_manual_docking_nodes_camera_and_hud.md`
- `Tickets/P0_1_T10_docking_mode_controls_and_camera_cycle.md`
- `Tickets/P0_1_T11_manual_magnetic_capture_and_docked_state.md`
- `Tickets/P0_1_T12_manual_undock_and_recapture_lockout.md`
- `Tickets/P0_1_T16_mineable_resource_stub.md`
- `Tickets/P0_1_T17_snapshot_and_event_dtos.md`
- `Tickets/P0_1_T18_first_networking_integration.md`
- `Tickets/P0_1_T19_two_ship_friend_test.md`
- `Assets/Scripts/Docking/*.cs`
- `Assets/Scripts/Asteroids/*.cs`
- `Assets/Scripts/Networking/*.cs`
- `Assets/Scripts/NetworkingFuture/*.cs`
- `Assets/Scripts/Authority/LocalGameAuthority.cs`

Do not start until T19 is complete.

## Required Files

Create:

- `Assets/Scripts/Networking/NetworkDockingAuthority.cs`
- `Assets/Scripts/Networking/NetworkAsteroidAuthority.cs`

Modify:

- existing docking controllers
- existing asteroid promotion/resource registries
- networking snapshot/event send path
- `Assets/Scenes/FlightTest.unity`

## Docking Replication

Server owns docking state.

Client may request:

- toggle docking mode locally for camera/HUD only
- deploy/retract own docking node, sent to server
- undock request

Server decides:

- whether capture magnet can activate
- when final docked state occurs
- when undock occurs
- when recapture lockout ends

Replicate:

- ship docking node deployed/retracted state
- docked/free/lockout state
- station port ID
- ship attach pose while docked
- undock event

Keep docking manual. Do not add autopilot.

## Asteroid Activation Replication

Server owns promoted asteroid activation for gameplay-relevant asteroids.

Replicate:

- descriptor ID
- activated/deactivated
- entity ID for promoted asteroid
- position/rotation/radius

Clients may still render far scenery locally from deterministic descriptors. Promoted gameplay asteroids must match server activation for colliders/hits/resources.

## Resource Replication

Server owns resource depletion.

Replicate:

- descriptor ID
- remaining resource amount
- depleted flag
- hit/depletion event

Clients may show debug feedback. Do not add inventory, cargo, selling, or station trading.

## Conflict Rules

If local deterministic asteroid scenery disagrees with server activation:

- server activation wins for promoted gameplay objects
- local scenery may be hidden for that descriptor while active

If client predicts docking capture but server rejects:

- client returns to server snapshot state
- no autopilot correction is added

## Acceptance Criteria

- Project compiles:

```powershell
dotnet build Assembly-CSharp.csproj --no-restore
```

- Two-player session still works from T19.
- Docking state changes are server-owned and visible to both players.
- Undocking and recapture lockout replicate.
- Asteroid promotion/activation for gameplay asteroids is visible to both players.
- Shooting/mining a promoted asteroid changes server resource state and replicates remaining/depleted state.
- Far asteroid scenery remains deterministic/local and does not require per-asteroid network objects.
- No station interior/trading/economy is added.

## Guardrails

- Do not add autopilot docking.
- Do not replicate every scenery asteroid.
- Do not make asteroid resource depletion client-authoritative.
- Do not add persistence.
- Do not add inventory/cargo/economy.
- Do not add lobby/matchmaking.
- Do not rewrite the networking stack.
