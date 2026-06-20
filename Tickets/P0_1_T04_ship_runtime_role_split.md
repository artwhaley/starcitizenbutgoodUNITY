# P0.1 T04 - Ship Runtime Role Split

## Goal

Reduce `PlayerShipController` from a god object into a small local-player coordinator.

After this ticket, responsibilities should be clearer:

- input submission belongs to the local player controller
- authoritative simulation belongs to `LocalGameAuthority`
- transform/camera/HUD/VFX/audio presentation reads state and updates visuals
- runtime dependency repair should be reduced, not expanded

Gameplay must feel unchanged.

## Reading Instructions

Read these files before editing:

- `Tickets/P0_1_T01_entity_identity_and_registry.md`
- `Tickets/P0_1_T02_route_flight_through_local_authority.md`
- `Tickets/P0_1_T03_authority_owned_fixed_tick.md`
- `Assets/Scripts/Flight/PlayerShipController.cs`
- `Assets/Scripts/Flight/ShipFlightController.cs`
- `Assets/Scripts/Camera/ShipCameraController.cs`
- `Assets/Scripts/Debug/FlightDebugHud.cs`
- `Assets/Scripts/Audio/ShipAudioHooks.cs`
- `Assets/Scripts/Flight/RcsThrusterVfx.cs`
- `Assets/Scripts/Flight/EngineGlowVfx.cs`
- `Assets/Scripts/Input/ShipInputReader.cs`
- `Assets/Scripts/Weapons/PrimaryWeaponController.cs`
- `Assets/Scripts/Authority/LocalGameAuthority.cs`

Do not start until T02 and T03 are complete.

## Required Design

Create one new component:

- `Assets/Scripts/Flight/ShipPresentationController.cs`

`ShipPresentationController` is responsible for applying ship state to Unity presentation:

- set COG transform from `ShipFlightController.State`
- update camera pan/tilt/zoom
- update HUD telemetry and world markers
- update audio

`PlayerShipController` remains responsible for:

- local input callbacks
- submitting input to authority
- toggling assist/camera/bindings panel
- coordinating local-only player behavior

Do not create more than one new component in this ticket.

## Implementation Instructions

### 1. Create `ShipPresentationController`

Fields should include the presentation references currently used by `PlayerShipController`:

- `Transform cogTransform`
- `ShipFlightController flight`
- `ShipInputReader input`
- `FlightDebugHud hud`
- `ShipCameraController cameraController`
- `PrimaryWeaponController weapon`
- `ShipAudioHooks audioHooks`

Methods:

- `ApplySimulationState()` for setting COG transform from `flight.State`
- `TickPresentation(in ShipInputCommand command, float deltaTime)` for camera/HUD/audio presentation

Keep behavior equivalent to current `PlayerShipController.Update()` and post-sim transform application.

### 2. Update `PlayerShipController`

Add serialized field:

- `ShipPresentationController presentation`

Move presentation logic out of `PlayerShipController`:

- COG `SetPositionAndRotation`
- camera pan/tilt/zoom calls
- audio hook update
- HUD telemetry/world marker update

`PlayerShipController.FixedUpdate()` should:

- submit input to authority
- call authority tick
- call `presentation.ApplySimulationState()`

`PlayerShipController.Update()` should:

- resolve local command
- call `weapon.Tick(command.firePrimary)` for now
- call `presentation.TickPresentation(command, Time.deltaTime)`

### 3. Keep Visual Wiring Stable

Do not fully rewrite `WireVisualSubsystems()` in this ticket.

You may update it only enough to wire the new `ShipPresentationController` reference if needed.

Prefab hygiene is T05.

## Acceptance Criteria

- Project compiles:

```powershell
dotnet build Assembly-CSharp.csproj --no-restore
```

- Current single-player flight, camera, HUD, audio, VFX, and firing behavior are preserved.
- `PlayerShipController` no longer directly updates HUD/audio/camera/COG transform except through `ShipPresentationController`.
- `ShipPresentationController` contains presentation update logic.
- No networking package is added.
- No docking or asteroid code is added.

## Guardrails

- Do not create a large framework.
- Do not split into many new components yet.
- Do not remove the tuning overlay or bindings panel behavior.
- Do not change input bindings.
- Do not rewrite VFX systems.
- Do not change authority behavior beyond calling presentation after simulation.

