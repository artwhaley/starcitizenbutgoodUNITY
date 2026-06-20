# P0.1 T09 - Manual Docking Nodes Camera And HUD

## Goal

Add the manual docking view foundation:

- ship docking node
- station docking port
- docking camera located at the ship docking node
- docking HUD guidance elements
- key to enter/exit docking view mode

This ticket does not implement magnetic capture, docking state changes, or undocking. It gives the pilot the correct manual visual instrumentation first.

## Behavioral Requirement

Docking is manual. Do not implement autopilot, AI takeover, path following, or automatic approach in this ticket.

The pilot must be able to switch to a docking camera located at the ship's docking node, looking outward along the node's authored docking axis. The HUD must show relative alignment, lateral offset, lateral velocity, roll offset, vertical offset, and closure velocity against a selected station docking port.

## Reading Instructions

Read these files before editing:

- `Assets/Scripts/Flight/BlenderImportedAxes.cs`
- `Assets/Scripts/Camera/ShipCameraController.cs`
- `Assets/Scripts/Debug/FlightDebugHud.cs`
- `Assets/Scripts/Flight/PlayerShipController.cs`
- `Assets/Scripts/Flight/ShipPresentationController.cs` if it exists
- `Assets/Scripts/Input/ShipInputReader.cs`
- `Assets/Scripts/Input/KeyboardMouseInputProvider.cs`
- `Assets/Scripts/Gameplay/DockingZone.cs`
- `Assets/Prefabs/Ships/PF_PlayerShip.prefab`
- `Assets/Prefabs/Arena/PF_TestArena.prefab`
- `Assets/Scenes/FlightTest.unity`

If T04 has not introduced `ShipPresentationController`, keep integration in `PlayerShipController`/`ShipCameraController` but do not expand responsibilities beyond this ticket.

## Required Files

Create:

- `Assets/Scripts/Docking/ShipDockingNode.cs`
- `Assets/Scripts/Docking/StationDockingPort.cs`
- `Assets/Scripts/Docking/DockingPortClass.cs`
- `Assets/Scripts/Docking/DockingPortDeploymentState.cs`
- `Assets/Scripts/Docking/DockingTargetProvider.cs`
- `Assets/Scripts/Docking/DockingHud.cs`
- `Assets/Scripts/Docking/DockingTelemetry.cs`

Modify:

- `Assets/Scripts/Camera/ShipCameraController.cs`
- `Assets/Scripts/Flight/PlayerShipController.cs` or `ShipPresentationController` if present
- `Assets/Scripts/Input/ShipInputReader.cs`
- `Assets/Scripts/Input/KeyboardMouseInputProvider.cs`
- `Assets/Settings/InputProfiles/FlightInputActions.inputactions`
- `Assets/Prefabs/Ships/PF_PlayerShip.prefab`
- `Assets/Prefabs/Arena/PF_TestArena.prefab`
- `Assets/Scenes/FlightTest.unity`

## Input

Add one input action:

- `ToggleDockingMode`
- `ToggleDockingPortDeploy`

Default keyboard binding:

- `H`
- `J`

Do not overload `V`. `V` remains camera-cycle behavior and will be adjusted in T10.

`H` toggles docking camera/control mode.

`J` toggles the ship docking node deployed/retracted state.

These are intentionally separate. A pilot can deploy the docking port without switching camera, or switch camera and see that the port is still retracted.

## Docking Node And Port

### `ShipDockingNode`

MonoBehaviour on or under the player ship visual/COG hierarchy.

Fields:

- `Transform node`
- `Vector3 actionAxisLocal`, default `BlenderImportedAxes.DefaultActionAxisLocal`
- `bool startsDeployed`, default `false`
- runtime `DockingPortDeploymentState`

Properties/methods:

- `Vector3 WorldPosition`
- `Vector3 WorldForward`
- `Quaternion WorldRotation`
- `bool IsDeployed`
- `bool IsDockingActive`
- `void SetDeployed(bool deployed)`
- `void ToggleDeployed()`

Use:

```csharp
BlenderImportedAxes.GetWorldActionDirection(node, actionAxisLocal)
```

