# P0.1 T12B - Vehicle Possession Refactor Overview

## Purpose

This is an umbrella ticket for splitting "local player controls this ship" away from "this ship exists as a vehicle entity."

Do not execute this umbrella as one large refactor. Execute the sub-tickets in order:

1. `P0_1_T12B_1_vehicle_and_pilot_seat_shell.md`
2. `P0_1_T12B_2_local_player_vehicle_controller_input_events.md`
3. `P0_1_T12B_3_authority_submission_and_presentation_ownership.md`
4. `P0_1_T12B_4_shrink_or_remove_player_ship_controller.md`
5. `P0_1_T12B_5_idle_and_docked_activation_modes.md`

Each sub-ticket must compile and preserve current flight, weapon, camera, docking, capture, and undock behavior before the next sub-ticket starts.

## Why This Exists

The ship is not the character. It is a vehicle entity that can be:

- occupied by the local player
- occupied by a remote player
- occupied by AI
- docked and powered down
- unoccupied but still persistent
- scenery or cargo while attached to a station arm

`PlayerShipController` currently still acts as the local player, vehicle coordinator, subsystem bootstrapper, input event router, authority submitter, docking HUD owner, camera router, and prefab repair path. That shape will fight on-foot station play, AI pilots, server authority, remote ships, and idle/docked ship optimization.

## Non-Negotiable Guardrails

- Read `Docs/project_rules.md` before executing any sub-ticket.
- Do not invent missing authored setup at runtime.
- Do not create fake docking nodes, station ports, hardpoints, or ship roots.
- Do not assume gameplay markers are direct children of ship root or COG.
- Do not rewrite docking math during this refactor.
- Do not change input bindings.
- Do not change tuning values.
- Do not add on-foot movement yet.
- Do not add networking packages yet.
- Do not make asteroid changes.

## Target Model

After all sub-tickets:

- `ShipVehicle` represents the durable vehicle entity and its ship-owned systems.
- `ShipPilotSeat` represents a controllable seat/position on the vehicle.
- `LocalPlayerVehicleController` represents local human possession and local input/camera/HUD routing.
- `PlayerShipController` is removed or reduced to a temporary thin facade.
- The current `FlightTest` still starts with the local player seated in `PF_PlayerShip`.
- A ship can conceptually exist without local input, camera ownership, or player HUD.

## Required Verification At Every Sub-Ticket Boundary

Run:

```powershell
dotnet build Assembly-CSharp.csproj --no-restore
dotnet build Assembly-CSharp-Editor.csproj --no-restore
```

Manual smoke test in `FlightTest`:

1. Press Play.
2. Fly forward/back/strafe/up/down.
3. Toggle assist with `F`.
4. Toggle camera with `V`.
5. Fire with `Space` / LMB.
6. Deploy docking port.
7. Enter docking camera.
8. Dock.
9. Confirm controls are gated while captured/docked.
10. Undock.
11. Continue flying.

Stop and fix regressions immediately. Do not proceed to the next sub-ticket with a broken smoke test.

## Final Acceptance

- Local player possession is represented by a controller possessing a pilot seat, not by the ship pretending to be the player.
- Existing fly/fire/dock/undock loop still works.
- `PlayerShipController` no longer owns input event routing, authority submission, docking HUD telemetry, camera routing, weapon routing, and runtime subsystem creation all at once.
- No authored gameplay setup is fabricated at runtime.
