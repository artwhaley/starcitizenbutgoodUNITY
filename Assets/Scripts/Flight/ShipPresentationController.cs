using UnityEngine;

namespace FlightModel
{
    /// <summary>
    /// Applies ship simulation state to Unity presentation: transform, camera,
    /// HUD, and audio. Does not own simulation or input logic.
    /// </summary>
    public class ShipPresentationController : MonoBehaviour
    {
        [SerializeField] Transform cogTransform;
        [SerializeField] ShipFlightController flight;
        [SerializeField] ShipInputReader input;
        [SerializeField] FlightDebugHud hud;
        [SerializeField] ShipCameraController cameraController;
        [SerializeField] PrimaryWeaponController weapon;
        [SerializeField] ShipAudioHooks audioHooks;

        public Transform CogTransform
        {
            get => cogTransform;
            set => cogTransform = value;
        }

        public ShipInputReader Input
        {
            get => input;
            set => input = value;
        }

        public ShipFlightController Flight
        {
            get => flight;
            set => flight = value;
        }

        public FlightDebugHud Hud
        {
            get => hud;
            set => hud = value;
        }

        public ShipCameraController CameraController
        {
            get => cameraController;
            set => cameraController = value;
        }

        public PrimaryWeaponController Weapon
        {
            get => weapon;
            set => weapon = value;
        }

        public ShipAudioHooks AudioHooks
        {
            get => audioHooks;
            set => audioHooks = value;
        }

        /// <summary>
        /// Apply the authoritative ship state to the COG transform.
        /// Call from FixedUpdate after simulation has advanced.
        /// </summary>
        public void ApplySimulationState()
        {
            if (flight == null || cogTransform == null)
            {
                return;
            }

            ShipState state = flight.State;
            cogTransform.SetPositionAndRotation(state.position, state.rotation);
        }

        /// <summary>
        /// Update camera, HUD, and audio presentation from the current command
        /// and ship state. Call from Update.
        /// </summary>
        public void TickPresentation(in ShipInputCommand command, float deltaTime)
        {
            if (input == null)
            {
                return;
            }

            cameraController?.ApplyExternalPanTilt(
                input.GetCameraPanInput(),
                input.GetCameraTiltInput(),
                deltaTime);
            cameraController?.ApplyZoomDelta(input.ConsumeExternalZoomDelta());

            audioHooks?.UpdateFromCommand(command);

            if (hud != null && flight != null)
            {
                var viewModel = new FlightHudViewModel
                {
                    viewMode = cameraController != null && cameraController.IsExternalActive
                        ? "EXTERNAL"
                        : "COCKPIT",
                    externalPanDegrees = cameraController != null ? cameraController.ExternalPanDegrees : 0f,
                    externalTiltDegrees = cameraController != null ? cameraController.ExternalTiltDegrees : 0f,
                    externalDistance = cameraController != null ? cameraController.ExternalDistance : 0f,
                    cockpitFov = cameraController != null ? cameraController.ActiveFov : 95f
                };

                hud.SetTelemetry(flight.State, command, viewModel);
                hud.SetWorldMarkers(
                    cameraController != null ? cameraController.GetActiveCamera() : Camera.main,
                    flight.State,
                    weapon);
            }
        }
    }
}