Do not hardcode Unity `Vector3.forward`, `Vector3.up`, or `Vector3.down` as the docking direction. This project already has Blender import axis handling.

Ship docking nodes are not always active. Most ships will eventually hide/retract their docking port by animation. For phase 0.1, deployment can be instant, but the state must exist now.

`IsDockingActive` should be true only when the node is deployed and usable for docking/capture.

### `StationDockingPort`

MonoBehaviour on a station docking port marker.

Fields:

- `string portId`
- `DockingPortClass portClass`
- `Transform node`
- `Transform shipAttachTransform`
- `Vector3 actionAxisLocal`, default `BlenderImportedAxes.DefaultActionAxisLocal`
- `bool startsAvailable`, default `true`
- runtime `DockingPortDeploymentState` or equivalent availability state
- configurable HUD/capture metadata may be added later, but do not implement capture here

Properties/methods:

- `Vector3 WorldPosition`
- `Vector3 WorldForward`
- `Quaternion WorldRotation`
- `Transform ShipAttachTransform`
- `bool IsAvailable`
- `bool IsDockingActive`
- `void SetAvailable(bool available)`

The port's `WorldForward` is the direction the port faces outward. For correct final alignment, the ship docking node forward should face opposite this direction.

`shipAttachTransform` is important for future animated docking ports. For phase 0.1 it may be the same transform as `node`. Later, an animated port can move `shipAttachTransform` during capture/retract/extend animations, and the docked ship can follow that transform without changing the docking state model.

Do not hardcode docking to the station root transform.

### `DockingPortClass`

Create an enum:

```csharp
namespace FlightModel.Docking
{
    public enum DockingPortClass
    {
        Unknown = 0,
        SmallShip = 1,
        MediumShip = 2,
        LargeShip = 3,
        Cargo = 4,
        Maintenance = 5
    }
}
```

The first implementation does not need to enforce class compatibility. Add the data now so station prefabs do not need a broad migration later.

### `DockingPortDeploymentState`

Create an enum:

```csharp
namespace FlightModel.Docking
{
    public enum DockingPortDeploymentState
    {
        Retracted = 0,
        Deploying = 1,
        Deployed = 2,
        Retracting = 3,
        Disabled = 4
    }
}
```

For phase 0.1, transitions may be instant between `Retracted` and `Deployed`. The enum exists so future animation can move through `Deploying` and `Retracting` without changing the capture logic.

## Target Provider

`DockingTargetProvider` should select one current `StationDockingPort`.

For this ticket, keep it simple:

- serialized explicit target port reference preferred
- fallback to nearest active `StationDockingPort` within a configurable range, default `100m`

Do not build station traffic control, port reservations, networking, or occupancy yet.

## Docking Camera

Extend `ShipCameraController` with a docking camera mode.

Requirements:

- Docking camera position equals `ShipDockingNode.WorldPosition`.
- Docking camera forward equals `ShipDockingNode.WorldForward`.
- Docking camera uses a configurable FOV, default `70`.
- Main view switches to docking camera when docking mode is active.
- External camera remains available, but `V` behavior is finalized in T10.
- Docking camera may be entered even if the ship docking node is retracted, but HUD must indicate that capture is inactive until deployed.

Implementation option:

- Reuse the cockpit camera object and move it to the docking node while docking mode is active.
- Or add a dedicated docking camera.

Choose the smaller, safer implementation. Do not render to a dashboard display yet.

## Docking Telemetry

Create `DockingTelemetry` as a struct with at least:

- `bool hasTarget`
- `bool shipPortDeployed`
- `bool targetPortAvailable`
- `Vector2 lateralOffsetMeters`
- `Vector2 lateralVelocityMetersPerSecond`
- `float closureVelocityMetersPerSecond`
- `float rollOffsetDegrees`
- `float verticalOffsetMeters`
- `Vector2 angularAxisError`
- `float distanceMeters`

Coordinate conventions:

