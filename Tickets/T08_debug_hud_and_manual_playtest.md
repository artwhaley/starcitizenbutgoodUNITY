# T08 - Debug HUD And Manual Playtest

## Goal

Expose enough live telemetry to verify the rewritten solver and document a repeatable manual test procedure for the U-wing.

## Primary Files

- `Assets/Scripts/Debug/FlightDebugHud.cs`
- `README.md`
- `Docs/flight_tuning_model.md`
- new test notes under `Docs/` if needed

## Scope

- Add or expand HUD/debug readouts for:
  - active assist mode
  - boost state
  - fine-control state
  - current linear speed
  - current angular rates
  - current mass
  - remaining fuel
  - remaining hypergolic
  - requested linear/angular command
  - applied linear/angular command
  - cap-blocked state if practical
- Write a concise manual test procedure for:
  - normal forward acceleration
  - reverse/strafe authority
  - angular caps
  - assist stabilization
  - brake behavior
  - boost behavior
  - fine-control behavior
  - propellant burn
  - boost/fine-control VFX transitions

## Acceptance Criteria

- A developer can tell why the ship is or is not accelerating from the HUD/debug data.
- Manual playtest steps are written down and can be repeated after tuning changes.
- The documented test covers both solver behavior and visual output.

## Guardrails

- Keep telemetry high signal. We want enough data to debug feel, not a wall of noise.
