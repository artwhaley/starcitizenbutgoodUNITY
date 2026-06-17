using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace FlightModel
{
    public class FlightTuningOverlay : MonoBehaviour
    {
        struct TuningFieldBinding
        {
            public string label;
            public System.Func<ShipTuning, float> getter;
            public System.Action<ShipTuning, float> setter;
            public float min;
            public float max;
        }

        [SerializeField] GameObject root;
        [SerializeField] Text profileLabel;
        [SerializeField] Dropdown profileDropdown;
        [SerializeField] Button applyButton;
        [SerializeField] Button saveButton;
        [SerializeField] Button resetButton;
        [SerializeField] RectTransform fieldsContainer;

        ShipFlightController flight;
        ShipTuning sourceAsset;
        ShipTuning runtimeCopy;
        ShipTuningProfileLibrary library;
        readonly List<TuningFieldBinding> fieldBindings = new();
        readonly List<InputField> fieldInputs = new();

        public void Initialize(ShipFlightController controller, ShipTuning asset, ShipTuningProfileLibrary profileLibrary)
        {
            flight = controller;
            sourceAsset = asset;
            library = profileLibrary;
            runtimeCopy = Instantiate(asset);
            flight.Tuning = runtimeCopy;
            ShipTuningJsonStore.TryLoad(runtimeCopy, ShipTuningJsonStore.GetProfileKey(sourceAsset));

            BuildFieldBindings();
            BuildFieldUi();
            PopulateProfiles();
            BindUi();
            SyncFieldsFromRuntime();
            Hide();
        }

        public void Toggle()
        {
            if (IsVisible)
            {
                Hide();
            }
            else
            {
                Show();
            }
        }

        public void Show()
        {
            if (root != null)
            {
                root.SetActive(true);
            }
        }

        public void Hide()
        {
            if (root != null)
            {
                root.SetActive(false);
            }
        }

        public bool IsVisible => root != null && root.activeSelf;

        void BuildFieldBindings()
        {
            fieldBindings.Clear();
            AddField("Dry Mass (kg)", t => t.dryMassKg, (t, v) => t.dryMassKg = v, 1000f, 50000f);
            AddField("Main Engine Forward Accel", t => t.mainEngineForwardAccel, (t, v) => t.mainEngineForwardAccel = v, 50f, 2000f);
            AddField("Maneuver Forward Accel", t => t.maneuverForwardAccel, (t, v) => t.maneuverForwardAccel = v, 50f, 2000f);
            AddField("Reverse Accel", t => t.reverseAccel, (t, v) => t.reverseAccel = v, 50f, 2000f);
            AddField("Right Accel", t => t.rightAccel, (t, v) => t.rightAccel = v, 50f, 1500f);
            AddField("Left Accel", t => t.leftAccel, (t, v) => t.leftAccel = v, 50f, 1500f);
            AddField("Up Accel", t => t.upAccel, (t, v) => t.upAccel = v, 50f, 1500f);
            AddField("Down Accel", t => t.downAccel, (t, v) => t.downAccel = v, 50f, 1500f);
            AddField("Pitch + Accel", t => t.pitchPositiveAccel, (t, v) => t.pitchPositiveAccel = v, 0.5f, 20f);
            AddField("Pitch - Accel", t => t.pitchNegativeAccel, (t, v) => t.pitchNegativeAccel = v, 0.5f, 20f);
            AddField("Yaw + Accel", t => t.yawPositiveAccel, (t, v) => t.yawPositiveAccel = v, 0.5f, 20f);
            AddField("Yaw - Accel", t => t.yawNegativeAccel, (t, v) => t.yawNegativeAccel = v, 0.5f, 20f);
            AddField("Roll + Accel", t => t.rollPositiveAccel, (t, v) => t.rollPositiveAccel = v, 0.5f, 20f);
            AddField("Roll - Accel", t => t.rollNegativeAccel, (t, v) => t.rollNegativeAccel = v, 0.5f, 20f);
            AddField("Max Linear Speed", t => t.maxLinearSpeedMps, (t, v) => t.maxLinearSpeedMps = v, 50f, 3000f);
            AddField("Boost Max Linear Speed", t => t.boostMaxLinearSpeedMps, (t, v) => t.boostMaxLinearSpeedMps = v, 50f, 4000f);
            AddField("Boost Accel Multiplier", t => t.boostAccelMultiplier, (t, v) => t.boostAccelMultiplier = v, 1f, 4f);
            AddField("Max Pitch Speed (rad/s)", t => t.maxPitchSpeedRad, (t, v) => t.maxPitchSpeedRad = v, 0.5f, 8f);
            AddField("Max Yaw Speed (rad/s)", t => t.maxYawSpeedRad, (t, v) => t.maxYawSpeedRad = v, 0.5f, 8f);
            AddField("Max Roll Speed (rad/s)", t => t.maxRollSpeedRad, (t, v) => t.maxRollSpeedRad = v, 0.5f, 8f);
            AddField("Boost Angular Speed Mult", t => t.boostAngularSpeedMultiplier, (t, v) => t.boostAngularSpeedMultiplier = v, 1f, 4f);
            AddField("Fine Control Linear Accel Mult", t => t.fineControlLinearAccelMultiplier, (t, v) => t.fineControlLinearAccelMultiplier = v, 0.05f, 1f);
            AddField("Fine Control Max Speed", t => t.fineControlMaxLinearSpeedMps, (t, v) => t.fineControlMaxLinearSpeedMps = v, 5f, 300f);
            AddField("Fine Control Angular Accel Mult", t => t.fineControlAngularAccelMultiplier, (t, v) => t.fineControlAngularAccelMultiplier = v, 0.05f, 1f);
            AddField("Fuel Capacity (kg)", t => t.fuelCapacityKg, (t, v) => t.fuelCapacityKg = v, 0f, 50000f);
            AddField("Hypergolic Capacity (kg)", t => t.hypergolicCapacityKg, (t, v) => t.hypergolicCapacityKg = v, 0f, 10000f);
            AddField("Fuel Burn / N-s", t => t.fuelBurnRatePerNewtonSecond, (t, v) => t.fuelBurnRatePerNewtonSecond = v, 0f, 0.001f);
            AddField("Hypergolic Burn / N-s", t => t.hypergolicBurnRatePerNewtonSecond, (t, v) => t.hypergolicBurnRatePerNewtonSecond = v, 0f, 0.001f);
            AddField("Attitude Assist Response", t => t.attitudeAssistResponsiveness, (t, v) => t.attitudeAssistResponsiveness = v, 0f, 10f);
            AddField("Coupled Assist Response", t => t.coupledAssistResponsiveness, (t, v) => t.coupledAssistResponsiveness = v, 0f, 10f);
            AddField("Frame Lock Assist Response", t => t.frameLockAssistResponsiveness, (t, v) => t.frameLockAssistResponsiveness = v, 0f, 10f);
            AddField("Brake Response", t => t.brakeResponsiveness, (t, v) => t.brakeResponsiveness = v, 0f, 10f);
        }

        void AddField(string label, System.Func<ShipTuning, float> getter, System.Action<ShipTuning, float> setter, float min, float max)
        {
            fieldBindings.Add(new TuningFieldBinding
            {
                label = label,
                getter = getter,
                setter = setter,
                min = min,
                max = max
            });
        }

        void BuildFieldUi()
        {
            fieldInputs.Clear();
            RectTransform container = fieldsContainer != null ? fieldsContainer : root?.GetComponent<RectTransform>();
            if (container == null)
            {
                return;
            }

            for (int i = container.childCount - 1; i >= 0; i--)
            {
                Transform child = container.GetChild(i);
                if (child.name.StartsWith("TuningField_"))
                {
                    Destroy(child.gameObject);
                }
            }

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            float y = -10f;
            for (int i = 0; i < fieldBindings.Count; i++)
            {
                TuningFieldBinding binding = fieldBindings[i];
                var row = new GameObject($"TuningField_{i}", typeof(RectTransform));
                row.transform.SetParent(container, false);
                var rowRect = row.GetComponent<RectTransform>();
                rowRect.anchorMin = new Vector2(0f, 1f);
                rowRect.anchorMax = new Vector2(1f, 1f);
                rowRect.pivot = new Vector2(0f, 1f);
                rowRect.anchoredPosition = new Vector2(10f, y);
                rowRect.sizeDelta = new Vector2(-20f, 22f);

                var labelGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
                labelGo.transform.SetParent(row.transform, false);
                var labelRect = labelGo.GetComponent<RectTransform>();
                labelRect.anchorMin = new Vector2(0f, 0f);
                labelRect.anchorMax = new Vector2(0.62f, 1f);
                labelRect.offsetMin = Vector2.zero;
                labelRect.offsetMax = Vector2.zero;
                var labelText = labelGo.GetComponent<Text>();
                labelText.font = font;
                labelText.fontSize = 12;
                labelText.alignment = TextAnchor.MiddleLeft;
                labelText.color = Color.white;
                labelText.text = binding.label;

                var inputGo = new GameObject("Input", typeof(RectTransform), typeof(Image), typeof(InputField));
                inputGo.transform.SetParent(row.transform, false);
                var inputRect = inputGo.GetComponent<RectTransform>();
                inputRect.anchorMin = new Vector2(0.64f, 0f);
                inputRect.anchorMax = new Vector2(1f, 1f);
                inputRect.offsetMin = Vector2.zero;
                inputRect.offsetMax = Vector2.zero;
                inputGo.GetComponent<Image>().color = new Color(0.12f, 0.12f, 0.12f, 0.9f);

                var textGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
                textGo.transform.SetParent(inputGo.transform, false);
                var textRect = textGo.GetComponent<RectTransform>();
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.offsetMin = new Vector2(4f, 0f);
                textRect.offsetMax = new Vector2(-4f, 0f);
                var text = textGo.GetComponent<Text>();
                text.font = font;
                text.fontSize = 12;
                text.alignment = TextAnchor.MiddleRight;
                text.color = Color.white;
                text.supportRichText = false;

                InputField inputField = inputGo.GetComponent<InputField>();
                inputField.textComponent = text;
                inputField.contentType = InputField.ContentType.DecimalNumber;
                fieldInputs.Add(inputField);
                y -= 24f;
            }

            container.sizeDelta = new Vector2(container.sizeDelta.x, Mathf.Max(300f, fieldBindings.Count * 24f + 20f));
        }

        void PopulateProfiles()
        {
            if (profileDropdown == null)
            {
                return;
            }

            profileDropdown.ClearOptions();
            if (library == null || library.profiles == null)
            {
                return;
            }

            var names = new List<string>();
            foreach (ShipTuning profile in library.profiles)
            {
                names.Add(profile != null ? profile.name : "Missing");
            }

            profileDropdown.AddOptions(names);
        }

        void BindUi()
        {
            if (applyButton != null)
            {
                applyButton.onClick.AddListener(ApplyFromFields);
            }

            if (saveButton != null)
            {
                saveButton.onClick.AddListener(SaveProfile);
            }

            if (resetButton != null)
            {
                resetButton.onClick.AddListener(ResetToDefault);
            }

            if (profileDropdown != null)
            {
                profileDropdown.onValueChanged.AddListener(OnProfileSelected);
            }
        }

        void SyncFieldsFromRuntime()
        {
            for (int i = 0; i < fieldBindings.Count && i < fieldInputs.Count; i++)
            {
                fieldInputs[i].text = fieldBindings[i].getter(runtimeCopy).ToString("0.###");
            }

            if (profileLabel != null)
            {
                profileLabel.text = sourceAsset != null ? sourceAsset.name : "Runtime";
            }
        }

        void ApplyFromFields()
        {
            for (int i = 0; i < fieldBindings.Count && i < fieldInputs.Count; i++)
            {
                if (float.TryParse(fieldInputs[i].text, out float value))
                {
                    value = Mathf.Clamp(value, fieldBindings[i].min, fieldBindings[i].max);
                    fieldBindings[i].setter(runtimeCopy, value);
                    fieldInputs[i].text = value.ToString("0.###");
                }
            }

            flight.Tuning = runtimeCopy;
        }

        void SaveProfile()
        {
            ApplyFromFields();
            ShipTuningJsonStore.Save(runtimeCopy, ShipTuningJsonStore.GetProfileKey(sourceAsset));
        }

        void ResetToDefault()
        {
            if (sourceAsset == null)
            {
                return;
            }

            runtimeCopy.CopyFrom(sourceAsset);
            flight.Tuning = runtimeCopy;
            SyncFieldsFromRuntime();
        }

        void OnProfileSelected(int index)
        {
            if (library == null || library.profiles == null || index < 0 || index >= library.profiles.Length)
            {
                return;
            }

            runtimeCopy.CopyFrom(library.profiles[index]);
            flight.Tuning = runtimeCopy;
            SyncFieldsFromRuntime();
        }
    }
}
