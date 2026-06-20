using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.UI;

namespace FlightModel
{
    public class FireBindingRow : MonoBehaviour
    {
        const float ListenTimeoutSeconds = 5f;

        [SerializeField] Dropdown deviceDropdown;
        [SerializeField] Dropdown buttonDropdown;
        [SerializeField] Button bindButton;
        [SerializeField] Text liveText;

        JoystickInputProvider provider;
        System.Action onChanged;
        System.Func<HardwareButtonBinding> bindingGetter;
        string labelText = "Fire Primary";
        bool listening;
        float listenStartedAt;
        readonly System.Collections.Generic.HashSet<string> pressedAtListenStart = new();

        public void Initialize(JoystickInputProvider inputProvider, System.Action changedCallback)
            => Initialize("Fire Primary", inputProvider, () => inputProvider.Bindings.firePrimary, changedCallback);

        public void Initialize(
            string label,
            JoystickInputProvider inputProvider,
            System.Func<HardwareButtonBinding> getBinding,
            System.Action changedCallback)
        {
            labelText = label;
            provider = inputProvider;
            bindingGetter = getBinding;
            onChanged = changedCallback;
            EnsureControls();
            RuntimeDropdownUtility.EnsureUsable(deviceDropdown);
            RuntimeDropdownUtility.EnsureUsable(bindButton);
            if (buttonDropdown != null)
            {
                buttonDropdown.gameObject.SetActive(false);
            }

            LayoutRowControls();
            deviceDropdown.onValueChanged.AddListener(_ => OnDeviceChanged());
            bindButton.onClick.AddListener(BeginListening);
        }

        public void RefreshDeviceList()
        {
            EnsureControls();
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
            if (listening)
            {
                TickListener();
            }

            bool pressed = provider != null && provider.GetLiveButtonPressed(Binding);

            if (bindButton != null)
            {
                Text buttonText = bindButton.GetComponentInChildren<Text>(true);
                if (buttonText != null)
                {
                    buttonText.text = listening
                        ? $"Listening... {Mathf.CeilToInt(GetRemainingListenSeconds())}s"
                        : BindingLabel();
                }
            }

            if (liveText != null)
            {
                liveText.text = listening ? "PRESS A JOYSTICK BUTTON" : pressed ? "PRESSED" : "---";
                liveText.color = pressed ? new Color(1f, 0.85f, 0.35f) : new Color(0.75f, 0.8f, 0.88f);
            }
        }

        void LoadFromBinding()
        {
            HardwareButtonBinding binding = Binding;
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
            RefreshBindButtonLabel();
        }

        void OnDeviceChanged()
        {
            HardwareButtonBinding binding = Binding;
            int index = deviceDropdown.value;
            if (index <= 0)
            {
                binding.enabled = false;
                binding.devicePath = null;
                binding.displayName = null;
                binding.buttonIndex = 0;
            }
            else
            {
                InputDevice device = provider.GetJoysticks()[index - 1];
                binding.enabled = true;
                binding.devicePath = device.path;
                binding.displayName = device.displayName;
                binding.buttonIndex = -1;
            }

            listening = false;
            RefreshBindButtonLabel();
            onChanged?.Invoke();
        }

        void BeginListening()
        {
            if (provider == null)
            {
                return;
            }

            if (provider.GetJoysticks().Count == 0)
            {
                return;
            }

            listening = true;
            listenStartedAt = Time.unscaledTime;
            pressedAtListenStart.Clear();

            foreach (InputDevice device in GetListeningDevices())
            {
                var buttons = provider.GetButtonControls(device);
                for (int i = 0; i < buttons.Count; i++)
                {
                    if (buttons[i].isPressed)
                    {
                        pressedAtListenStart.Add(ButtonKey(device, i));
                    }
                }
            }

            RefreshBindButtonLabel();
        }

        void TickListener()
        {
            if (GetRemainingListenSeconds() <= 0f)
            {
                listening = false;
                RefreshBindButtonLabel();
                return;
            }

            foreach (InputDevice device in GetListeningDevices())
            {
                var buttons = provider.GetButtonControls(device);
                for (int i = 0; i < buttons.Count; i++)
                {
                    ButtonControl button = buttons[i];
                    string key = ButtonKey(device, i);
                    if (button.isPressed && !pressedAtListenStart.Contains(key))
                    {
                        HardwareButtonBinding binding = Binding;
                        binding.enabled = true;
                        binding.devicePath = device.path;
                        binding.displayName = device.displayName;
                        binding.buttonIndex = i;
                        SelectDevice(device);
                        listening = false;
                        RefreshBindButtonLabel();
                        onChanged?.Invoke();
                        return;
                    }
                }
            }
        }

