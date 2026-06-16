using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FlightModel
{
    public class ShipInputReader : MonoBehaviour
    {
        [SerializeField] InputActionAsset inputActions;

        KeyboardMouseInputProvider keyboardMouse;
        JoystickInputProvider joystick;

        ShipInputCommand lastCommand;
        float pendingExternalZoomDelta;
        bool loggedBindingsStub;

        public ShipInputCommand LastCommand => lastCommand;

        public event Action ToggleAssistRequested;
        public event Action ToggleCameraRequested;
        public event Action ToggleBindingsPanelRequested;
        public event Action ToggleTuningOverlayRequested;

        public void SetJoystickProvider(JoystickInputProvider provider) => joystick = provider;

        void Awake()
        {
            keyboardMouse = new KeyboardMouseInputProvider();
            keyboardMouse.Initialize(inputActions);
        }

        void OnEnable()
        {
            if (keyboardMouse != null && keyboardMouse.IsReady)
            {
                keyboardMouse.Enable();
            }
        }

        void OnDisable() => keyboardMouse?.Disable();

        void Update()
        {
            if (keyboardMouse == null || !keyboardMouse.IsReady)
            {
                return;
            }

            lastCommand = BuildCommand();

            if (keyboardMouse.WasToggleAssistPressedThisFrame())
            {
                ToggleAssistRequested?.Invoke();
            }

            if (keyboardMouse.WasToggleCameraPressedThisFrame())
            {
                ToggleCameraRequested?.Invoke();
            }

            if (keyboardMouse.WasToggleBindingsPanelPressedThisFrame())
            {
                ToggleBindingsPanelRequested?.Invoke();
            }

            if (keyboardMouse.WasToggleTuningOverlayPressedThisFrame())
            {
                ToggleTuningOverlayRequested?.Invoke();
            }

            pendingExternalZoomDelta += keyboardMouse.ReadExternalCameraZoomDelta();
        }

        public ShipInputCommand BuildCommand()
        {
            if (keyboardMouse == null || !keyboardMouse.IsReady)
            {
                return default;
            }

            ShipInputCommand command = keyboardMouse.Poll();

            if (joystick != null)
            {
                ShipInputCommand hardware = joystick.BuildHardwareCommand();
                command.thrustForward = MergeAxis(hardware.thrustForward, command.thrustForward);
                command.thrustRight = MergeAxis(hardware.thrustRight, command.thrustRight);
                command.thrustUp = MergeAxis(hardware.thrustUp, command.thrustUp);
                command.pitch = MergeAxis(hardware.pitch, command.pitch);
                command.yaw = MergeAxis(hardware.yaw, command.yaw);
                command.roll = MergeAxis(hardware.roll, command.roll);
                command.firePrimary = hardware.firePrimary || command.firePrimary;
            }

            return command;
        }

        public float ConsumeExternalZoomDelta()
        {
            float delta = pendingExternalZoomDelta;
            pendingExternalZoomDelta = 0f;
            return delta;
        }

        public float GetCameraPanInput() => joystick != null ? joystick.GetCameraPan() : 0f;
        public float GetCameraTiltInput() => joystick != null ? joystick.GetCameraTilt() : 0f;

        public void HandleBindingsPanelToggle(InputBindingsPanel panel)
        {
            if (panel == null)
            {
                if (!loggedBindingsStub)
                {
                    Debug.Log("Bindings panel not wired yet.");
                    loggedBindingsStub = true;
                }

                return;
            }

            if (panel.IsVisible)
            {
                panel.Hide();
            }
            else
            {
                panel.Show();
            }
        }

        static float MergeAxis(float hardware, float other)
            => Mathf.Abs(hardware) > 0.001f ? hardware : other;
    }
}
