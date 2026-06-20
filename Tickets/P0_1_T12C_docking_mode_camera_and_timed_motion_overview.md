# P0.1 T12C - Docking Mode, Docking Camera, And Timed Capture Motion

## Goal

Refine manual docking so docking mode, docking camera, and docking-relative controls are separate concepts.

The player should be able to dock from cockpit, external, or docking camera as long as docking mode is enabled. The docking camera and docking-relative controls are available only while docking mode is enabled, and docking-relative controls apply only while the active camera is the docking camera.

Also replace instant-looking magnetic capture and undock hops with short timed motions that read as visible ship movement.

## Stack

Execute in order:

1. `P0_1_T12C_1_decouple_docking_mode_camera_and_controls.md`
2. `P0_1_T12C_2_capture_eligibility_and_auto_mode_off.md`
3. `P0_1_T12C_3_timed_magnetic_capture.md`
4. `P0_1_T12C_4_timed_undock_separation.md`
5. `P0_1_T12C_5_docking_playtest_and_regression_checks.md`

## Global Behavioral Decisions

- `J` toggles docking mode.
- `V` cycles camera views.
- `U` undocks when docked.
- Docking mode controls capture permission.
- Docking camera is only available while docking mode is on.
- Docking HUD appears only while the active view is docking camera.
- Docking-relative input transform applies only while the active view is docking camera.
- Capture can begin from cockpit, external, or docking camera if docking mode is on.
- Capture cannot begin if docking mode is off.
- Docking mode automatically turns off when the ship reaches `Docked`.
- Docking mode is not required to undock.
- Roll around the docking axis must not be required for capture.
- Magnetic capture should visibly pull the ship in over about `0.5s` by default.
- Undock should visibly separate the docking node from the port over several frames, then release the ship with a small outward drift.

## Guardrails

- Do not add autopilot.
- Do not make docking require the docking camera view.
- Do not make docking-relative controls apply in cockpit or external view.
- Do not make `U` require docking mode.
- Do not create fake docking nodes, fake station ports, fake hardpoints, or fake prefab setup.
- Do not use Unity physics joints.
- Do not merge ship and station into one object.
- Do not replace authored transform contracts with generated defaults.
- Preserve server-authoritative direction: route state changes through docking/authority components, not UI-only transform writes.

