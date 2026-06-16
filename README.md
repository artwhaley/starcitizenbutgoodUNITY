# FlightModel Unity Project

## Open and play

1. Unity Hub -> open `Unity_FlightModel` with editor **6000.4.0f1**
2. **Active Input Handling** = Input System Package (New) only
3. Open **`Assets/Scenes/FlightTest.unity`**
4. Press **Play**

Do **not** use `Assets/flightmodel.unity` — that is an old scratch scene with an unwired cube.

## Scene contents

`FlightTest.unity` contains:

- `ArenaRoot/PF_TestArena` — station, debris, target, docking zone
- `PF_PlayerShip` at `(50, 2, 0)` — fully wired player prefab
- Directional Light
- EventSystem

## Controls

| Key | Action |
|---|---|
| W/S/A/D/E/Q | Thrust |
| Arrows / Mouse | Pitch / Yaw |
| Z/C | Roll |
| Shift | Boost |
| Ctrl | Brake |
| F | Cycle assist mode |
| V | Toggle cockpit / external camera |
| Space / LMB | Fire |
| \\ | Input bindings panel |
| T | Tuning overlay |

## Input asset

Keyboard bindings live in `Assets/Settings/InputProfiles/FlightInputActions.inputactions`.

`PF_PlayerShip` references that asset on `ShipInputReader`.

## U-wing art (later)

Import FBX to `Assets/Art/Ships/UWing/Source/`, replace cube under player ship, run **FlightModel -> Auto-Wire Ship References**.
