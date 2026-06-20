# P0.1 Roadmap Ticket Index

This index covers the road from the current architecture cleanup through the first friend login/fly/fight test.

## Execution Order

1. `P0_1_T01_entity_identity_and_registry.md`
2. `P0_1_T02_route_flight_through_local_authority.md`
3. `P0_1_T03_authority_owned_fixed_tick.md`
4. `P0_1_T04_ship_runtime_role_split.md`
5. `P0_1_T05_prefab_hygiene_and_explicit_wiring.md`
6. `P0_1_T06_ship_collision_proxy_and_physics_layers.md`
7. `P0_1_T07_simulation_extraction_and_tests.md`
8. `P0_1_T08_reference_frame_ids.md`
9. `P0_1_T09_manual_docking_nodes_camera_and_hud.md`
10. `P0_1_T10_docking_mode_controls_and_camera_cycle.md`
11. `P0_1_T11_manual_magnetic_capture_and_docked_state.md`
12. `P0_1_T12_manual_undock_and_recapture_lockout.md`
13. `P0_1_T12A_custom_collision_query_and_bounce.md`
14. `P0_1_T12B_vehicle_possession_refactor_overview.md`
15. `P0_1_T12B_1_vehicle_and_pilot_seat_shell.md`
16. `P0_1_T12B_2_local_player_vehicle_controller_input_events.md`
17. `P0_1_T12B_3_authority_submission_and_presentation_ownership.md`
18. `P0_1_T12B_4_shrink_or_remove_player_ship_controller.md`
19. `P0_1_T12B_5_idle_and_docked_activation_modes.md`
20. `P0_1_T12C_docking_mode_camera_and_timed_motion_overview.md`
21. `P0_1_T12C_1_decouple_docking_mode_camera_and_controls.md`
22. `P0_1_T12C_2_capture_eligibility_and_auto_mode_off.md`
23. `P0_1_T12C_3_timed_magnetic_capture.md`
24. `P0_1_T12C_4_timed_undock_separation.md`
25. `P0_1_T12C_5_docking_playtest_and_regression_checks.md`
26. `P0_1_T13_asteroid_sector_descriptors.md`
27. `P0_1_T14_instanced_asteroid_scenery.md`
28. `P0_1_T15_asteroid_promotion_pool.md`
29. `P0_1_T16_mineable_resource_stub.md`
30. `P0_1_T17_snapshot_and_event_dtos.md`
31. `P0_1_T18_first_networking_integration.md`
32. `P0_1_T19_two_ship_friend_test.md`
33. `P0_1_T20_docking_and_asteroid_replication.md`

## Notes

- T12A is inserted before asteroid/network work so collision response is explicit before promoted asteroids become collidable.
- T12B is split into five sub-tickets before asteroid/network work so local player control, ship vehicle identity, and pilot-seat possession are not fused before multiplayer and station interiors.
- T12C is split into five sub-tickets before asteroid/network work so docking mode, docking camera, capture permission, magnetic pull, and undock separation are clean manual systems before multiplayer replication.
- T13-T16 build the deterministic asteroid loop without networking.
- T17 creates package-agnostic network data contracts.
- T18 chooses NGO and introduces server-owned ship replication.
- T19 is the first fly/fight friend test.
- T20 ties docking and asteroid state into the shared multiplayer loop.

## Global Guardrails

- Server/local authority owns gameplay state.
- Ship movement remains custom simulation, not Rigidbody-driven.
- Unity physics queries may be used locally, but core simulation should not directly depend on `UnityEngine.Physics`.
- Far asteroid scenery must not become networked GameObject spam.
- Docking remains manual.
- Avoid adding economy, persistence, station interiors, or account systems before the first 0.1 friend test.
