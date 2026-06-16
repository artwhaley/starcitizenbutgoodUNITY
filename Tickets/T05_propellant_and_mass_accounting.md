# T05 - Propellant And Mass Accounting

## Goal

Track propellant use from actual applied thrust and make runtime mass a first-class part of the ship model.

## Primary Files

- `Assets/Scripts/Flight/ShipFlightController.cs`
- `Assets/Scripts/Flight/ShipState.cs`
- `Assets/Scripts/Flight/ShipTuning.cs`
- `Assets/Scripts/Debug/FlightDebugHud.cs`

## Scope

- Add current `fuel` and `hypergolic` state to runtime ship state.
- Burn `fuel` from main-engine forward thrust only.
- Burn `hypergolic` from maneuvering thrust, rotational thrust, assist-generated thrust, brake thrust, and boost rear-thruster assist.
- Scale burn from actual applied thrust, not requested input.
- Stop burn when thrust is clamped to zero by speed caps, lack of authority, or resource exhaustion.
- Structure runtime mass so future cargo/fuel/damage can modify it cleanly.
- If current fuel mass contribution is not implemented yet, leave a clear hook for it.

## Acceptance Criteria

- A tiny applied maneuver uses less propellant than a sustained high-authority maneuver.
- Speed-clamped or cap-blocked thrust does not continue burning propellant.
- Brake and assist use propellant because they are real counter-thrust.
- Runtime mass is not hard-coded to dry mass.

## Guardrails

- Do not build a full economy or refuel system here.
- Keep burn math straightforward and inspectable in debug output.
