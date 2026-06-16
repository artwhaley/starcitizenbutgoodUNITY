using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace FlightModel
{
    public class FireBindingRow : MonoBehaviour
    {
        [SerializeField] Dropdown deviceDropdown;
        [SerializeField] Dropdown buttonDropdown;
        [SerializeField] Text liveText;

        JoystickInputProvider provider;
        System.Action onChanged;

        public void Initialize(JoystickInputProvider inputProvider, System.Action changedCallback)
        {
            provider = inputProvider;
            onChanged = changedCallback;
            RuntimeDropdownUtility.EnsureUsable(deviceDropdown);
            RuntimeDropdownUtility.EnsureUsable(buttonDropdown);
            LayoutRowControls();
            deviceDropdown.onValueChanged.AddListener(_ => OnDeviceChanged());
            buttonDropdown.onValueChanged.AddListener(_ => OnButtonChanged());
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
            bool pressed = false;
            int deviceIndex = deviceDropdown != null ? deviceDropdown.value : 0;
            int buttonIndex = buttonDropdown != null ? buttonDropdown.value : 0;
            if (deviceIndex > 0 && provider != null)
            {
                var devices = provider.GetJoysticks();
                if (deviceIndex - 1 < devices.Count)
                {
                    InputDevice device = devices[deviceIndex - 1];
                    pressed = provider.ReadRawButton(device, buttonIndex);
                }
            }
            else if (provider != null)
            {
                pressed = provider.GetLiveButtonPressed(provider.Bindings.firePrimary);
            }

            if (liveText != null)
            {
                liveText.text = pressed ? "PRESSED" : "---";
                liveText.color = pressed ? new Color(1f, 0.85f, 0.35f) : new Color(0.75f, 0.8f, 0.88f);
            }
        }

        void LoadFromBinding()
        {
            HardwareButtonBinding binding = provider.Bindings.firePrimary;
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
            PopulateButtonDropdown(deviceIndex);
            int safeButtonIndex = Mathf.Clamp(binding.buttonIndex, 0, Mathf.Max(0, buttonDropdown.options.Count - 1));
            buttonDropdown.SetValueWithoutNotify(safeButtonIndex);
            binding.buttonIndex = safeButtonIndex;
        }

        void OnDeviceChanged()
        {
            HardwareButtonBinding binding = provider.Bindings.firePrimary;
            int index = deviceDropdown.value;
            if (index <= 0)
            {
                binding.enabled = false;
                binding.devicePath = null;
                binding.displayName = null;
            }
            else
            {
                InputDevice device = provider.GetJoysticks()[index - 1];
                binding.enabled = true;
                binding.devicePath = device.path;
                binding.displayName = device.displayName;
            }

            PopulateButtonDropdown(index);
            binding.buttonIndex = Mathf.Clamp(binding.buttonIndex, 0, Mathf.Max(0, buttonDropdown.options.Count - 1));
            buttonDropdown.SetValueWithoutNotify(binding.buttonIndex);
            onChanged?.Invoke();
        }

        void OnButtonChanged()
        {
            provider.Bindings.firePrimary.buttonIndex = buttonDropdown.value;
            onChanged?.Invoke();
        }

        void PopulateButtonDropdown(int deviceIndex)
        {
            buttonDropdown.ClearOptions();
            var options = new System.Collections.Generic.List<string>();
            if (deviceIndex > 0)
            {
                InputDevice device = provider.GetJoysticks()[deviceIndex - 1];
                int count = provider.GetButtonCount(device);
                for (int i = 0; i < count; i++)
                {
                    options.Add(provider.GetButtonControlLabel(device, i));
                }
            }

            if (options.Count == 0)
            {
                options.Add("No button selected");
            }

            buttonDropdown.AddOptions(options);
        }

        void LayoutRowControls()
        {
            SetStretchTop(deviceDropdown != null ? deviceDropdown.transform as RectTransform : null, 12f, 48f, 12f, 42f);
            SetStretchTop(buttonDropdown != null ? buttonDropdown.transform as RectTransform : null, 12f, 96f, 12f, 42f);
            SetStretchTop(liveText != null ? liveText.rectTransform : null, 12f, 150f, 12f, 34f);

            Text label = EnsureLabel();
            label.text = "Fire Primary";
            label.fontStyle = FontStyle.Bold;
            label.fontSize = 18;
            SetStretchTop(label.rectTransform, 12f, 10f, 12f, 28f);

            if (liveText != null)
            {
                liveText.fontSize = 18;
                liveText.alignment = TextAnchor.MiddleCenter;
            }
        }

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
