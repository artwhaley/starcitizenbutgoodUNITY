# P0.1 T12 - Manual Undock And Recapture Lockout

## Goal

Implement undocking from a docked ship and prevent immediate magnetic recapture.

When the player undocks:

- docked state ends
- magnetic capture is disabled
- ship docking node remains deployed unless the player toggles it separately
- ship separates from the port safely
- capture remains disabled until the ship docking node has moved at least `1.0m` away from the port

## Reading Instructions

Read these files before editing:

- `Tickets/P0_1_T09_manual_docking_nodes_camera_and_hud.md`
- `Tickets/P0_1_T10_docking_mode_controls_and_camera_cycle.md`
- `Tickets/P0_1_T11_manual_magnetic_capture_and_docked_state.md`
- `Assets/Scripts/Docking/DockingState.cs`
- `Assets/Scripts/Docking/DockableShip.cs`
- `Assets/Scripts/Docking/DockingCaptureController.cs`
- `Assets/Scripts/Docking/DockingModeController.cs`
- `Assets/Scripts/Docking/StationDockingPort.cs`
- `Assets/Scripts/Flight/ShipFlightController.cs`
- `Assets/Scripts/Authority/LocalGameAuthority.cs`
- `Assets/Scripts/Input/ShipInputReader.cs`
- `Assets/Scripts/Input/KeyboardMouseInputProvider.cs`
- `Assets/Settings/InputProfiles/FlightInputActions.inputactions`

Do not start until T11 is complete.

## Input

Add one input action:

- `Undock`

Default keyboard binding:

- `U`

If the ship is not docked, pressing `U` does nothing.

## Required Behavior

When `Undock` is pressed while `DockingState.Docked`:

1. Clear docked port/station attachment state.
2. Disable magnetic capture immediately.
3. Move the ship to a safe undock pose from the port's current attach transform.
4. Set a small separation velocity away from the station port.
5. Enter `RecaptureLockout`.
6. Re-enable manual flight controls.

## Safe Undock Pose

Use station port forward to define the separation direction.

Because the ship docking node faces opposite the station port while docked:

- moving away from the station should move the ship along the station port's outward direction.
- use the port's current `ShipAttachTransform`, not a cached station-root pose

Configurable values:

- `float undockOffsetMeters = 1.25f`
- `float undockVelocityMetersPerSecond = 0.5f`
- `float recaptureUnlockDistanceMeters = 1.0f`

Set the ship pose so the docking node is at least `undockOffsetMeters` away from the port.

## Future Animated Port Compatibility

This ticket does not implement animated extend/release, but it must not block it.

Structure undock so a future implementation can do:

```text
docked ship follows moving attach transform
-> port plays extend animation
-> release at final extended attach transform
-> apply outward velocity
-> recapture lockout
```

For phase 0.1, releasing immediately from the current attach transform is acceptable.

Do not bake assumptions that the port never moves while docked.

## Recapture Lockout

While in `RecaptureLockout`:

- magnetic capture cannot activate
- HUD should show lockout state if docking HUD is visible
- normal manual flight is allowed

Exit lockout only after:

- distance between ship docking node and last docked station port is at least `recaptureUnlockDistanceMeters`

Default:

- `1.0m`

After lockout exits:

- state becomes `FreeFlight` or `DockingMode` depending on whether docking mode is still active
- capture may activate again if the player deliberately re-enters the envelope

## Camera Behavior

On undock:

- Keep docking mode active by default if it was active.
- The pilot may press `H` to exit docking mode.
- `V` behavior from T10 remains unchanged.

Do not force cockpit view unless necessary.

Do not automatically retract the ship docking node on undock in phase 0.1. Retraction is a separate pilot command and later may play its own animation.

## Authority

For local single-player:

- `LocalGameAuthority` or the docking capture controller may own the undock state transition.

Prepare the code so later server authority can own the same transition:

- do not let UI directly set transform only
- route through a method like `RequestUndock()` on `DockableShip` or docking authority/controller

Do not add networking yet.

## Acceptance Criteria

- Project compiles:

```powershell
dotnet build Assembly-CSharp.csproj --no-restore
```

- Player can dock through T11 behavior.
- Pressing `U` while docked undocks the ship.
- Ship is moved safely off the port's current attach transform.
- Ship receives a small outward velocity.
- Magnetic capture does not immediately recapture the ship.
- Recapture remains disabled until the docking node has moved at least `1.0m` from the last port.
- After moving away, capture can work again if the pilot manually returns to the port.
- Undock does not automatically retract the ship docking node.
- No autopilot is added.
- No station trading/interior UI is added.
- No networking is added.

## Guardrails

- Do not use physics joints.
- Do not merge ship/station entities.
- Do not force automatic approach after undock.
- Do not disable manual controls during lockout.
- Do not permanently disable capture after undock.
- Do not add persistence or economy systems.