- All HUD offsets are expressed in the docking camera frame.
- Center mark is `(0, 0)`.
- Lateral X is docking camera right.
- Lateral Y is docking camera up.
- Closure is along docking camera forward toward the target.
- The target is correctly facing the ship when ship node forward and station port forward are opposite.

## HUD Elements

Create `DockingHud` using existing Unity UI style (`Text`, `Image`, simple rects/circles are fine).

Must show:

1. Center mark, the visual `(0,0)` reference.
2. Axis alignment indicator: a larger circle with a dot showing angular axis error.
3. Horizontal and vertical needles showing lateral offset from center.
4. Velocity bug showing relative horizontal/vertical velocity.
5. Numeric readouts:
   - relative rotation around docking Z/forward axis
   - vertical distance between nodes
   - closure velocity

Use simple graphics/text first. Do not over-polish.

HUD should be visible only in docking mode.

HUD must indicate whether the ship docking node is deployed. Simple text such as `PORT: RETRACTED` / `PORT: DEPLOYED` is enough.

## Prefab/Scene Setup

Add a ship docking node marker to `PF_PlayerShip`.

Acceptable first placement:

- near the front/lower ship body if no authored node exists
- clearly named `node_docking`

`FlightTest.unity` now contains a station GameObject named:

- `station`

Use that scene station as the first real docking target.

Find a docking marker empty under `station` and attach/configure `StationDockingPort` there.

Accepted marker names under `station`:

- `node_docking_port`
- `node_docking`
- any child transform whose name starts with `node_docking_`

If multiple matching markers exist under `station`, configure all of them as `StationDockingPort`s.

If no matching marker exists under `station`, create one child marker named `node_docking_port` in a plausible visible port location on the station model, then attach `StationDockingPort`.

Do not use the old `PF_TestArena` `StationBlock` cube as the primary docking target if the scene `station` object exists.

Only add a fallback `StationDockingPort` marker to `PF_TestArena` if the `station` GameObject is missing or unusable.

If a station model has already been imported into the scene and contains a docking marker empty, attach `StationDockingPort` to that marker instead of creating a new placeholder marker.

Accepted station marker names for auto-setup/search:

- `node_docking_port`
- `node_docking`
- any child transform whose name starts with `node_docking_`

Setup rules for imported station markers:

- add `StationDockingPort` to each marker
- set `node` to the marker transform
- set `shipAttachTransform` to the marker transform unless a child named `ship_attach` or `node_ship_attach` exists
- set `portId` from the marker name if not explicitly authored
- set `portClass = SmallShip` for the first playable port unless the name clearly says otherwise
- set station port available/deployed by default
- leave visual station hierarchy otherwise unchanged

Scene setup rule:

- The `DockingTargetProvider` in `FlightTest.unity` should prefer a `StationDockingPort` under the scene `station` object.

Do not depend on marker names at runtime once the component is attached. Name-based search is only an import/setup convenience.

## Acceptance Criteria

- Project compiles:

```powershell
dotnet build Assembly-CSharp.csproj --no-restore
```

- Pressing `H` enters/exits docking mode.
- Pressing `J` toggles ship docking node deployed/retracted state.
- Docking camera appears from the ship docking node.
- Docking camera points along the node action axis using `BlenderImportedAxes`.
- Docking HUD appears only in docking mode.
- HUD updates against a station docking port.
- HUD shows whether the ship docking node is deployed.
- The scene object named `station` in `FlightTest.unity` has at least one configured `StationDockingPort`.
- Imported station docking marker empties have `StationDockingPort` components attached and configured.
- `StationDockingPort` exposes `shipAttachTransform` so future animated ports can move the docked ship.
- Existing cockpit/external flight remains usable.
- No autopilot is added.
- No magnetic capture is added.
- No docking state transition is added.
- No networking package is added.

## Guardrails

- Manual docking only.
- Do not implement automatic approach.
- Do not change flight tuning.
- Do not disable main engines yet; that is T10.
- Do not implement snap/capture; that is T11.
- Do not implement undock lockout; that is T12.
- Do not render to an in-ship dashboard display yet.
- Do not make docking camera mode automatically deploy the port.
- Do not make deploying the port automatically enter docking camera mode.
