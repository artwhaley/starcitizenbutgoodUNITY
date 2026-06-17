# U-Wing Flight Model Manual Playtest

Run in `FlightTest.unity` with the debug HUD visible.

## Normal Forward Acceleration

1. Assist off, fine control off, boost off.
2. Hold W until speed stabilizes near max linear speed.
3. Expect: speed rises smoothly, HUD `APL LIN` Z tracks request, main engine VFX active, fuel decreases.

## Reverse / Strafe Authority

1. From a stop, tap S, D, and E separately.
2. Expect: reverse/strafe motion matches tuned accel; hypergolic decreases; no main-engine plume on pure strafe.

## Angular Caps

1. Hold pitch/yaw/roll inputs.
2. Expect: angular rates plateau at tuned caps; HUD angular rates stay within max pitch/yaw/roll values.

## Assist Stabilization

1. Cycle assist with F, build drift on one axis, release input.
2. Expect: ship settles via counter-thrust only; HUD shows assist-generated `REQ LIN` / `REQ ANG` without instant velocity snaps.

## Brake

1. Build speed, hold Ctrl.
2. Expect: counter-thrust on all axes, RCS/brake puffs, hypergolic burn, no magical velocity erase.

## Boost

1. Hold Shift + W at cruise.
2. Expect: higher speed cap, stronger forward accel, yellow RCS, brighter engine plume, faster fuel burn.

## Fine Control

1. Toggle G, use W/A/D/E/Q for small movements.
2. Expect: maneuver thrusters/RCS drive motion, main engines stay dark for routine forward nudging, lower speed cap.

## Propellant Burn

1. Watch HUD fuel/hypergolic during main-engine cruise vs RCS-only taps.
2. Expect: sustained main thrust burns fuel; small RCS taps burn less hypergolic; cap-blocked thrust shows reduced `APL` and minimal burn.

## VFX Transitions

1. Toggle fine control and boost while observing engine plume and RCS color.
2. Expect: main plume only for normal forward thrust; boost shifts RCS to yellow; fine-control forward uses maneuver visuals only.
