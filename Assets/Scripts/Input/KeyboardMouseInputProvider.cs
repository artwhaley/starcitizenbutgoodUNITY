using UnityEngine;
using UnityEngine.InputSystem;

namespace FlightModel
{
    public class KeyboardMouseInputProvider
    {
        const float MouseSensitivity = 0.15f;

        InputActionAsset asset;
        InputActionMap flight;
        InputAction thrustForward;
        InputAction thrustRight;
        InputAction thrustUp;
        InputAction pitch;
        InputAction yaw;
        InputAction roll;
        InputAction mousePitch;
        InputAction mouseYaw;
        InputAction boost;
        InputAction brake;
        InputAction firePrimary;
        InputAction toggleAssist;
        InputAction toggleCamera;
        InputAction toggleBindingsPanel;
        InputAction toggleTuningOverlay;
        InputAction toggleFineControl;
        InputAction externalCameraZoom;

        public bool IsReady => flight != null;

        public void Initialize(InputActionAsset inputAsset)
        {
            if (inputAsset == null)
            {
                Debug.LogError("KeyboardMouseInputProvider: InputActionAsset is not assigned.");
                return;
            }

            asset = inputAsset;
            flight = asset.FindActionMap("Flight", throwIfNotFound: true);
            thrustForward = flight.FindAction("ThrustForward", throwIfNotFound: true);
            thrustRight = flight.FindAction("ThrustRight", throwIfNotFound: true);
            thrustUp = flight.FindAction("ThrustUp", throwIfNotFound: true);
            pitch = flight.FindAction("Pitch", throwIfNotFound: true);
            yaw = flight.FindAction("Yaw", throwIfNotFound: true);
            roll = flight.FindAction("Roll", throwIfNotFound: true);
            mousePitch = flight.FindAction("MousePitch", throwIfNotFound: true);
            mouseYaw = flight.FindAction("MouseYaw", throwIfNotFound: true);
            boost = flight.FindAction("Boost", throwIfNotFound: true);
            brake = flight.FindAction("Brake", throwIfNotFound: true);
            firePrimary = flight.FindAction("FirePrimary", throwIfNotFound: true);
            toggleAssist = flight.FindAction("ToggleAssist", throwIfNotFound: true);
            toggleCamera = flight.FindAction("ToggleCamera", throwIfNotFound: true);
            toggleBindingsPanel = flight.FindAction("ToggleBindingsPanel", throwIfNotFound: true);
            toggleTuningOverlay = flight.FindAction("ToggleTuningOverlay", throwIfNotFound: true);
            toggleFineControl = flight.FindAction("ToggleFineControl", throwIfNotFound: true);
            externalCameraZoom = flight.FindAction("ExternalCameraZoom", throwIfNotFound: true);
        }

        public void Enable()
        {
            asset?.Enable();
            flight?.Enable();
        }

        public void Disable() => flight?.Disable();

        public ShipInputCommand Poll()
        {
            if (flight == null)
            {
                return default;
            }

            var command = new ShipInputCommand
            {
                thrustForward = thrustForward.ReadValue<float>(),
                thrustRight = thrustRight.ReadValue<float>(),
                thrustUp = thrustUp.ReadValue<float>(),
                pitch = pitch.ReadValue<float>(),
                yaw = yaw.ReadValue<float>(),
                roll = roll.ReadValue<float>(),
                boost = IsPressed(boost),
                brake = IsPressed(brake),
                firePrimary = IsPressed(firePrimary)
            };

            float mousePitchValue = mousePitch.ReadValue<float>();
            float mouseYawValue = mouseYaw.ReadValue<float>();
            if (Mathf.Abs(mousePitchValue) < 0.001f && Mathf.Abs(mouseYawValue) < 0.001f && Mouse.current != null)
            {
                Vector2 delta = Mouse.current.delta.ReadValue();
                mousePitchValue = -delta.y * MouseSensitivity;
                mouseYawValue = delta.x * MouseSensitivity;
            }

            command.pitch = MergeAxis(mousePitchValue, command.pitch);
            command.yaw = MergeAxis(mouseYawValue, command.yaw);

            return command;
        }

        public bool WasToggleAssistPressedThisFrame() => toggleAssist != null && toggleAssist.WasPressedThisFrame();
        public bool WasToggleCameraPressedThisFrame() => toggleCamera != null && toggleCamera.WasPressedThisFrame();
        public bool WasToggleBindingsPanelPressedThisFrame() => toggleBindingsPanel != null && toggleBindingsPanel.WasPressedThisFrame();
        public bool WasToggleTuningOverlayPressedThisFrame() => toggleTuningOverlay != null && toggleTuningOverlay.WasPressedThisFrame();
        public bool WasToggleFineControlPressedThisFrame() => toggleFineControl != null && toggleFineControl.WasPressedThisFrame();
        public float ReadExternalCameraZoomDelta() => externalCameraZoom != null ? externalCameraZoom.ReadValue<float>() : 0f;

        static bool IsPressed(InputAction action) => action != null && action.IsPressed();

        static float MergeAxis(float primary, float secondary)
            => Mathf.Abs(primary) > 0.001f ? primary : secondary;
    }
}
