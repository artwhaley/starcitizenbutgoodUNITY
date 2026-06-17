using UnityEngine;
using UnityEngine.UI;

namespace FlightModel
{
    public class FlightTuningOverlay : MonoBehaviour
    {
        [SerializeField] GameObject root;
        [SerializeField] Text profileLabel;
        [SerializeField] Slider massSlider;
        [SerializeField] Slider boostSlider;
        [SerializeField] Slider forwardThrustSlider;
        [SerializeField] Slider torqueSlider;
        [SerializeField] Dropdown profileDropdown;
        [SerializeField] Button applyButton;
        [SerializeField] Button saveButton;
        [SerializeField] Button resetButton;

        ShipFlightController flight;
        ShipTuning sourceAsset;
        ShipTuning runtimeCopy;
        ShipTuningProfileLibrary library;

        public void Initialize(ShipFlightController controller, ShipTuning asset, ShipTuningProfileLibrary profileLibrary)
        {
            flight = controller;
            sourceAsset = asset;
            library = profileLibrary;
            runtimeCopy = Instantiate(asset);
            flight.Tuning = runtimeCopy;
            ShipTuningJsonStore.TryLoad(runtimeCopy);

            PopulateProfiles();
            BindUi();
            SyncSlidersFromRuntime();
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

        void PopulateProfiles()
        {
            profileDropdown.ClearOptions();
            if (library == null || library.profiles == null)
            {
                return;
            }

            var names = new System.Collections.Generic.List<string>();
            foreach (ShipTuning profile in library.profiles)
            {
                names.Add(profile != null ? profile.name : "Missing");
            }

            profileDropdown.AddOptions(names);
        }

        void BindUi()
        {
            applyButton.onClick.AddListener(ApplyFromSliders);
            saveButton.onClick.AddListener(SaveProfile);
            resetButton.onClick.AddListener(ResetToDefault);
            profileDropdown.onValueChanged.AddListener(OnProfileSelected);
        }

        void SyncSlidersFromRuntime()
        {
            massSlider.value = runtimeCopy.dryMassKg;
            boostSlider.value = runtimeCopy.boostAccelMultiplier;
            forwardThrustSlider.value = runtimeCopy.mainEngineForwardAccel * runtimeCopy.dryMassKg;
            torqueSlider.value = runtimeCopy.pitchPositiveAccel * runtimeCopy.dryMassKg;
            if (profileLabel != null)
            {
                profileLabel.text = sourceAsset != null ? sourceAsset.name : "Runtime";
            }
        }

        void ApplyFromSliders()
        {
            runtimeCopy.dryMassKg = massSlider.value;
            runtimeCopy.boostAccelMultiplier = boostSlider.value;
            runtimeCopy.mainEngineForwardAccel = forwardThrustSlider.value / Mathf.Max(1f, runtimeCopy.dryMassKg);
            float pitchTorqueEquivalent = torqueSlider.value / Mathf.Max(1f, runtimeCopy.dryMassKg);
            runtimeCopy.pitchPositiveAccel = pitchTorqueEquivalent;
            runtimeCopy.pitchNegativeAccel = pitchTorqueEquivalent;
            runtimeCopy.yawPositiveAccel = pitchTorqueEquivalent;
            runtimeCopy.yawNegativeAccel = pitchTorqueEquivalent;
            runtimeCopy.rollPositiveAccel = pitchTorqueEquivalent;
            runtimeCopy.rollNegativeAccel = pitchTorqueEquivalent;
            flight.Tuning = runtimeCopy;
        }

        void SaveProfile()
        {
            ApplyFromSliders();
            ShipTuningJsonStore.Save(runtimeCopy);
        }

        void ResetToDefault()
        {
            if (sourceAsset == null)
            {
                return;
            }

            CopyTuning(sourceAsset, runtimeCopy);
            flight.Tuning = runtimeCopy;
            SyncSlidersFromRuntime();
        }

        void OnProfileSelected(int index)
        {
            if (library == null || library.profiles == null || index < 0 || index >= library.profiles.Length)
            {
                return;
            }

            CopyTuning(library.profiles[index], runtimeCopy);
            flight.Tuning = runtimeCopy;
            SyncSlidersFromRuntime();
        }

        static void CopyTuning(ShipTuning from, ShipTuning to)
        {
            to.CopyFrom(from);
        }
    }
}
