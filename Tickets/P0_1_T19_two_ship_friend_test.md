# P0.1 T19 - Two Ship Friend Test

## Goal

Create a usable two-player friend test loop: two ships connect to a host/listen server, fly together, and fire basic weapons with enough interpolation to judge whether flying/fighting together is fun.

After this ticket, the project should support the first real P0.1 multiplayer playtest.

## Reading Instructions

Read these files before editing:

- `Tickets/P0_1_T18_first_networking_integration.md`
- `Assets/Scripts/Networking/*.cs`
- `Assets/Scripts/Weapons/PrimaryWeaponController.cs`
- `Assets/Scripts/Weapons/WeaponDefinition.cs`
- `Assets/Scripts/Projectiles/ProjectileWorld.cs`
- `Assets/Scripts/Projectiles/*`
- `Assets/Scripts/Gameplay/IHitReceiver.cs`
- `Assets/Scripts/Flight/ShipFlightController.cs`
- `Assets/Scripts/Input/ShipInputReader.cs`
- `Assets/Prefabs/Ships/PF_PlayerShip.prefab`
- `Assets/Scenes/FlightTest.unity`

Do not start until T18 is complete.

## Required Files

Create:

- `Assets/Scripts/Networking/NetworkProjectileAuthority.cs`
- `Assets/Scripts/Networking/NetworkHitReplication.cs`
- `Assets/Scripts/Networking/NetworkPlaytestHud.cs`

Modify:

- `Assets/Scripts/Networking/NetworkGameBootstrap.cs`
- `Assets/Scripts/Networking/NetworkShipAuthority.cs`
- `Assets/Scripts/Weapons/PrimaryWeaponController.cs`
- `Assets/Scripts/Projectiles/ProjectileWorld.cs`
- `Assets/Scenes/FlightTest.unity`

## Playtest Flow

Required:

- host starts a listen server
- friend/client connects by address using Unity Transport default local/LAN path
- each player gets one ship
- each player controls only their own ship
- each player can see the other ship move
- each player can fire
- weapon fire is server-owned
- hits are server-owned

No lobby is required. A small debug HUD is enough.

## Weapon Replication

Use server authority:

- client sends fire input/request
- server validates controlled ship
- server spawns or simulates projectile
- server sends projectile spawn/hit/despawn events

For phase 0.1, hits may be simple:

- hit other ship collision proxy
- show a debug hit event
- optional health number may be added only if needed for visibility

Do not build full damage/death.

## Interpolation

Remote ship motion must be smooth enough for a friend test.

Implement simple snapshot interpolation:

- buffer recent snapshots
- render remote ships slightly behind server time
- default interpolation delay: `100ms`
- extrapolate for short gaps only, max `150ms`

Do not implement rollback.

## Scene

`FlightTest` should support:

- local single-player if no network session is started
- host mode
- client mode

Add a minimal on-screen network status:

- disconnected / host / client / server
- local player entity ID
- connected client count

## Acceptance Criteria

- Project compiles:

```powershell
dotnet build Assembly-CSharp.csproj --no-restore
```

- Host and one client can fly two separate ships.
- Each player sees the other ship moving.
- Each player can fire.
- Fire/hit events are server-owned.
- Remote ship interpolation is smooth enough to playtest.
- Existing single-player path remains usable.
- Docking and asteroid replication are not required yet.

## Guardrails

- Do not add accounts or persistence.
- Do not implement lobby/matchmaking.
- Do not add full damage/death rules.
- Do not make weapon hits client-authoritative.
- Do not remove local test controls.
- Do not block playtest on polish UI.
