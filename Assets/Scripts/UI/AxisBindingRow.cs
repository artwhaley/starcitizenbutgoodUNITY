using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace FlightModel
{
    public class AxisBindingRow : MonoBehaviour
    {
        [SerializeField] Text labelText;
        [SerializeField] Dropdown deviceDropdown;
        [SerializeField] Dropdown axisDropdown;
        [SerializeField] Toggle invertToggle;
        [SerializeField] Slider deadzoneSlider;
        [SerializeField] Slider scaleSlider;
        [SerializeField] Slider curveSlider;
        [SerializeField] Text liveValueText;
        [SerializeField] Slider liveBar;

        ShipControlAxis axis;
        JoystickInputProvider provider;
        System.Action onChanged;
        Text deadzoneValueText;
        Text scaleValueText;
        Text curveValueText;

        public void Initialize(
            ShipControlAxis controlAxis,
            JoystickInputProvider inputProvider,
            System.Action changedCallback)
        {
            axis = controlAxis;
            provider = inputProvider;
            onChanged = changedCallback;

            RuntimeDropdownUtility.EnsureUsable(deviceDropdown);
            RuntimeDropdownUtility.EnsureUsable(axisDropdown);
            LayoutRowControls();
            ConfigureSlider(deadzoneSlider, 0f, 0.5f, 0.05f);
            ConfigureSlider(scaleSlider, 0.1f, 3f, 0.5f);
            ConfigureSlider(curveSlider, 0.25f, 4f, 1f);
            deadzoneValueText = EnsureValueText(deadzoneSlider, "DeadzoneValue");
            scaleValueText = EnsureValueText(scaleSlider, "GainValue");
            curveValueText = EnsureValueText(curveSlider, "CurveValue");
            LayoutValueText(deadzoneValueText);
            LayoutValueText(scaleValueText);
            LayoutValueText(curveValueText);

            if (labelText != null)
            {
                labelText.text = controlAxis.ToString();
            }

            deviceDropdown.onValueChanged.AddListener(_ => OnDeviceChanged());
            axisDropdown.onValueChanged.AddListener(_ => OnAxisChanged());
            invertToggle.onValueChanged.AddListener(_ => OnCalibrationChanged());
            deadzoneSlider.onValueChanged.AddListener(_ => OnCalibrationChanged());
            scaleSlider.onValueChanged.AddListener(_ => OnCalibrationChanged());
            curveSlider.onValueChanged.AddListener(_ => OnCalibrationChanged());
            RefreshCalibrationLabels();
        }

        public void RefreshDeviceList()
        {
            deviceDropdown.ClearOptions();
            var options = new System.Collections.Generic.List<string> { "None" };
            foreach (InputDevice device in provider.GetJoysticks())
            {
                options.Add(provider.GetDeviceLabel(device));
            }

            deviceDropdown.AddOptions(options);
            LoadFromBinding();
        }

        public void RefreshLiveReadout()
        {
            float value = 0f;
            int deviceIndex = deviceDropdown != null ? deviceDropdown.value : 0;
            int axisIndex = axisDropdown != null ? axisDropdown.value : 0;
            if (deviceIndex > 0 && provider != null)
            {
                var devices = provider.GetJoysticks();
                if (deviceIndex - 1 < devices.Count)
                {
                    InputDevice device = devices[deviceIndex - 1];
                    value = provider.ReadRawAxis(device, axisIndex);
                    HardwareAxisBinding binding = GetBinding();
                    value = InputCalibrationUtility.Apply(value, binding.calibration);
                }
            }
            else
            {
                value = provider != null ? provider.GetLiveAxisValue(GetBinding()) : 0f;
            }

            if (liveValueText != null)
            {
                liveValueText.text = $"{value:+0.00;-0.00;+0.00}";
            }

            if (liveBar != null)
            {
                liveBar.value = (value + 1f) * 0.5f;
            }

            RefreshCalibrationLabels();
        }

        void LoadFromBinding()
        {
            HardwareAxisBinding binding = GetBinding();
            int deviceIndex = 0;
            if (binding.enabled && !string.IsNullOrEmpty(binding.devicePath))
            {
                for (int i = 0; i < provider.GetJoysticks().Count; i++)
                {
                    if (provider.GetJoysticks()[i].path == binding.devicePath)
                    {
                        deviceIndex = i + 1;
                        break;
                    }
                }
            }

            deviceDropdown.SetValueWithoutNotify(deviceIndex);
            PopulateAxisDropdown(deviceIndex);
            int safeAxisIndex = Mathf.Clamp(binding.axisIndex, 0, Mathf.Max(0, axisDropdown.options.Count - 1));
            axisDropdown.SetValueWithoutNotify(safeAxisIndex);
            binding.axisIndex = safeAxisIndex;
            invertToggle.SetIsOnWithoutNotify(binding.calibration.invert);
            deadzoneSlider.SetValueWithoutNotify(binding.calibration.deadzone);
            scaleSlider.SetValueWithoutNotify(binding.calibration.scale);
            curveSlider.SetValueWithoutNotify(binding.calibration.curveExponent);
            RefreshCalibrationLabels();
        }

        void OnDeviceChanged()
        {
            HardwareAxisBinding binding = GetBinding();
            int index = deviceDropdown.value;
            if (index <= 0)
            {
                binding.enabled = false;
                binding.devicePath = null;
                binding.displayName = null;
            }
            else
            {
                var device = provider.GetJoysticks()[index - 1];
                binding.enabled = true;
                binding.devicePath = device.path;
                binding.displayName = device.displayName;
            }

            PopulateAxisDropdown(index);
            binding.axisIndex = Mathf.Clamp(binding.axisIndex, 0, Mathf.Max(0, axisDropdown.options.Count - 1));
            axisDropdown.SetValueWithoutNotify(binding.axisIndex);
            onChanged?.Invoke();
        }

        void OnAxisChanged()
        {
            GetBinding().axisIndex = axisDropdown.value;
            onChanged?.Invoke();
        }

        void OnCalibrationChanged()
        {
            HardwareAxisBinding binding = GetBinding();
            binding.calibration.invert = invertToggle.isOn;
            binding.calibration.deadzone = deadzoneSlider.value;
            binding.calibration.scale = scaleSlider.value;
            binding.calibration.curveExponent = curveSlider.value;
            RefreshCalibrationLabels();
            onChanged?.Invoke();
        }

        void PopulateAxisDropdown(int deviceIndex)
        {
            axisDropdown.ClearOptions();
            var options = new System.Collections.Generic.List<string>();
            if (deviceIndex > 0)
            {
                InputDevice device = provider.GetJoysticks()[deviceIndex - 1];
                int count = provider.GetAxisCount(device);
                for (int i = 0; i < count; i++)
                {
                    options.Add(provider.GetAxisControlLabel(device, i));
                }
            }

            if (options.Count == 0)
            {
                options.Add("No axis selected");
            }

            axisDropdown.AddOptions(options);
        }

        void RefreshCalibrationLabels()
        {
            if (deadzoneValueText != null && deadzoneSlider != null)
            {
                deadzoneValueText.text = $"DZ {deadzoneSlider.value:0.00}";
            }

            if (scaleValueText != null && scaleSlider != null)
            {
                scaleValueText.text = $"Gain {scaleSlider.value:0.00}";
            }

            if (curveValueText != null && curveSlider != null)
            {
                curveValueText.text = $"Exp {curveSlider.value:0.00}";
            }
        }

        void LayoutRowControls()
        {
            SetStretchTop(labelText != null ? labelText.rectTransform : null, 10f, 8f, 380f, 24f);
            SetStretchTop(deviceDropdown != null ? deviceDropdown.transform as RectTransform : null, 118f, 8f, 12f, 28f);
            SetStretchTop(axisDropdown != null ? axisDropdown.transform as RectTransform : null, 118f, 42f, 12f, 28f);
            SetStretchTop(invertToggle != null ? invertToggle.transform as RectTransform : null, 10f, 42f, 390f, 28f);
            SetStretchTop(deadzoneSlider != null ? deadzoneSlider.transform as RectTransform : null, 10f, 84f, 310f, 20f);
            SetStretchTop(scaleSlider != null ? scaleSlider.transform as RectTransform : null, 170f, 84f, 160f, 20f);
            SetStretchTop(curveSlider != null ? curveSlider.transform as RectTransform : null, 320f, 84f, 10f, 20f);
            SetStretchTop(liveValueText != null ? liveValueText.rectTransform : null, 10f, 72f, 390f, 20f);
            SetStretchTop(liveBar != null ? liveBar.transform as RectTransform : null, 74f, 74f, 310f, 16f);

            if (labelText != null)
            {
                labelText.alignment = TextAnchor.MiddleLeft;
                labelText.fontStyle = FontStyle.Bold;
            }
        }

        static void SetStretchTop(RectTransform rect, float left, float top, float right, float height)
        {
            if (rect == null)
            {
                return;
            }

            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.offsetMin = new Vector2(left, -top - height);
            rect.offsetMax = new Vector2(-right, -top);
        }

        static void LayoutValueText(Text text)
        {
            if (text == null)
            {
                return;
            }

            RectTransform rect = text.rectTransform;
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.offsetMin = new Vector2(0f, 18f);
            rect.offsetMax = new Vector2(0f, 34f);
        }

        static void ConfigureSlider(Slider slider, float min, float max, float fallback)
        {
            if (slider == null)
            {
                return;
            }

            slider.minValue = min;
            slider.maxValue = max;
            if (slider.value < min || slider.value > max || Mathf.Approximately(slider.value, 0f) && fallback > min)
            {
                slider.SetValueWithoutNotify(fallback);
            }
        }

        static Text EnsureValueText(Slider slider, string name)
        {
            if (slider == null)
            {
                return null;
            }

            Transform existing = slider.transform.Find(name);
            if (existing != null && existing.TryGetComponent(out Text existingText))
            {
                return existingText;
            }

            GameObject go = new(name, typeof(RectTransform), typeof(Text));
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.SetParent(slider.transform, false);
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, 14f);
            rect.sizeDelta = new Vector2(0f, 16f);

            Text text = go.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 10;
            text.color = new Color(0.82f, 0.88f, 0.95f);
            text.alignment = TextAnchor.MiddleCenter;
            return text;
        }

        HardwareAxisBinding GetBinding() => provider.Bindings.axisBindings[(int)axis];
    }
}
