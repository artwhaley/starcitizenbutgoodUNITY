using System;
using FlightModel.Authority;
using FlightModel.Docking;
using UnityEngine;

namespace FlightModel
{
    public class ShipVehicle : MonoBehaviour
    {
        [Header("Core")]
        [SerializeField] Transform cogTransform;
        [SerializeField] ShipFlightController flight;
        [SerializeField] LocalGameAuthority authority;
        [SerializeField] ShipPresentationController presentation;
        [SerializeField] ShipTuning tuning;
        [SerializeField] Vector3 spawnPosition = new(50f, 2f, 0f);
        [SerializeField] Quaternion spawnRotation = Quaternion.identity;

        [Header("Local presentation")]
        [SerializeField] ShipInputReader input;
        [SerializeField] FlightDebugHud hud;
        [SerializeField] ShipCameraController cameraController;
        [SerializeField] PrimaryWeaponController weapon;
        [SerializeField] JoystickInputProvider joystickProvider;
        [SerializeField] InputBindingsPanel bindingsPanel;
        [SerializeField] RcsThrusterVfx rcsVfx;
        [SerializeField] EngineGlowVfx engineGlowVfx;
        [SerializeField] ShipAudioHooks audioHooks;
        [SerializeField] GameObject rcsPuffPrefab;
        [SerializeField] GameObject enginePlumePrefab;

        [Header("Docking")]
        [SerializeField] ShipDockingNode shipDockingNode;
        [SerializeField] DockingTargetProvider dockingTargetProvider;
        [SerializeField] DockingHud dockingHud;
        [SerializeField] DockingModeController dockingModeController;
        [SerializeField] DockableShip dockableShip;
        [SerializeField] DockingCaptureController dockingCaptureController;
        [SerializeField] DockingCaptureSettings dockingCaptureSettings = DockingCaptureSettings.Default;

        [Header("Seat")]
        [SerializeField] ShipPilotSeat pilotSeat;

        bool loggedPresentationWiring;

        public bool HasLocalPilot { get; private set; }
        public VehicleOperationalState OperationalState { get; private set; } = VehicleOperationalState.IdleUnoccupied;
        public ShipInputReader Input => input;
        public InputBindingsPanel BindingsPanel => bindingsPanel;
        public ShipPilotSeat PilotSeat => pilotSeat;
        public uint ServerTick => authority != null ? authority.ServerTick : 0u;
        public int ControlledEntityId => authority != null ? authority.ControlledEntityId : 1;

        void Awake()
        {
            ResolveAuthoredReferences();

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
        }

        void Start()
        {
            WireShipSystemsSafe();
            RefreshOperationalState();
        }

        public void SetLocalPilotSeat(ShipPilotSeat seat)
        {
            pilotSeat = seat;
            HasLocalPilot = true;
            RefreshOperationalState();
        }

        public void ClearLocalPilotSeat(ShipPilotSeat seat)
        {
            if (pilotSeat == seat)
            {
                HasLocalPilot = false;
                RefreshOperationalState();
            }
        }

        public void SubmitLocalInput(in ClientInputCommand localCommand)
        {
            if (flight == null)
            {
                return;
            }

            ShipInputCommand submitCommand = dockingModeController != null
                ? dockingModeController.TransformInput(localCommand.shipInput, flight.State)
                : localCommand.shipInput;

            if (dockingCaptureController != null && dockingCaptureController.IsFlightGated)
            {
                submitCommand = default;
            }

            if (authority == null)
            {
                Debug.LogError(
                    "ShipVehicle: LocalGameAuthority is missing. Local ship simulation cannot advance.",
                    this);
                return;
            }

            ClientInputCommand command = localCommand;
            command.shipInput = submitCommand;
            authority.SubmitInput(command);
            authority.Tick(Time.fixedDeltaTime);
            presentation?.ApplySimulationState();
            RefreshOperationalState();
        }