        void LayoutRowControls()
        {
            SetStretchTop(deviceDropdown != null ? deviceDropdown.transform as RectTransform : null, 12f, 48f, 12f, 42f);
            SetStretchTop(bindButton != null ? bindButton.transform as RectTransform : null, 12f, 96f, 12f, 42f);
            SetStretchTop(liveText != null ? liveText.rectTransform : null, 12f, 150f, 12f, 34f);

            Text label = EnsureLabel();
            label.text = labelText;
            label.fontStyle = FontStyle.Bold;
            label.fontSize = 18;
            SetStretchTop(label.rectTransform, 12f, 10f, 12f, 28f);

            if (liveText != null)
            {
                liveText.fontSize = 18;
                liveText.alignment = TextAnchor.MiddleCenter;
            }
        }

        void EnsureControls()
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            if (deviceDropdown == null)
            {
                deviceDropdown = CreateDropdown("DeviceDropdown", font);
            }

            if (bindButton == null)
            {
                bindButton = CreateButton("BindButton", font);
            }

            if (liveText == null)
            {
                GameObject liveGo = new("LiveText", typeof(RectTransform), typeof(Text));
                liveGo.transform.SetParent(transform, false);
                liveText = liveGo.GetComponent<Text>();
                liveText.font = font;
                liveText.color = Color.white;
            }
        }

        Dropdown CreateDropdown(string objectName, Font font)
        {
            GameObject go = new(objectName, typeof(RectTransform), typeof(Image), typeof(Dropdown));
            go.transform.SetParent(transform, false);
            Dropdown dropdown = go.GetComponent<Dropdown>();
            RuntimeDropdownUtility.EnsureUsable(dropdown);
            return dropdown;
        }

        Button CreateButton(string objectName, Font font)
        {
            GameObject go = new(objectName, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(transform, false);
            Image image = go.GetComponent<Image>();
            image.color = new Color(0.12f, 0.16f, 0.21f, 0.98f);

            GameObject textGo = new("Text", typeof(RectTransform), typeof(Text));
            textGo.transform.SetParent(go.transform, false);
            RectTransform textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(8f, 0f);
            textRect.offsetMax = new Vector2(-8f, 0f);

            Text text = textGo.GetComponent<Text>();
            text.font = font;
            text.fontSize = 16;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleCenter;

            Button button = go.GetComponent<Button>();
            RuntimeDropdownUtility.EnsureUsable(button);
            return button;
        }

        HardwareButtonBinding Binding => bindingGetter != null ? bindingGetter() : provider.Bindings.firePrimary;

        string BindingLabel()
        {
            HardwareButtonBinding binding = Binding;
            if (binding == null || !binding.enabled)
            {
                return "Bind Button";
            }

            InputDevice device = FindSelectedOrBoundDevice(binding);
            return device != null
                ? provider.GetButtonControlLabel(device, binding.buttonIndex)
                : $"Button {binding.buttonIndex}";
        }

        InputDevice FindSelectedOrBoundDevice(HardwareButtonBinding binding)
        {
            int selected = deviceDropdown != null ? deviceDropdown.value : 0;
            if (selected > 0 && selected - 1 < provider.GetJoysticks().Count)
            {
                return provider.GetJoysticks()[selected - 1];
            }

            foreach (InputDevice device in provider.GetJoysticks())
            {
                if (device.path == binding.devicePath || device.displayName == binding.displayName)
                {
                    return device;
                }
            }

            return null;
        }

        void RefreshBindButtonLabel()
        {
            if (bindButton == null)
            {
                return;
            }

            Text text = bindButton.GetComponentInChildren<Text>(true);
            if (text != null)
            {
                text.text = listening ? $"Listening... {Mathf.CeilToInt(GetRemainingListenSeconds())}s" : BindingLabel();
            }
        }

        float GetRemainingListenSeconds() => ListenTimeoutSeconds - (Time.unscaledTime - listenStartedAt);

        System.Collections.Generic.IEnumerable<InputDevice> GetListeningDevices()
        {
            int selected = deviceDropdown != null ? deviceDropdown.value : 0;
            if (selected > 0 && selected - 1 < provider.GetJoysticks().Count)
            {
                yield return provider.GetJoysticks()[selected - 1];
                yield break;
            }

            foreach (InputDevice device in provider.GetJoysticks())
            {
                yield return device;
            }
        }

        void SelectDevice(InputDevice selectedDevice)
        {
            if (deviceDropdown == null || selectedDevice == null)
            {
                return;
            }

            for (int i = 0; i < provider.GetJoysticks().Count; i++)
            {
                InputDevice device = provider.GetJoysticks()[i];
                if (device.path == selectedDevice.path)
                {
                    deviceDropdown.SetValueWithoutNotify(i + 1);
                    return;
                }
            }
        }

        static string ButtonKey(InputDevice device, int buttonIndex) => $"{device.path}:{buttonIndex}";

        Text EnsureLabel()
        {
            Transform existing = transform.Find("FireRowLabel");
            if (existing != null && existing.TryGetComponent(out Text existingText))
            {
                return existingText;
            }

            GameObject go = new("FireRowLabel", typeof(RectTransform), typeof(Text));
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.SetParent(transform, false);

            Text text = go.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 18;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleLeft;
            return text;
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
    }
}
