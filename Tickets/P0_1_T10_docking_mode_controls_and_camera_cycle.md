# P0.1 T10 - Docking Mode Controls And Camera Cycle

## Goal

Make docking mode feel like the pilot is flying from the docking camera.

When docking mode is active:

- control inputs are interpreted relative to the docking camera/node frame
- main engines are disabled
- all translation uses RCS/fine-control style authority
- `V` cycles between docking camera and external camera, not cockpit and external

Docking remains manual. No autopilot.

## Reading Instructions

Read these files before editing:

- `Tickets/P0_1_T09_manual_docking_nodes_camera_and_hud.md`
- `Assets/Scripts/Docking/ShipDockingNode.cs`
- `Assets/Scripts/Docking/DockingPortDeploymentState.cs`
- `Assets/Scripts/Docking/DockingHud.cs`
- `Assets/Scripts/Docking/DockingTelemetry.cs`
- `Assets/Scripts/Camera/ShipCameraController.cs`
- `Assets/Scripts/Flight/PlayerShipController.cs`
- `Assets/Scripts/Flight/ShipPresentationController.cs` if it exists
- `Assets/Scripts/Flight/ShipInputCommand.cs`
- `Assets/Scripts/Flight/ShipControlRequest.cs`
- `Assets/Scripts/Flight/ShipFlightController.cs`
- `Assets/Scripts/Input/ShipInputReader.cs`

Do not start until T09 is complete.

## Required Files

Create:

- `Assets/Scripts/Docking/DockingModeController.cs`
- `Assets/Scripts/Docking/DockingInputTransformer.cs`

Modify:

- `Assets/Scripts/Camera/ShipCameraController.cs`
- `Assets/Scripts/Flight/PlayerShipController.cs` or local input submission path
- `Assets/Scripts/Flight/ShipFlightController.cs` only if a small support hook is needed

## Docking Mode State

`DockingModeController` owns local docking view/control mode:

- `bool IsDockingModeActive`
- `bool IsDockingExternalViewActive`
- reference to `ShipDockingNode` and its deployment state
- references to `ShipDockingNode`, `DockingTargetProvider`, `DockingHud`, and `ShipCameraController`

This is a local pilot mode, not the final docked state.

Do not confuse:

- docking mode = pilot is using docking camera/manual RCS mapping
- ship docking node deployed = ship port is physically/visually available for capture
- docked state = ship has physically captured and is attached to port

Docking camera mode and port deployment are separate states.

## Camera Cycle Behavior

When not in docking mode:

- `V` keeps existing cockpit/external behavior.

When in docking mode:

- `V` toggles between docking camera and external camera.
- It must not switch to cockpit camera while docking mode is active.

When exiting docking mode:

- restore normal cockpit/external cycle behavior.

## Input Transformation

Create `DockingInputTransformer`.

It should transform a normal `ShipInputCommand` into a docking-relative command.

Requirements:

- Pilot input should feel like cockpit flight from the docking camera.
- Docking camera/node forward becomes the pilot's effective forward axis.
- Docking camera/node right becomes effective right strafe.
- Docking camera/node up becomes effective up strafe.
- Pitch/yaw/roll should also be relative to docking view as much as the current solver supports.
- Main engine forward thrust is disabled.
- Forward/backward docking movement uses maneuver/RCS authority, not main engine authority.
- Output command should force `fineControl = true`.
- Output command should force `boost = false`.

Implementation guidance:

Current solver accepts local ship axes, so convert desired docking-frame world movement into ship-local axes:

```text
input axes -> desired world linear vector in docking node frame
desired world linear vector -> ship local vector using inverse ship rotation
ship local vector -> ShipInputCommand thrustRight/thrustUp/thrustForward
```

For angular input, do the same conceptually:

```text
docking-frame desired angular vector -> ship local angular vector
```

Map the resulting local angular vector into pitch/yaw/roll.

Keep signs intuitive during playtest. If signs are ambiguous, choose the mapping that makes pressing right move the docking HUD target left/right in the expected pilot-facing way, then document it.

## Main Engine Disable Requirement

In docking mode, the ship must not use main engines.

Preferred implementation:

- Transformed command sets `fineControl = true`, and current solver uses maneuver forward in fine control.

If the current solver still lights or uses main engine in docking mode, add an explicit docking-mode flag or request path to force maneuver-only forward thrust.

Do not retune the ship globally.

## HUD

Docking HUD from T09 remains active in docking mode.

External view while docking mode is active may hide the HUD or keep a minimal docking target indicator. Choose the simpler behavior:

- HUD visible only when docking camera is the active camera.

## Acceptance Criteria

- Project compiles:

```powershell
dotnet build Assembly-CSharp.csproj --no-restore
```

- Pressing `H` toggles docking mode.
- In docking mode, main camera uses docking camera unless external view is selected.
- In docking mode, pressing `V` toggles docking camera/external only.
- In docking mode, controls are relative to the docking node/camera frame.
- In docking mode, main engines are disabled and forward movement uses fine-control/maneuver authority.
- Docking camera mode does not automatically deploy the ship docking node.
- Ship docking node deployment does not automatically enter docking camera mode.
- Exiting docking mode restores normal controls and normal `V` behavior.
- No autopilot is added.
- No magnetic capture is added.
- No final docked state is added.

## Guardrails

- Manual docking only.
- Do not add automatic input sources.
- Do not add `ShipAutopilotRequestSource` docking behavior.
- Do not change normal flight controls outside docking mode.
- Do not alter global tuning assets.
- Do not implement capture/snap.
- Do not add networking.
