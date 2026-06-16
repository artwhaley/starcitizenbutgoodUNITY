# T06 - Thruster Output And VFX Contract

## Goal

Drive engine and RCS effects from applied ship output, with ship-swappable assets and explicit support for boost/fine-control presentation.

## Primary Files

- `Assets/Scripts/Flight/RcsThrusterVfx.cs`
- `Assets/Scripts/Flight/EngineGlowVfx.cs`
- `Assets/Scripts/Flight/ParticleVfxUtility.cs`
- `Assets/Scripts/Flight/ShipVisualReferences.cs`
- `Assets/Scripts/Flight/ShipFlightController.cs`

## Scope

- Define a clean applied-thruster telemetry contract from the solver to VFX.
- Keep ship-specific particle systems/materials abstractable behind shared logic.
- Normal forward thrust:
  - main engine visuals active
  - maneuvering forward jets do not fake a normal forward push
- Boost:
  - main engines intensify
  - rear-facing maneuvering jets may contribute visually and logically
  - RCS color shifts from blue to yellow
- Fine control:
  - main engines stay out of the path for routine forward nudging
  - maneuvering jets represent the motion source
- Assist-generated and brake-generated thrust must also trigger the correct RCS visuals.

## Acceptance Criteria

- VFX reflect applied output, not merely raw player input.
- A ship with different prefabs/materials can still use the same solver/VFX contract.
- Boost visibly changes RCS presentation to yellow.
- Fine-control forward movement does not look like full main-engine flight.

## Guardrails

- Keep matching logic data-driven enough that later ships can reuse it.
- Avoid duplicating solver logic inside VFX code.
