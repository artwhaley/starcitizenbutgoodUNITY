# T04 - Boost And Fine Control Modes

## Goal

Implement distinct ship modes for normal flight, boost, and fine control, each using the new solver limits and correct thruster families.

## Primary Files

- `Assets/Scripts/Flight/ShipFlightController.cs`
- `Assets/Scripts/Flight/ShipState.cs`
- `Assets/Scripts/Flight/ShipTuning.cs`
- `Assets/Scripts/Input/KeyboardMouseInputProvider.cs`
- `Assets/Scripts/Input/ShipInputReader.cs`
- `Assets/Settings/InputProfiles/FlightInputActions.inputactions`
- `README.md`

## Scope

- Keep boost as a held mode.
- Boost raises:
  - linear speed cap
  - forward acceleration
  - modest rotational speed or rotational response
- Add a toggle keybind for fine control mode.
- Fine control mode:
  - disables normal main-engine forward thrust for routine forward movement
  - uses maneuvering thruster authority instead
  - applies lower tuned linear caps
  - applies lower tuned speed caps
  - optionally applies lower angular caps if the tuning schema supports it
- Update README controls table to document the fine-control keybind.

## Acceptance Criteria

- Boost and fine control are both visible runtime states, not inferred hacks.
- Normal forward thrust uses the main engine path.
- Fine-control forward thrust does not use the normal main-engine path.
- Fine control can be toggled on and off during play.
- The solver respects the correct caps for the active mode.

## Guardrails

- Do not let fine-control mode become a second solver. It should reuse the same solver with different authority/cap inputs.
- Choose a keybind that does not collide with current documented controls.
