# T02 - Control Request Pipeline

## Goal

Separate “who is asking the ship to do something” from “what the ship can actually apply,” so pilot input, assist logic, brake logic, and future autodock can all feed the same solver.

## Primary Files

- `Assets/Scripts/Flight/ShipInputCommand.cs`
- `Assets/Scripts/Input/ShipInputReader.cs`
- `Assets/Scripts/Input/KeyboardMouseInputProvider.cs`
- `Assets/Scripts/Input/JoystickInputProvider.cs`
- `Assets/Scripts/Flight/ShipFlightController.cs`

## Scope

- Introduce a new request structure for solver input.
- Keep the current raw player-facing input structure if it is still useful, but do not let it remain the only command object.
- Support at least these request contributors:
  - pilot
  - assist
  - brake
  - future autopilot/autodock hook
- Define a clear merge point where requests become one final requested local linear/angular command.
- Add a placeholder or interface boundary for future non-player command sources.

## Acceptance Criteria

- `ShipFlightController` does not have to infer every behavior directly from raw player input alone.
- There is a clear location in code where a future autodock feature could submit local linear and angular requests without refactoring the solver again.
- The request pipeline supports boost and fine-control flags.
- Existing keyboard and joystick input still build into the pipeline cleanly.

## Guardrails

- Do not implement autodock here. Only leave the hook.
- Keep the abstraction small. We need a clear request path, not a framework.
- Preserve current fire/brake/boost behavior while changing the pipeline shape.
