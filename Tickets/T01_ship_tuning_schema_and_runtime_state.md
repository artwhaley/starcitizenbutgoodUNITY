# T01 - Ship Tuning Schema And Runtime State

## Goal

Replace the current coarse `ShipTuning` and `ShipState` layout with a schema that can represent asymmetric per-axis ship authority, speed limits, boost behavior, fine-control limits, and propellant state.

## Primary Files

- `Assets/Scripts/Flight/ShipTuning.cs`
- `Assets/Scripts/Flight/ShipState.cs`
- `Assets/Scripts/Flight/ShipTuningJsonStore.cs`
- `Assets/Scripts/Flight/ShipTuningProfileLibrary.cs`

## Scope

- Redesign `ShipTuning` around dry-mass acceleration values instead of force values.
- Add asymmetric linear authority for:
  - forward main-engine acceleration
  - forward maneuver acceleration
  - reverse acceleration
  - right acceleration
  - left acceleration
  - up acceleration
  - down acceleration
- Add asymmetric angular authority for:
  - pitch positive/negative acceleration
  - yaw positive/negative acceleration
  - roll positive/negative acceleration
- Add max linear speed.
- Add boost max linear speed.
- Add boost acceleration multiplier.
- Add per-axis max angular speeds.
- Add boost angular speed multiplier or equivalent boosted caps.
- Add fine-control linear acceleration caps.
- Add fine-control max linear speed.
- Add fine-control angular acceleration caps if implemented at tuning level.
- Add dry mass plus runtime mass support.
- Add `fuel` and `hypergolic` capacity and burn-rate fields.
- Add assist responsiveness fields that describe controller aggressiveness, not fake drag.
- Expand tuning JSON save/load DTO to include all new fields.
- Expand runtime `ShipState` to include the mode/resource data the solver needs.

## Acceptance Criteria

- `ShipTuning` no longer uses the old `maxThrustNewtons` and `maxTorque` shape as the authoritative flight model.
- `ShipState` contains enough runtime state to support:
  - assist mode
  - boost state
  - fine-control state
  - current mass
  - remaining fuel
  - remaining hypergolic
  - any applied-output telemetry needed by later tickets
- Tuning save/load still compiles and preserves the new fields.
- Existing scene references can still point to a `ShipTuning` asset without breaking the project at compile time.

## Guardrails

- Keep field names designer-readable. Optimize for “tune by feel,” not textbook physics naming.
- Do not cram autopilot-specific data into `ShipState` yet.
- This ticket is schema/runtime-state only. Do not rewrite solver math here.
