# Flight Tuning Model

This project has two separate tuning domains. They should stay separate.

## Global Input Bindings

Global input settings describe the pilot hardware, not the ship.

- Device selection
- Axis selection
- Button selection
- Axis inversion
- Axis deadzone
- Axis exponent
- Axis gain

These settings live in the input bindings JSON and should apply no matter what ship the player is flying. If a joystick axis is noisy, inverted, too sensitive near center, or physically mapped differently than another pilot's hardware, this is the right layer to fix it.

The binding panel is a hardware calibration and assignment panel. It should not contain ship performance numbers.

## Per-Ship Tuning

Per-ship tuning describes the flight character of a specific hull.

The current `ShipTuning` ScriptableObject is the correct place for this. Each flyable ship prefab should eventually reference its own `ShipTuning` asset.

For the current U-wing prototype, the tuning asset should represent that ship's first-pass flight feel. Later ships should get separate assets instead of sharing one global flight profile.

Per-ship tuning should include:

- Mass
- Maximum linear thrust on local right/up/forward axes
- Maximum rotational torque on pitch/yaw/roll axes
- Boost multiplier
- Attitude assist strength
- Brake linear damping
- Brake angular damping
- Coupled lateral damping
- Frame-lock damping
- Future per-ship caps such as max safe speed, heat, fuel, capacitor, weapon hardpoint behavior, and damage modifiers

## Runtime Tuning

Runtime tuning exists to iterate quickly during playtests.

The tuning overlay should edit the active ship's runtime copy, not mutate the source asset every frame. When a profile is saved, it should save an override using a ship/profile key, not one global `ship_tuning_override.json` for every ship forever.

Short term:

- Keep U-wing as the only active ship profile.
- Expand the tuning overlay so it exposes every field in `ShipTuning`, with labels and numeric entry.
- Save U-wing tuning overrides under a ship-specific filename (`ship_tuning_<profile>.json` in persistent data).

Medium term:

- Add a `ShipDefinition` asset or component that links ship prefab, visual prefab, tuning asset, camera mounts, hardpoints, RCS markers, engine markers, display name, and ship id.
- Spawn ships from `ShipDefinition` instead of scene-specific object lookups.
- Store player-selected tuning overrides per ship id.

## Rule of Thumb

If the setting answers "how does my physical controller report input?", it belongs in global input bindings.

If the setting answers "how does this ship accelerate, rotate, brake, assist, or feel?", it belongs in per-ship tuning.
