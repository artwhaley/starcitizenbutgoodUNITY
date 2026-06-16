# T03 - Flight Solver Rewrite

## Goal

Replace the current pseudo-damping solver with a ship-authoritative solver where all motion, including assists, is governed by the ship’s tuned acceleration and angular limits.

## Primary Files

- `Assets/Scripts/Flight/ShipFlightController.cs`
- `Assets/Scripts/Flight/ShipState.cs`
- `Assets/Scripts/Flight/ShipTuning.cs`

## Scope

- Rewrite linear motion to use local-axis acceleration authority at dry mass.
- Convert dry-mass acceleration tuning into actual runtime acceleration based on current mass.
- Apply asymmetric per-axis acceleration.
- Enforce max linear speed.
- Enforce per-axis angular speed caps.
- Rewrite rotational motion using tuned angular acceleration and angular speed limits.
- Rewrite assist behavior so stabilization uses requested counter-thrust and counter-torque rather than direct velocity lerp/damping.
- Make brake mode request maximum allowed counter-thrust rather than magical decay.
- Preserve and expose applied output telemetry for later VFX/fuel tickets.

## Acceptance Criteria

- No `Mathf.Lerp` or exponential drag remains as the source of ship stabilization behavior for in-space motion.
- Releasing controls in assist modes causes the ship to stabilize only as fast as its tuned thruster authority allows.
- If a ship has weak reverse or weak yaw authority, braking or yaw stabilization is correspondingly weak.
- Linear and angular caps are respected.
- The solver still updates `ShipState.position` and `ShipState.rotation` consistently in play mode.

## Guardrails

- Do not add environmental drag.
- Do not solve fuel burn here beyond exposing applied output.
- If a safety hard clamp is needed after integration, keep it as a final guard, not the primary feel mechanic.
