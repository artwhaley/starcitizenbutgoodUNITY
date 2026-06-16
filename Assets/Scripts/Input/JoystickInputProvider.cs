using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace FlightModel
{
    public class JoystickInputProvider : MonoBehaviour
    {
        ShipInputBindingsData bindings = new();
        readonly List<InputDevice> joysticks = new();

        void Awake()
        {
            bindings = ShipInputBindingsStore.Load();
            RefreshDevices();
        }

        public void RefreshDevices()
        {
            joysticks.Clear();
            foreach (InputDevice device in InputSystem.devices)
            {
                if (!IsBindableDevice(device))
                {
                    continue;
                }

                joysticks.Add(device);
            }

            joysticks.Sort((a, b) =>
            {
                int nameCompare = string.Compare(a.displayName, b.displayName, System.StringComparison.Ordinal);
                return nameCompare != 0
                    ? nameCompare
                    : string.Compare(a.path, b.path, System.StringComparison.Ordinal);
            });
        }

        public string GetDeviceLabel(InputDevice device)
            => device == null ? "None" : $"{device.displayName}  [{device.path}]";

        public float ReadRawAxis(InputDevice device, int axisIndex)
        {
            if (device == null)
            {
                return 0f;
            }

            IReadOnlyList<AxisControl> axes = GetAxisControls(device);
            if (axisIndex < 0 || axisIndex >= axes.Count)
            {
                return 0f;
            }

            return axes[axisIndex].ReadValue();
        }

        public bool ReadRawButton(InputDevice device, int buttonIndex)
        {
            if (device == null)
            {
                return false;
            }

            IReadOnlyList<ButtonControl> buttons = GetButtonControls(device);
            if (buttonIndex < 0 || buttonIndex >= buttons.Count)
            {
                return false;
            }

            return buttons[buttonIndex].isPressed;
        }

        public IReadOnlyList<InputDevice> GetJoysticks() => joysticks;

        public ShipInputBindingsData Bindings => bindings;

        public void SaveBindings()
        {
            ShipInputBindingsStore.Save(bindings);
        }

        public ShipInputCommand BuildHardwareCommand()
        {
            var command = new ShipInputCommand();
            SetAxisValue(ref command.thrustForward, ShipControlAxis.ThrustForward);
            SetAxisValue(ref command.thrustRight, ShipControlAxis.ThrustRight);
            SetAxisValue(ref command.thrustUp, ShipControlAxis.ThrustUp);
            SetAxisValue(ref command.pitch, ShipControlAxis.Pitch);
            SetAxisValue(ref command.yaw, ShipControlAxis.Yaw);
            SetAxisValue(ref command.roll, ShipControlAxis.Roll);
            command.firePrimary = IsFireButtonPressed();
            return command;
        }

        public float GetCameraPan() => ReadBoundAxis(ShipControlAxis.CameraPan);
        public float GetCameraTilt() => ReadBoundAxis(ShipControlAxis.CameraTilt);

        public bool IsFireButtonPressed() => ReadBoundButton(bindings.firePrimary);

        public float GetLiveAxisValue(HardwareAxisBinding binding) => ReadAxisBinding(binding);

        public bool GetLiveButtonPressed(HardwareButtonBinding binding) => ReadButtonBinding(binding);

        public int GetAxisCount(InputDevice device)
        {
            return device == null ? 0 : GetAxisControls(device).Count;
        }

        public int GetButtonCount(InputDevice device)
        {
            return device == null ? 0 : GetButtonControls(device).Count;
        }

        void SetAxisValue(ref float field, ShipControlAxis axis)
        {
            field = ReadBoundAxis(axis);
        }

        float ReadBoundAxis(ShipControlAxis axis)
        {
            int index = (int)axis;
            if (bindings.axisBindings == null || index < 0 || index >= bindings.axisBindings.Length)
            {
                return 0f;
            }

            return ReadAxisBinding(bindings.axisBindings[index]);
        }

        float ReadAxisBinding(HardwareAxisBinding binding)
        {
            if (binding == null || !binding.enabled)
            {
                return 0f;
            }

            InputDevice device = FindDevice(binding.devicePath, binding.displayName);
            if (device == null)
            {
                return 0f;
            }

            IReadOnlyList<AxisControl> axes = GetAxisControls(device);
            if (binding.axisIndex < 0 || binding.axisIndex >= axes.Count)
            {
                return 0f;
            }

            float raw = axes[binding.axisIndex].ReadValue();
            return InputCalibrationUtility.Apply(raw, binding.calibration);
        }

        bool ReadBoundButton(HardwareButtonBinding binding) => ReadButtonBinding(binding);

        bool ReadButtonBinding(HardwareButtonBinding binding)
        {
            if (binding == null || !binding.enabled)
            {
                return false;
            }

            InputDevice device = FindDevice(binding.devicePath, binding.displayName);
            if (device == null)
            {
                return false;
            }

            IReadOnlyList<ButtonControl> buttons = GetButtonControls(device);
            if (binding.buttonIndex < 0 || binding.buttonIndex >= buttons.Count)
            {
                return false;
            }

            return buttons[binding.buttonIndex].isPressed;
        }

        InputDevice FindDevice(string devicePath, string displayName)
        {
            if (!string.IsNullOrEmpty(devicePath))
            {
                foreach (InputDevice device in joysticks)
                {
                    if (device.path == devicePath)
                    {
                        return device;
                    }
                }
            }

            if (!string.IsNullOrEmpty(displayName))
            {
                foreach (InputDevice device in joysticks)
                {
                    if (device.displayName == displayName)
                    {
                        return device;
                    }
                }
            }

            return null;
        }

        public IReadOnlyList<AxisControl> GetAxisControls(InputDevice device)
        {
            if (device == null)
            {
                return System.Array.Empty<AxisControl>();
            }

            return device.allControls
                .OfType<AxisControl>()
                .Where(c => c is not ButtonControl)
                .OrderBy(c => c.path)
                .ToList();
        }

        public IReadOnlyList<ButtonControl> GetButtonControls(InputDevice device)
        {
            if (device == null)
            {
                return System.Array.Empty<ButtonControl>();
            }

            return device.allControls.OfType<ButtonControl>().OrderBy(c => c.path).ToList();
        }

        public string GetAxisControlLabel(InputDevice device, int axisIndex)
        {
            IReadOnlyList<AxisControl> axes = GetAxisControls(device);
            if (axisIndex < 0 || axisIndex >= axes.Count)
            {
                return "Axis ?";
            }

            AxisControl axisControl = axes[axisIndex];
            return $"{axisIndex}: {axisControl.displayName}  <{axisControl.path}>";
        }

        public string GetButtonControlLabel(InputDevice device, int buttonIndex)
        {
            IReadOnlyList<ButtonControl> buttons = GetButtonControls(device);
            if (buttonIndex < 0 || buttonIndex >= buttons.Count)
            {
                return "Button ?";
            }

            ButtonControl button = buttons[buttonIndex];
            return $"{buttonIndex}: {button.displayName}  <{button.path}>";
        }

        static bool IsBindableDevice(InputDevice device)
        {
            if (device == null || !device.added)
            {
                return false;
            }

            if (device is Keyboard or Mouse or Pen or Pointer or Sensor)
            {
                return false;
            }

            if (device is Joystick or Gamepad)
            {
                return true;
            }

            foreach (InputControl control in device.allControls)
            {
                if (control is AxisControl or ButtonControl or StickControl or Vector2Control)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
