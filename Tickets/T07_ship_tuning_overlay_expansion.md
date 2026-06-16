# T07 - Ship Tuning Overlay Expansion

## Goal

Turn the ship tuning overlay into a complete per-ship tuning surface for the new flight model while keeping hardware bindings separate.

## Primary Files

- `Assets/Scripts/UI/FlightTuningOverlay.cs`
- `Assets/Scripts/Flight/ShipTuning.cs`
- `Assets/Scripts/Flight/ShipTuningJsonStore.cs`
- `Assets/Prefabs/UI/PF_FlightTuningOverlay.prefab`

## Scope

- Expand the tuning overlay to expose all relevant ship-level fields with clear labels and editable numeric values.
- Keep the input bindings panel focused only on joystick/button calibration and assignment.
- Add mode-related tuning fields for:
  - boost speed/response
  - fine-control caps
- Add fuel/hypergolic tuning fields where useful for iteration.
- Save runtime tuning overrides per ship/profile key rather than one global override file.
- Preserve the runtime-copy workflow so playtest tuning does not mutate the source asset every frame.

## Acceptance Criteria

- The tuning overlay can edit the U-wing’s full ship behavior without touching global joystick calibration.
- Every field shown is clearly labeled and maps to a real ship tuning field.
- Runtime save/load works per ship identity rather than one global file for all ships.

## Guardrails

- Keep layout pragmatic and readable. This is a developer tuning screen, not player-facing UX.
- Do not merge bindings UI and tuning UI.
