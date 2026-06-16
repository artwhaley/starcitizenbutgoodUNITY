using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace FlightModel
{
    public class JoystickProbeMonitor : MonoBehaviour
    {
        class DeviceRowUi
        {
            public InputDevice device;
            public Text header;
            public Text[] axisTexts;
            public Text[] buttonTexts;
        }

        readonly List<DeviceRowUi> rows = new();
        JoystickInputProvider provider;
        RectTransform rowsParent;

        public void Initialize(JoystickInputProvider inputProvider, RectTransform parent)
        {
            provider = inputProvider;
            BuildShell(parent);
            RefreshDeviceList();
        }

        public void RefreshDeviceList()
        {
            if (provider == null || rowsParent == null)
            {
                return;
            }

            for (int i = rowsParent.childCount - 1; i >= 0; i--)
            {
                Destroy(rowsParent.GetChild(i).gameObject);
            }

            rows.Clear();

            IReadOnlyList<InputDevice> devices = provider.GetJoysticks();
            if (devices.Count == 0)
            {
                var empty = CreateText(rowsParent, "No joysticks detected - connect hardware and press Refresh Devices.", 14);
                empty.color = new Color(1f, 0.7f, 0.5f);
                rows.Add(new DeviceRowUi { header = empty });
                return;
            }

            foreach (InputDevice device in devices)
            {
                rows.Add(CreateDeviceRow(device));
            }
        }

        public void RefreshLiveReadout()
        {
            foreach (DeviceRowUi row in rows)
            {
                if (row.device == null)
                {
                    continue;
                }

                int axisCount = provider.GetAxisCount(row.device);
                for (int i = 0; i < axisCount; i++)
                {
                    if (row.axisTexts == null || i >= row.axisTexts.Length || row.axisTexts[i] == null)
                    {
                        continue;
                    }

                    float value = provider.ReadRawAxis(row.device, i);
                    row.axisTexts[i].text = $"{value:+0.0;-0.0;+0.0}";
                    row.axisTexts[i].color = Mathf.Abs(value) > 0.08f
                        ? new Color(0.55f, 1f, 0.75f)
                        : new Color(0.8f, 0.8f, 0.8f);
                }

                int buttonCount = provider.GetButtonCount(row.device);
                for (int i = 0; i < buttonCount; i++)
                {
                    if (row.buttonTexts == null || i >= row.buttonTexts.Length || row.buttonTexts[i] == null)
                    {
                        continue;
                    }

                    bool pressed = provider.ReadRawButton(row.device, i);
                    row.buttonTexts[i].text = pressed ? "ON" : "--";
                    row.buttonTexts[i].color = pressed
                        ? new Color(1f, 0.85f, 0.35f)
                        : new Color(0.65f, 0.65f, 0.65f);
                }
            }
        }

        DeviceRowUi CreateDeviceRow(InputDevice device)
        {
            var row = new DeviceRowUi { device = device };
            row.header = CreateText(rowsParent, provider.GetDeviceLabel(device), 12);
            row.header.fontStyle = FontStyle.Bold;

            var axisLine = CreateRowContainer(rowsParent);
            int axisCount = provider.GetAxisCount(device);
            row.axisTexts = new Text[axisCount];
            for (int i = 0; i < axisCount; i++)
            {
                CreateText(axisLine, $"A{i}", 10, 24f).color = new Color(0.55f, 0.55f, 0.55f);
                row.axisTexts[i] = CreateText(axisLine, "0.0", 11, 44f);
                row.axisTexts[i].alignment = TextAnchor.MiddleCenter;
            }

            var buttonLine = CreateRowContainer(rowsParent);
            int buttonCount = provider.GetButtonCount(device);
            row.buttonTexts = new Text[buttonCount];
            for (int i = 0; i < buttonCount; i++)
            {
                CreateText(buttonLine, $"B{i}", 9, 30f).color = new Color(0.55f, 0.55f, 0.55f);
                row.buttonTexts[i] = CreateText(buttonLine, "--", 10, 30f);
                row.buttonTexts[i].alignment = TextAnchor.MiddleCenter;
            }

            return row;
        }

        void BuildShell(RectTransform parent)
        {
            var rootRect = gameObject.AddComponent<RectTransform>();
            rootRect.SetParent(parent, false);
            rootRect.anchorMin = new Vector2(0f, 1f);
            rootRect.anchorMax = new Vector2(1f, 1f);
            rootRect.pivot = new Vector2(0.5f, 1f);
            rootRect.sizeDelta = new Vector2(0f, 0f);

            LayoutElement layout = gameObject.AddComponent<LayoutElement>();
            layout.flexibleWidth = 1f;

            var fitter = gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var group = gameObject.AddComponent<VerticalLayoutGroup>();
            group.spacing = 4f;
            group.padding = new RectOffset(4, 4, 4, 8);
            group.childAlignment = TextAnchor.UpperLeft;
            group.childControlWidth = true;
            group.childControlHeight = true;
            group.childForceExpandWidth = true;
            group.childForceExpandHeight = false;

            CreateText(rootRect, "DEVICE PROBE - wriggle sticks/buttons; lit axes/buttons are active", 13);

            var rowsGo = new GameObject("Rows", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            rowsParent = rowsGo.GetComponent<RectTransform>();
            rowsParent.SetParent(rootRect, false);
            var rowsLayout = rowsGo.GetComponent<VerticalLayoutGroup>();
            rowsLayout.spacing = 8f;
            rowsLayout.childControlWidth = true;
            rowsLayout.childControlHeight = true;
            rowsLayout.childForceExpandWidth = true;
            rowsGo.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        static RectTransform CreateRowContainer(RectTransform parent)
        {
            var go = new GameObject("Row", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            HorizontalLayoutGroup layout = go.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 2f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = false;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            go.GetComponent<LayoutElement>().minHeight = 16f;
            return rect;
        }

        static Text CreateText(RectTransform parent, string text, int fontSize, float width = 0f)
        {
            var go = new GameObject("Text", typeof(RectTransform), typeof(Text), typeof(LayoutElement));
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            LayoutElement layout = go.GetComponent<LayoutElement>();
            if (width > 0f)
            {
                layout.preferredWidth = width;
                layout.minWidth = width;
            }
            else
            {
                layout.flexibleWidth = 1f;
            }

            Text label = go.GetComponent<Text>();
            label.text = text;
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = fontSize;
            label.color = Color.white;
            label.alignment = TextAnchor.MiddleLeft;
            return label;
        }
    }
}
