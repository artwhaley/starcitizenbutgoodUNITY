using System;
using UnityEngine;

namespace FlightModel
{
    public class PlayerShipController : MonoBehaviour
    {
        [SerializeField] Transform cogTransform;
        [SerializeField] ShipFlightController flight;
        [SerializeField] ShipInputReader input;
        [SerializeField] FlightDebugHud hud;
        [SerializeField] ShipCameraController cameraController;
        [SerializeField] PrimaryWeaponController weapon;
        [SerializeField] ShipTuning tuning;
        [SerializeField] ShipTuningProfileLibrary tuningLibrary;
        [SerializeField] JoystickInputProvider joystickProvider;
        [SerializeField] InputBindingsPanel bindingsPanel;
        [SerializeField] FlightTuningOverlay tuningOverlay;
        [SerializeField] RcsThrusterVfx rcsVfx;
        [SerializeField] EngineGlowVfx engineGlowVfx;
        [SerializeField] ShipAudioHooks audioHooks;
        [SerializeField] GameObject rcsPuffPrefab;
        [SerializeField] GameObject enginePlumePrefab;
        [SerializeField] Vector3 spawnPosition = new(50f, 2f, 0f);
        [SerializeField] Quaternion spawnRotation = Quaternion.identity;

        void Awake()
        {
            if (flight != null && tuning != null)
            {
                flight.Tuning = tuning;
                flight.InitializeState(spawnPosition, spawnRotation);
            }

            if (input != null && joystickProvider != null)
            {
                input.SetJoystickProvider(joystickProvider);
            }

            if (bindingsPanel != null && joystickProvider != null)
            {
                bindingsPanel.Initialize(joystickProvider);
            }

            if (tuningOverlay != null && flight != null && tuning != null)
            {
                tuningOverlay.Initialize(flight, tuning, tuningLibrary);
            }

            WireInputCallbacks();
        }

        void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            WireVisualSubsystemsSafe();
        }

        void WireVisualSubsystemsSafe()
        {
            try
            {
                rcsPuffPrefab = VfxPrefabResolver.ResolveRcsPuff(rcsPuffPrefab);
                WireVisualSubsystems();
            }
            catch (Exception exception)
            {
                Debug.LogError($"PlayerShipController: visual/VFX wiring failed: {exception.Message}", this);
            }
        }

        void WireInputCallbacks()
        {
            if (input == null)
            {
                return;
            }

            input.ToggleAssistRequested += CycleAssistMode;
            input.ToggleCameraRequested += () => cameraController?.ToggleView();
            input.ToggleBindingsPanelRequested += () => input.HandleBindingsPanelToggle(bindingsPanel);
            input.ToggleTuningOverlayRequested += () => tuningOverlay?.Toggle();
        }

        void FixedUpdate()
        {
            if (flight == null || input == null || cogTransform == null)
            {
                return;
            }

            ShipInputCommand command = input.BuildCommand();
            flight.Simulate(Time.fixedDeltaTime, command);
            ShipState state = flight.State;
            cogTransform.SetPositionAndRotation(state.position, state.rotation);
        }

        void Update()
        {
            if (input == null)
            {
                return;
            }

            ShipInputCommand command = input.LastCommand;

            cameraController?.ApplyExternalPanTilt(input.GetCameraPanInput(), input.GetCameraTiltInput(), Time.deltaTime);
            cameraController?.ApplyZoomDelta(input.ConsumeExternalZoomDelta());

            weapon?.Tick(command.firePrimary, Time.deltaTime);

            audioHooks?.UpdateFromCommand(command);

            if (hud != null && flight != null)
            {
                var viewModel = new FlightHudViewModel
                {
                    viewMode = cameraController != null && cameraController.IsExternalActive ? "EXTERNAL" : "COCKPIT",
                    externalPanDegrees = cameraController != null ? cameraController.ExternalPanDegrees : 0f,
                    externalTiltDegrees = cameraController != null ? cameraController.ExternalTiltDegrees : 0f,
                    externalDistance = cameraController != null ? cameraController.ExternalDistance : 0f,
                    cockpitFov = cameraController != null ? cameraController.ActiveFov : 95f
                };

                hud.SetTelemetry(flight.State, command, viewModel);
            }
        }

        public void CycleAssistMode()
        {
            if (flight == null)
            {
                return;
            }

            FlightAssistMode next = flight.State.assistMode switch
            {
                FlightAssistMode.AssistOff => FlightAssistMode.AttitudeAssist,
                FlightAssistMode.AttitudeAssist => FlightAssistMode.CoupledAssist,
                FlightAssistMode.CoupledAssist => FlightAssistMode.FrameLockAssist,
                _ => FlightAssistMode.AssistOff
            };

            flight.SetAssistMode(next);
        }

        void WireVisualSubsystems()
        {
            if (cogTransform == null)
            {
                return;
            }

            ShipVisualReferences visuals = cogTransform.GetComponentInChildren<ShipVisualReferences>(true);
            if (visuals == null)
            {
                Transform uwingRoot = ShipHierarchyUtility.FindChildRecursive(cogTransform, "uwing", "uwing2");
                if (uwingRoot != null)
                {
                    visuals = uwingRoot.GetComponent<ShipVisualReferences>();
                    if (visuals == null)
                    {
                        visuals = uwingRoot.gameObject.AddComponent<ShipVisualReferences>();
                    }
                }
            }

            if (visuals == null)
            {
                Debug.LogWarning("PlayerShipController: no U-wing visual references found under COG.", this);
                return;
            }

            visuals.TryAutoWire();
            ShipHierarchyUtility.DisableMeshColliders(visuals.transform);
            DisablePlayerShipMotionVectors(visuals.transform);

            if (weapon != null)
            {
                weapon.SetGunNodes(visuals.GunNode1, visuals.GunNode2);
            }

            cameraController?.SetFpvNode(visuals.FpvCameraNode);

            if (rcsVfx == null)
            {
                rcsVfx = GetComponent<RcsThrusterVfx>();
                if (rcsVfx == null)
                {
                    rcsVfx = gameObject.AddComponent<RcsThrusterVfx>();
                }
            }

            if (rcsPuffPrefab != null)
            {
                rcsVfx.Configure(visuals, input, flight, rcsPuffPrefab);
            }

            if (engineGlowVfx == null)
            {
                engineGlowVfx = GetComponent<EngineGlowVfx>();
                if (engineGlowVfx == null)
                {
                    engineGlowVfx = gameObject.AddComponent<EngineGlowVfx>();
                }
            }

            engineGlowVfx.Configure(visuals, input, flight, enginePlumePrefab);
        }

        static void DisablePlayerShipMotionVectors(Transform visualRoot)
        {
            if (visualRoot == null)
            {
                return;
            }

            Renderer[] renderers = visualRoot.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer != null)
                {
                    renderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
                }
            }
        }
    }
}