        public void TickLocalPresentation(in ShipInputCommand command, float deltaTime)
        {
            ShipInputCommand presentationCommand = dockingCaptureController != null && dockingCaptureController.IsFlightGated
                ? default
                : command;

            weapon?.Tick(
                presentationCommand.firePrimary
                && HasLocalPilot
                && (dockingCaptureController == null || !dockingCaptureController.IsFlightGated));
            presentation?.TickPresentation(presentationCommand, deltaTime);
            UpdateDockingHud();
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

        public void ToggleCameraView()
        {
            if (dockingModeController != null)
            {
                dockingModeController.ToggleCameraView();
                return;
            }

            cameraController?.ToggleView(false, null);
        }

        public void ToggleDebugOverlay() => hud?.ToggleDebugOverlay();

        public void ToggleDockingMode()
        {
            if (shipDockingNode == null)
            {
                ResolveShipDockingNode();
                if (shipDockingNode == null)
                {
                    return;
                }
            }

            if (dockingModeController == null)
            {
                Debug.LogWarning(
                    "ShipVehicle: cannot toggle docking mode because DockingModeController is missing from the ship prefab.",
                    this);
                return;
            }

            dockingModeController.ToggleDockingMode(shipDockingNode);
        }

        public void HandleUndockRequest()
        {
            if (dockingCaptureController == null)
            {
                return;
            }

            if (dockingCaptureController.RequestUndock())
            {
                Debug.Log("ShipVehicle: undock requested.", this);
                RefreshOperationalState();
            }
        }

        void ResolveAuthoredReferences()
        {
            cogTransform ??= ShipHierarchyUtility.FindChildRecursive(transform, "COG");
            flight ??= cogTransform != null ? cogTransform.GetComponent<ShipFlightController>() : GetComponentInChildren<ShipFlightController>(true);
            input ??= GetComponent<ShipInputReader>();
            hud ??= FindAnyObjectByType<FlightDebugHud>(FindObjectsInactive.Include);
            cameraController ??= GetComponent<ShipCameraController>();
            weapon ??= GetComponent<PrimaryWeaponController>();
            authority ??= GetComponent<LocalGameAuthority>();
            presentation ??= GetComponent<ShipPresentationController>();
            joystickProvider ??= GetComponent<JoystickInputProvider>();
            bindingsPanel ??= FindAnyObjectByType<InputBindingsPanel>(FindObjectsInactive.Include);
            rcsVfx ??= GetComponent<RcsThrusterVfx>();
            engineGlowVfx ??= GetComponent<EngineGlowVfx>();
            audioHooks ??= GetComponent<ShipAudioHooks>();
            pilotSeat ??= GetComponentInChildren<ShipPilotSeat>(true);
            dockingTargetProvider ??= GetComponent<DockingTargetProvider>();
            dockingModeController ??= GetComponent<DockingModeController>();
            dockableShip ??= GetComponent<DockableShip>();
            dockingCaptureController ??= GetComponent<DockingCaptureController>();
        }

        void WireShipSystemsSafe()
        {
            try
            {
                rcsPuffPrefab = VfxPrefabResolver.ResolveRcsPuff(rcsPuffPrefab);
                WireVisualSubsystems();
                WireDockingSubsystems();
            }
            catch (Exception exception)
            {
                Debug.LogError($"ShipVehicle: startup wiring failed: {exception.Message}", this);
            }
        }

        void WireVisualSubsystems()
        {
            if (cogTransform == null)
            {
                Debug.LogError("ShipVehicle: COG transform is missing. Add/assign the authored COG under the ship prefab.", this);
                return;
            }

            Transform uwingRoot = ShipHierarchyUtility.FindChildRecursive(cogTransform, "uwing", "uwing2");
            ShipVisualReferences visuals = cogTransform.GetComponentInChildren<ShipVisualReferences>(true);
            if (visuals == null && uwingRoot != null)
            {
                visuals = uwingRoot.GetComponent<ShipVisualReferences>();
            }

            if (visuals == null)
            {
                Debug.LogWarning(
                    "ShipVehicle: no ShipVisualReferences component found under the ship COG. " +
                    "Add it to the authored visual root so camera, weapons, and VFX can use explicit model nodes.",
                    this);
                return;
            }

            visuals.TryAutoWire();
            ShipHierarchyUtility.DisableMeshColliders(visuals.transform);
            DisablePlayerShipMotionVectors(visuals.transform);

            ShipWeaponHardpoints hardpoints = GetComponentInChildren<ShipWeaponHardpoints>(true);
            if (hardpoints == null)
            {
                Debug.LogWarning(
                    "ShipVehicle: ShipWeaponHardpoints missing from authored ship hierarchy. " +
                    "Add it to the visual root so weapons use explicit muzzle nodes.",
                    this);
            }
            else
            {
                hardpoints.TryAutoWire();
            }

            if (presentation != null)
            {
                presentation.CogTransform = cogTransform;
                presentation.Flight = flight;
                presentation.Input = input;
                WirePresentationFields(presentation);
            }

            if (authority != null)
            {
                authority.controlledFlight = flight;
                authority.hardpoints = hardpoints;
                authority.playerShipRoot = cogTransform;
                authority.RefreshControlledShipEntity(cogTransform);

                if (authority.viewPool == null)
                {
                    authority.viewPool = GetComponentInChildren<ProjectileViewPool>(true);
                    if (authority.viewPool == null)
                    {
                        Debug.LogError(
                            "ShipVehicle: ProjectileViewPool missing from prefab. Weapons will not fire.",
                            this);
                    }
                }
            }

            cameraController?.SetFpvNode(visuals.FpvCameraNode);

            if (rcsVfx != null && rcsPuffPrefab != null)
            {
                rcsVfx.Configure(visuals, input, flight, rcsPuffPrefab);
            }

            if (engineGlowVfx != null)
            {
                engineGlowVfx.Configure(visuals, input, flight, enginePlumePrefab);
            }
        }

        void WirePresentationFields(ShipPresentationController pres)
        {
            if (pres.Hud == null)
            {
                pres.Hud = hud;
            }

            if (pres.CameraController == null)
            {
                pres.CameraController = cameraController;
            }

            if (pres.Weapon == null)
            {
                pres.Weapon = weapon;
            }

            if (pres.AudioHooks == null)
            {
                pres.AudioHooks = audioHooks;
            }

            if (!loggedPresentationWiring)
            {
                Debug.Log(
                    $"ShipPresentationController wired: hud={pres.Hud != null}, " +
                    $"cam={pres.CameraController != null}, weapon={pres.Weapon != null}, " +
                    $"audio={pres.AudioHooks != null}",
                    this);
                loggedPresentationWiring = true;
            }
        }

        void WireDockingSubsystems()
        {
            ResolveShipDockingNode();
            EnsureDockingHud();

            if (dockingTargetProvider == null)
            {
                Debug.LogError(
                    "ShipVehicle: DockingTargetProvider missing from ship prefab. Add it explicitly; no runtime provider will be invented.",
                    this);
            }
            else if (shipDockingNode != null)
            {
                dockingTargetProvider.SetSearchOrigin(shipDockingNode.NodeTransform);
            }

            if (dockableShip == null)
            {
                Debug.LogError(
                    "ShipVehicle: DockableShip missing from ship prefab. Add it explicitly; capture will remain disabled.",
                    this);
            }
            else
            {
                dockableShip.ShipDockingNode = shipDockingNode;
                dockableShip.Flight = flight;
                dockableShip.TargetProvider = dockingTargetProvider;
                dockableShip.ShipRoot = cogTransform;
            }

            if (dockingCaptureController == null)
            {
                Debug.LogError(
                    "ShipVehicle: DockingCaptureController missing from ship prefab. Add it explicitly; capture will remain disabled.",
                    this);
            }
            else
            {
                dockingCaptureController.Configure(
                    dockableShip,
                    flight,
                    dockingCaptureSettings,
                    dockingModeController,
                    cameraController);
                if (authority != null)
                {
                    authority.dockingCapture = dockingCaptureController;
                }
            }

            if (dockingModeController == null)
            {
                Debug.LogError(
                    "ShipVehicle: DockingModeController missing from ship prefab. Add it explicitly; docking camera/control mode will remain disabled.",
                    this);
            }
            else
            {
                dockingModeController.CameraController = cameraController;
                dockingModeController.Hud = dockingHud;
                dockingModeController.TargetProvider = dockingTargetProvider;
                dockingModeController.DockingNode = shipDockingNode;
            }
        }

        void ResolveShipDockingNode()
        {
            if (cogTransform == null)
            {
                return;
            }

            if (shipDockingNode == null)
            {
                Transform found = FindAuthoredShipDockingNode(cogTransform);
                if (found != null && ShipDockingNode.IsPotentialNodeName(found.name))
                {
                    shipDockingNode = found.GetComponent<ShipDockingNode>();
                    if (shipDockingNode == null)
                    {
                        shipDockingNode = found.gameObject.AddComponent<ShipDockingNode>();
                        Debug.LogWarning(
                            $"ShipVehicle: attached ShipDockingNode to authored docking transform '{found.name}'. " +
                            "This is allowed only because the transform already exists in the ship hierarchy; " +
                            "move the component into the prefab/import setup when the ship asset is next touched.",
                            found);
                    }
                }
            }

            if (shipDockingNode == null)
            {
                Debug.LogError(
                    "ShipVehicle: missing authored ship docking node. Add a transform named 'node_docking' " +
                    "or 'node_docking_*' under the ship hierarchy and attach ShipDockingNode to it.",
                    this);
            }
        }

        static Transform FindAuthoredShipDockingNode(Transform root)
        {
            if (root == null)
            {
                return null;
            }

            Transform[] all = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++)
            {
                Transform candidate = all[i];
                if (candidate != null && ShipDockingNode.IsPotentialNodeName(candidate.name))
                {
                    return candidate;
                }
            }

            return null;
        }

