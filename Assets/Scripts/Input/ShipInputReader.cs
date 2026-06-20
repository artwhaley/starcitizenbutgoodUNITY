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
        bool fineControlModeActive;

        public ShipInputCommand LastCommand => lastCommand;
        public bool FineControlModeActive => fineControlModeActive;

        public event Action ToggleAssistRequested;
        public event Action ToggleCameraRequested;
        public event Action ToggleBindingsPanelRequested;
        public event Action ToggleTuningOverlayRequested;
        public event Action ToggleDockingModeRequested;
        public event Action UndockRequested;
        public event Action ToggleDebugOverlayRequested;

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

            if (keyboardMouse.WasToggleDockingModePressedThisFrame())
            {
                ToggleDockingModeRequested?.Invoke();
            }

            if (keyboardMouse.WasUndockPressedThisFrame())
            {
                UndockRequested?.Invoke();
            }

            if (keyboardMouse.WasToggleDebugOverlayPressedThisFrame())
            {
                ToggleDebugOverlayRequested?.Invoke();
            }

            if (keyboardMouse.WasToggleFineControlPressedThisFrame())
            {
                fineControlModeActive = !fineControlModeActive;
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
                command.boost = hardware.boost || command.boost;
                command.brake = hardware.brake || command.brake;
                command.firePrimary = hardware.firePrimary || command.firePrimary;
            }

            command.fineControl = fineControlModeActive;

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
