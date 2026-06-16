# Flight Model Rework Ticket Stack

This ticket stack replaces the current simplified flight model with a ship-authoritative model built around:

- per-ship dry-mass acceleration tuning
- asymmetric linear and angular authority
- real speed and angular speed caps
- boost and fine-control modes
- shared thruster-authority math for pilot, assist, and future autopilot sources
- propellant burn based on actual applied thrust
- VFX driven by applied ship output rather than raw input

## Execution Order

1. `T01_ship_tuning_schema_and_runtime_state.md`
2. `T02_control_request_pipeline.md`
3. `T03_flight_solver_rewrite.md`
4. `T04_boost_and_fine_control_modes.md`
5. `T05_propellant_and_mass_accounting.md`
6. `T06_thruster_output_and_vfx_contract.md`
7. `T07_ship_tuning_overlay_expansion.md`
8. `T08_debug_hud_and_manual_playtest.md`

## Guardrails

- Do not reintroduce fake drag or fake damping. All automatic stabilization must request counter-thrust through the same ship limits used by pilot input.
- Keep global joystick bindings separate from per-ship tuning. Hardware gain/deadzone/exponent stays in bindings JSON, never in `ShipTuning`.
- Preserve current playability after each ticket. Compile and smoke-test after every ticket.
- Record applied output separately from requested output so debug HUD, fuel burn, and VFX can reflect what the ship actually did.
- Keep ship-facing VFX abstracted from the solver so later ships can swap prefabs/materials while using the same logic.

## Current Anchor Files

- `Assets/Scripts/Flight/ShipTuning.cs`
- `Assets/Scripts/Flight/ShipState.cs`
- `Assets/Scripts/Flight/ShipInputCommand.cs`
- `Assets/Scripts/Flight/ShipFlightController.cs`
- `Assets/Scripts/Input/ShipInputReader.cs`
- `Assets/Scripts/Input/KeyboardMouseInputProvider.cs`
- `Assets/Scripts/Flight/RcsThrusterVfx.cs`
- `Assets/Scripts/Flight/EngineGlowVfx.cs`
- `Assets/Scripts/UI/FlightTuningOverlay.cs`
- `Assets/Scripts/Debug/FlightDebugHud.cs`

## Notes

- The current project only flies the U-wing, but the new model must be ship-safe from day one.
- `Docs/flight_tuning_model.md` is the high-level spec. These tickets turn it into implementation work.
