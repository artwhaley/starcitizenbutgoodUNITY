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
        Text rawValueText;
        Text deadzoneLabelText;
        Text scaleLabelText;
        Text curveLabelText;
        Text deadzoneValueText;
        Text scaleValueText;
        Text curveValueText;
        InputField deadzoneInput;
        InputField scaleInput;
        InputField curveInput;
        bool syncingControls;

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
            RuntimeDropdownUtility.EnsureReadableToggle(invertToggle);
            RuntimeDropdownUtility.EnsureUsable(deadzoneSlider);
            RuntimeDropdownUtility.EnsureUsable(scaleSlider);
            RuntimeDropdownUtility.EnsureUsable(curveSlider);
            ConfigureSlider(deadzoneSlider, 0f, 0.5f, 0.05f);
            ConfigureSlider(scaleSlider, 0.1f, 3f, 0.5f);
            ConfigureSlider(curveSlider, 0.25f, 4f, 1f);
            rawValueText = EnsureText("RawValue", 16, TextAnchor.MiddleLeft);
            deadzoneLabelText = EnsureText("DeadzoneLabel", 16, TextAnchor.MiddleLeft);
            scaleLabelText = EnsureText("GainLabel", 16, TextAnchor.MiddleLeft);
            curveLabelText = EnsureText("ExponentLabel", 16, TextAnchor.MiddleLeft);
            deadzoneValueText = EnsureText("DeadzoneValue", 16, TextAnchor.MiddleRight);
            scaleValueText = EnsureText("GainValue", 16, TextAnchor.MiddleRight);
            curveValueText = EnsureText("ExponentValue", 16, TextAnchor.MiddleRight);
            deadzoneInput = EnsureNumberInput("DeadzoneInput");
            scaleInput = EnsureNumberInput("GainInput");
            curveInput = EnsureNumberInput("CurveInput");
            LayoutRowControls();
            LayoutCalibrationControl(deadzoneLabelText, "Deadzone", deadzoneSlider, deadzoneValueText, deadzoneInput, 116f);
            LayoutCalibrationControl(scaleLabelText, "Gain", scaleSlider, scaleValueText, scaleInput, 156f);
            LayoutCalibrationControl(curveLabelText, "Exponent", curveSlider, curveValueText, curveInput, 196f);

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
            deadzoneInput.onEndEdit.AddListener(text => OnCalibrationInputChanged(deadzoneSlider, text));
            scaleInput.onEndEdit.AddListener(text => OnCalibrationInputChanged(scaleSlider, text));
            curveInput.onEndEdit.AddListener(text => OnCalibrationInputChanged(curveSlider, text));
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
            float raw = 0f;
            float calibrated = 0f;
            int deviceIndex = deviceDropdown != null ? deviceDropdown.value : 0;
            int axisIndex = axisDropdown != null ? axisDropdown.value : 0;
            if (deviceIndex > 0 && provider != null)
            {
                var devices = provider.GetJoysticks();
                if (deviceIndex - 1 < devices.Count)
                {
                    InputDevice device = devices[deviceIndex - 1];
                    raw = provider.ReadRawAxis(device, axisIndex);
                    HardwareAxisBinding binding = GetBinding();
                    calibrated = InputCalibrationUtility.Apply(raw, binding.calibration);
                }
            }
            else
            {
                calibrated = provider != null ? provider.GetLiveAxisValue(GetBinding()) : 0f;
            }

            if (rawValueText != null)
            {
                rawValueText.text = $"Raw {raw:+0.000;-0.000;+0.000}";
            }

            if (liveValueText != null)
            {
                liveValueText.text = $"Out {calibrated:+0.000;-0.000;+0.000}";
            }

            if (liveBar != null)
            {
                liveBar.value = (calibrated + 1f) * 0.5f;
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
            syncingControls = true;
            invertToggle.SetIsOnWithoutNotify(binding.calibration.invert);
            deadzoneSlider.SetValueWithoutNotify(binding.calibration.deadzone);
            scaleSlider.SetValueWithoutNotify(binding.calibration.scale);
            curveSlider.SetValueWithoutNotify(binding.calibration.curveExponent);
            syncingControls = false;
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
            if (syncingControls)
            {
                return;
            }

            HardwareAxisBinding binding = GetBinding();
            binding.calibration.invert = invertToggle.isOn;
            binding.calibration.deadzone = deadzoneSlider.value;
            binding.calibration.scale = scaleSlider.value;
            binding.calibration.curveExponent = curveSlider.value;
            RefreshCalibrationLabels();
            onChanged?.Invoke();
        }

        void OnCalibrationInputChanged(Slider slider, string text)
        {
            if (slider == null || !float.TryParse(text, out float value))
            {
                RefreshCalibrationLabels();
                return;
            }

            slider.value = Mathf.Clamp(value, slider.minValue, slider.maxValue);
            OnCalibrationChanged();
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
                deadzoneValueText.text = $"{deadzoneSlider.value:0.00}";
            }

            if (deadzoneInput != null && deadzoneSlider != null && !deadzoneInput.isFocused)
            {
                deadzoneInput.SetTextWithoutNotify($"{deadzoneSlider.value:0.00}");
            }

            if (scaleValueText != null && scaleSlider != null)
            {
                scaleValueText.text = $"{scaleSlider.value:0.00}";
            }

            if (scaleInput != null && scaleSlider != null && !scaleInput.isFocused)
            {
                scaleInput.SetTextWithoutNotify($"{scaleSlider.value:0.00}");
            }

            if (curveValueText != null && curveSlider != null)
            {
                curveValueText.text = $"{curveSlider.value:0.00}";
            }

            if (curveInput != null && curveSlider != null && !curveInput.isFocused)
            {
                curveInput.SetTextWithoutNotify($"{curveSlider.value:0.00}");
            }
        }

        void LayoutRowControls()
        {
            SetStretchTop(labelText != null ? labelText.rectTransform : null, 12f, 10f, 12f, 28f);
            SetStretchTop(deviceDropdown != null ? deviceDropdown.transform as RectTransform : null, 12f, 44f, 12f, 40f);
            SetStretchTop(axisDropdown != null ? axisDropdown.transform as RectTransform : null, 12f, 88f, 12f, 40f);
            SetStretchTop(rawValueText != null ? rawValueText.rectTransform : null, 12f, 132f, 220f, 24f);
            SetStretchTop(liveValueText != null ? liveValueText.rectTransform : null, 220f, 132f, 12f, 24f);
            SetStretchTop(invertToggle != null ? invertToggle.transform as RectTransform : null, 12f, 166f, 12f, 34f);

            if (labelText != null)
            {
                labelText.alignment = TextAnchor.MiddleLeft;
                labelText.fontStyle = FontStyle.Bold;
                labelText.fontSize = 18;
                labelText.color = Color.white;
            }

            if (liveValueText != null)
            {
                liveValueText.fontSize = 16;
                liveValueText.alignment = TextAnchor.MiddleRight;
                liveValueText.color = new Color(0.72f, 1f, 0.86f);
            }

            HideLiveBar();
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

        void LayoutCalibrationControl(Text labelText, string label, Slider slider, Text valueText, InputField input, float top)
        {
            SetStretchTop(labelText != null ? labelText.rectTransform : null, 12f, top, 330f, 34f);
            SetStretchTop(input != null ? input.transform as RectTransform : null, 116f, top, 264f, 34f);
            SetStretchTop(slider != null ? slider.transform as RectTransform : null, 220f, top + 8f, 78f, 18f);
            SetStretchTop(valueText != null ? valueText.rectTransform : null, 0f, top, 12f, 34f);

            if (labelText != null)
            {
                labelText.text = label;
                labelText.fontSize = 16;
                labelText.fontStyle = FontStyle.Bold;
                labelText.color = Color.white;
            }

            if (input != null)
            {
                Text text = input.textComponent;
                if (text != null)
                {
                    text.fontStyle = FontStyle.Bold;
                }
            }

            if (valueText != null)
            {
                valueText.fontSize = 16;
                valueText.alignment = TextAnchor.MiddleRight;
                valueText.color = new Color(0.76f, 0.86f, 0.95f);
            }
        }

        static void ConfigureSlider(Slider slider, float min, float max, float fallback)
        {
            if (slider == null)
            {
                return;
            }

            slider.minValue = min;
            slider.maxValue = max;
            slider.wholeNumbers = false;
            if (slider.value < min || slider.value > max || Mathf.Approximately(slider.value, 0f) && fallback > min)
            {
                slider.SetValueWithoutNotify(fallback);
            }
        }

        void HideLiveBar()
        {
            if (liveBar == null)
            {
                return;
            }

            liveBar.gameObject.SetActive(false);
        }

        Text EnsureText(string name, int fontSize, TextAnchor alignment)
        {
            Transform existing = transform.Find(name);
            if (existing != null && existing.TryGetComponent(out Text existingText))
            {
                existingText.fontSize = fontSize;
                existingText.alignment = alignment;
                existingText.color = Color.white;
                return existingText;
            }

            GameObject go = new(name, typeof(RectTransform), typeof(Text));
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.SetParent(transform, false);

            Text text = go.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.color = Color.white;
            text.alignment = alignment;
            return text;
        }

        InputField EnsureNumberInput(string name)
        {
            Transform existing = transform.Find(name);
            if (existing != null && existing.TryGetComponent(out InputField existingInput))
            {
                RuntimeDropdownUtility.EnsureUsable(existingInput);
                return existingInput;
            }

            GameObject go = new(name, typeof(RectTransform), typeof(Image), typeof(InputField));
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.SetParent(transform, false);

            Text text = CreateChildText(rect, "Text", 16, TextAnchor.MiddleCenter);
            Stretch(text.rectTransform, 6f, 2f, 6f, 2f);

            Text placeholder = CreateChildText(rect, "Placeholder", 16, TextAnchor.MiddleCenter);
            placeholder.text = "0.00";
            placeholder.color = new Color(0.65f, 0.7f, 0.78f, 0.85f);
            Stretch(placeholder.rectTransform, 6f, 2f, 6f, 2f);

            InputField input = go.GetComponent<InputField>();
            input.textComponent = text;
            input.placeholder = placeholder;
            input.contentType = InputField.ContentType.DecimalNumber;
            input.lineType = InputField.LineType.SingleLine;
            RuntimeDropdownUtility.EnsureUsable(input);
            return input;
        }

        static Text CreateChildText(RectTransform parent, string name, int fontSize, TextAnchor alignment)
        {
            GameObject go = new(name, typeof(RectTransform), typeof(Text));
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);

            Text text = go.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.color = Color.white;
            text.alignment = alignment;
            return text;
        }

        static void Stretch(RectTransform rect, float left, float top, float right, float bottom)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }

        HardwareAxisBinding GetBinding() => provider.Bindings.axisBindings[(int)axis];
    }
}
