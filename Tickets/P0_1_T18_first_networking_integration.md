# P0.1 T18 - First Networking Integration

## Goal

Add the first real networking integration using Netcode for GameObjects (NGO) and replicate server-owned ship movement from client input commands.

After this ticket, one host/server and one client should be able to connect in a simple test flow and see server-authoritative ship snapshots.

## Reading Instructions

Read these files before editing:

- `Tickets/P0_1_T17_snapshot_and_event_dtos.md`
- `Assets/Scripts/NetworkingFuture/*.cs`
- `Assets/Scripts/Authority/LocalGameAuthority.cs`
- `Assets/Scripts/Flight/ShipFlightController.cs`
- `Assets/Scripts/Flight/ShipSimulator.cs`
- `Assets/Scripts/Flight/PlayerShipController.cs`
- `Assets/Scripts/Input/ShipInputReader.cs`
- `Assets/Scripts/World/WorldEntity.cs`
- `Assets/Scripts/World/LocalEntityRegistry.cs`
- `Assets/Prefabs/Ships/PF_PlayerShip.prefab`
- `Assets/Scenes/FlightTest.unity`
- `Packages/manifest.json`
- `ProjectSettings/ProjectSettings.asset`

Do not start until T17 is complete.

## Networking Stack Decision

Use Unity Netcode for GameObjects.

Package:

- `com.unity.netcode.gameobjects`

Use Unity Transport if NGO requires it:

- `com.unity.transport`

Do not evaluate or switch to Mirror/FishNet/custom sockets in this ticket.

## Required Files

Create:

- `Assets/Scripts/Networking/NetworkGameBootstrap.cs`
- `Assets/Scripts/Networking/NetworkShipAuthority.cs`
- `Assets/Scripts/Networking/NetworkShipInputClient.cs`
- `Assets/Scripts/Networking/NetworkShipSnapshotInterpolator.cs`
- `Assets/Scripts/Networking/NetworkEntityIdMap.cs`

Modify:

- `Packages/manifest.json`
- `Assets/Prefabs/Ships/PF_PlayerShip.prefab`
- `Assets/Scenes/FlightTest.unity`
- `Assets/Scripts/Authority/LocalGameAuthority.cs` only as needed to coexist with network authority
- `Assets/Scripts/Flight/PlayerShipController.cs` only as needed to route local input to network client when networked

## Bootstrap

Create a minimal in-scene bootstrap with UI or hotkeys:

- start host
- start client
- start server if supported in editor

Keyboard shortcuts are acceptable:

- `F9` start host
- `F10` start client
- `F11` start server

No login UI in this ticket.

## Authority Model

Server owns ship simulation.

Client:

- reads local input
- sends `PlayerInputDto` or equivalent to server
- predicts only if trivial and safe; prediction is optional in this ticket

Server:

- receives input
- advances `ShipSimulator` on fixed network tick
- sends `EntitySnapshotDto` or equivalent snapshot to clients

Remote clients:

- interpolate snapshots for visual motion

Do not let each client independently simulate authoritative ship state.

## Ship Spawning

Support two ships for the upcoming friend test:

- host/player 1 ship
- client/player 2 ship

For this ticket, spawning at fixed separated positions is acceptable.

Default positions:

- player 1: `(0, 0, 0)`
- player 2: `(30, 0, 0)`

Do not add character selection or persistence.

## Prefab Setup

Add required NGO components to the ship prefab:

- `NetworkObject`
- networking authority/input/snapshot components created in this ticket

Preserve existing local single-player play mode. If no network session is active, existing local authority path should still work.

## Acceptance Criteria

- Project compiles:

```powershell
dotnet build Assembly-CSharp.csproj --no-restore
```

- NGO package is installed.
- Host can start from `FlightTest`.
- Client can connect locally to host.
- Server receives input commands from each client.
- Server advances ship simulation.
- Clients receive and display ship snapshots.
- Existing local play without starting network still works.
- Weapons, docking, and asteroids do not need full replication yet.

## Guardrails

- Do not implement account login.
- Do not implement matchmaking.
- Do not add Steam/Epic integration.
- Do not implement full rollback/prediction.
- Do not make client authority the source of truth.
- Do not rewrite the flight simulator.
- Do not remove local single-player mode.