        void EnsureDockingHud()
        {
            if (dockingHud == null)
            {
                DockingHud existing = FindAnyObjectByType<DockingHud>(FindObjectsInactive.Exclude);
                if (existing == null)
                {
                    existing = FindAnyObjectByType<DockingHud>(FindObjectsInactive.Include);
                }

                dockingHud = existing;
            }

            if (dockingHud == null)
            {
                var hudGo = new GameObject("DockingHud");
                DontDestroyOnLoad(hudGo);
                dockingHud = hudGo.AddComponent<DockingHud>();
            }

            dockingHud.SetActive(false);
        }

        void UpdateDockingHud()
        {
            dockingModeController?.SyncHudVisibility();

            if (dockingHud == null
                || dockingModeController == null
                || !dockingModeController.ShouldUseDockingControls
                || flight == null)
            {
                return;
            }

            if (shipDockingNode == null || dockingTargetProvider == null)
            {
                dockingHud.UpdateTelemetry(DockingTelemetry.Empty);
                return;
            }

            dockingTargetProvider.UpdateTarget();
            DockingTelemetry telemetry = DockingTelemetryUtility.Compute(
                shipDockingNode,
                flight.State.linearVelocity,
                shipDockingNode.IsDockingActive,
                dockingTargetProvider.CurrentTarget);

            if (dockingCaptureController != null)
            {
                telemetry.magneticCaptureActive = dockingCaptureController.IsMagneticActive;
                telemetry.docked = dockingCaptureController.IsDocked;
                telemetry.recaptureLockout = dockingCaptureController.IsLockedOut;
            }

            dockingHud.UpdateTelemetry(telemetry);
        }

        void RefreshOperationalState()
        {
            bool docked = dockingCaptureController != null && dockingCaptureController.IsDocked;
            OperationalState = docked
                ? (HasLocalPilot ? VehicleOperationalState.DockedOccupied : VehicleOperationalState.DockedIdle)
                : (HasLocalPilot ? VehicleOperationalState.PilotedLocal : VehicleOperationalState.IdleUnoccupied);
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
